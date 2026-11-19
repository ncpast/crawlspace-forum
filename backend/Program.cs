using Backend;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Backend.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddForumRateLimiting();

var app = builder.Build();

// create/update tables on startup (InMemory in tests is not relational, so it's skipped)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.UseRateLimiter();

app.MapGet("/", () => "Hello from backend");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/boards", async (AppDbContext db) =>
{
    var boards = await db.Boards
        .AsNoTracking()
        .OrderBy(b => b.Id)
        .Select(b => new BoardResponse(b.Id, b.Slug, b.Name, b.Description))
        .ToListAsync();

    return Results.Ok(boards);
}).RequireRateLimiting("read");

app.MapGet("/boards/{board}/posts", async (string board, AppDbContext db) =>
{
    var boardEntity = await db.Boards
        .AsNoTracking()
        .FirstOrDefaultAsync(b => b.Slug == board);

    if (boardEntity is null)
    {
        return Results.NotFound();
    }

    var posts = await db.Posts
        .AsNoTracking()
        .Where(p => p.BoardId == boardEntity.Id)
        .OrderByDescending(p => p.BumpedAt)
        .Take(50)
        .Select(p => new PostResponse(
            p.Id,
            p.PublicId,
            p.Title,
            p.Content,
            p.Author,
            p.CreatedAt,
            p.BumpedAt,
            p.Replies.Count
        ))
        .ToListAsync();

    return Results.Ok(posts);
}).RequireRateLimiting("read");

app.MapGet("/boards/{board}/posts/{publicId}", async (string board, string publicId, AppDbContext db) =>
{
    var boardEntity = await db.Boards
        .AsNoTracking()
        .FirstOrDefaultAsync(b => b.Slug == board);

    if (boardEntity is null)
    {
        return Results.NotFound();
    }

    var thread = await db.Posts
        .AsNoTracking()
        .Where(p => p.BoardId == boardEntity.Id && p.PublicId == publicId)
        .Select(p => new ThreadResponse(
            p.Id,
            p.PublicId,
            p.Title,
            p.Content,
            p.Author,
            p.CreatedAt,
            p.BumpedAt,
            p.Replies
                .OrderBy(r => r.Id)
                .Select(r => new ReplyResponse(
                    r.Id,
                    r.Content,
                    string.IsNullOrWhiteSpace(r.Author) ? "Anonymous" : r.Author,
                    r.CreatedAt
                ))
                .ToList()
        ))
        .FirstOrDefaultAsync();

    if (thread is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(thread);
}).RequireRateLimiting("read");;

app.MapPost("/boards/{board}/posts", async (string board, CreatePostRequest req, AppDbContext db) =>
{
    var boardEntity = await db.Boards
        .FirstOrDefaultAsync(b => b.Slug == board);

    if (boardEntity is null)
    {
        return Results.NotFound();
    }

    if (req is null)
    {
        return Results.BadRequest(new { message = "Request body is required." });
    }

    var title = req.Title?.Trim();
    if (string.IsNullOrWhiteSpace(title))
    {
        return Results.BadRequest(new { message = "Title is required." });
    }
    if (title.Length > 100)
    {
        return Results.BadRequest(new { message = "Title cannot exceed 100 characters." });
    }

    var content = req.Content?.Trim();
    if (string.IsNullOrWhiteSpace(content))
    {
        return Results.BadRequest(new { message = "Content is required." });
    }
    if (content.Length > 5000)
    {
        return Results.BadRequest(new { message = "Content cannot exceed 5000 characters." });
    }

    var author = string.IsNullOrWhiteSpace(req.Author) ? "Anonymous" : req.Author.Trim();
    if (author.Length > 30)
    {
        return Results.BadRequest(new { message = "Author cannot exceed 30 characters." });
    }

    var now = DateTime.UtcNow;
    var tempPublicId = Guid.NewGuid().ToString("N");
    var post = new Post
    {
        BoardId = boardEntity.Id,
        Title = title,
        Content = content,
        Author = author,
        CreatedAt = now,
        BumpedAt = now,
        PublicId = tempPublicId
    };

    if (db.Database.IsRelational())
    {
        using var tx = await db.Database.BeginTransactionAsync();
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        post.PublicId = SlugGenerator.GeneratePublicId(post.Title, post.Id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    else
    {
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        post.PublicId = SlugGenerator.GeneratePublicId(post.Title, post.Id);
        await db.SaveChangesAsync();
    }

    var response = new PostResponse(
        post.Id,
        post.PublicId,
        post.Title,
        post.Content,
        post.Author,
        post.CreatedAt,
        post.BumpedAt,
        0
    );

    return Results.Created($"/boards/{board}/posts/{post.PublicId}", response);
})
.RequireRateLimiting("create-post");

app.MapPost("/boards/{board}/posts/{publicId}/replies", async (string board, string publicId, CreateReplyRequest req, AppDbContext db) =>
{
    var boardEntity = await db.Boards
        .FirstOrDefaultAsync(b => b.Slug == board);

    if (boardEntity is null)
    {
        return Results.NotFound();
    }

    var post = await db.Posts
        .FirstOrDefaultAsync(p => p.BoardId == boardEntity.Id && p.PublicId == publicId);

    if (post is null)
    {
        return Results.NotFound();
    }

    if (req is null)
    {
        return Results.BadRequest(new { message = "Request body is required." });
    }

    var content = req.Content?.Trim();
    if (string.IsNullOrWhiteSpace(content))
    {
        return Results.BadRequest(new { message = "Content is required." });
    }
    if (content.Length > 5000)
    {
        return Results.BadRequest(new { message = "Content cannot exceed 5000 characters." });
    }

    var author = string.IsNullOrWhiteSpace(req.Author) ? "Anonymous" : req.Author.Trim();
    if (author.Length > 30)
    {
        return Results.BadRequest(new { message = "Author cannot exceed 30 characters." });
    }

    var now = DateTime.UtcNow;
    post.BumpedAt = now;

    var reply = new Reply
    {
        PostId = post.Id,
        Content = content,
        Author = author,
        CreatedAt = now
    };

    if (db.Database.IsRelational())
    {
        using var tx = await db.Database.BeginTransactionAsync();
        db.Replies.Add(reply);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    else
    {
        db.Replies.Add(reply);
        await db.SaveChangesAsync();
    }

    var response = new ReplyResponse(
        reply.Id,
        reply.Content,
        reply.Author ?? "Anonymous",
        reply.CreatedAt
    );

    return Results.Created($"/boards/{board}/posts/{publicId}", response);
}).RequireRateLimiting("create-reply");

app.Run();

public partial class Program { }
