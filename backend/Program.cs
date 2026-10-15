using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// create/update tables on startup
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.MapGet("/", () => "Hello from backend");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/posts", async (AppDbContext db) =>
    await db.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync());

app.MapPost("/posts", async (AppDbContext db, Post post) =>
{
    post.Id = 0;
    post.CreatedAt = DateTime.UtcNow;
    db.Posts.Add(post);
    await db.SaveChangesAsync();
    return Results.Created($"/posts/{post.Id}", post);
});

app.Run();