using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// create/update tables on startup (InMemory in tests is not relational, so it's skipped)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

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
});

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
});

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
});

app.Run();

public partial class Program { }
