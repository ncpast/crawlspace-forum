using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Backend.Tests;

public class ForumTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // remove the app's Npgsql registration
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    // create the DB (with the HasData boards) as soon as the host starts,
    // so every test sees tech/random whether or not it calls Seed
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    public void Seed(Action<AppDbContext> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seedAction(db);
        db.SaveChanges();
    }
}

public class ReadEndpointsTests : IClassFixture<ForumTestFactory>
{
    private readonly ForumTestFactory _factory;
    private readonly HttpClient _client;

    public ReadEndpointsTests(ForumTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBoards_ReturnsOk_WithSeededBoards()
    {
        var response = await _client.GetAsync("/boards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var boards = await response.Content.ReadFromJsonAsync<List<BoardResponse>>();
        Assert.NotNull(boards);
        Assert.Contains(boards, b => b.Slug == "tech" && b.Name == "Technology");
        Assert.Contains(boards, b => b.Slug == "random" && b.Name == "Random");
    }

    [Fact]
    public async Task GetBoardPosts_UnknownBoard_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/boards/unknown-board/posts");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBoardPosts_ReturnsPosts_OrderedByBumpedAtDesc()
    {
        var baseTime = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        _factory.Seed(db =>
        {
            var board = db.Boards.First(b => b.Slug == "tech");

            db.Posts.AddRange(
                new Post
                {
                    Id = 101,
                    BoardId = board.Id,
                    PublicId = "first-post-101",
                    Title = "First Post",
                    Content = "Content 1",
                    Author = "UserA",
                    CreatedAt = baseTime,
                    BumpedAt = baseTime
                },
                new Post
                {
                    Id = 102,
                    BoardId = board.Id,
                    PublicId = "second-post-bumped-102",
                    Title = "Second Post Bumped",
                    Content = "Content 2",
                    Author = "UserB",
                    CreatedAt = baseTime.AddHours(1),
                    BumpedAt = baseTime.AddHours(5) // Latest bump
                },
                new Post
                {
                    Id = 103,
                    BoardId = board.Id,
                    PublicId = "third-post-103",
                    Title = "Third Post",
                    Content = "Content 3",
                    Author = "UserC",
                    CreatedAt = baseTime.AddHours(2),
                    BumpedAt = baseTime.AddHours(2)
                }
            );

            // Add reply to post 102
            db.Replies.Add(new Reply
            {
                Id = 201,
                PostId = 102,
                Content = "Reply to second",
                Author = "Anon",
                CreatedAt = baseTime.AddHours(5)
            });
        });

        var response = await _client.GetAsync("/boards/tech/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var posts = await response.Content.ReadFromJsonAsync<List<PostResponse>>();
        Assert.NotNull(posts);
        Assert.True(posts.Count >= 3);

        // Verify order: bumped post (102) must be first
        Assert.Equal("second-post-bumped-102", posts[0].PublicId);
        Assert.Equal(1, posts[0].ReplyCount);
        Assert.Equal("third-post-103", posts[1].PublicId);
        Assert.Equal("first-post-101", posts[2].PublicId);
    }

    [Fact]
    public async Task GetBoardPosts_CapsAt50Posts()
    {
        using var factory = new ForumTestFactory();
        var client = factory.CreateClient();

        factory.Seed(db =>
        {
            var board = db.Boards.First(b => b.Slug == "tech");
            var now = DateTime.UtcNow;

            for (int i = 1; i <= 60; i++)
            {
                db.Posts.Add(new Post
                {
                    Id = 1000 + i,
                    BoardId = board.Id,
                    PublicId = $"cap-test-{i}",
                    Title = $"Post {i}",
                    Content = $"Content {i}",
                    Author = "Tester",
                    CreatedAt = now.AddMinutes(i),
                    BumpedAt = now.AddMinutes(i)
                });
            }
        });

        var response = await client.GetAsync("/boards/tech/posts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var posts = await response.Content.ReadFromJsonAsync<List<PostResponse>>();
        Assert.NotNull(posts);
        Assert.Equal(50, posts.Count);
    }

    [Fact]
    public async Task GetThread_ReturnsThread_WithRepliesInAscendingOrder()
    {
        var baseTime = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

        _factory.Seed(db =>
        {
            var board = db.Boards.First(b => b.Slug == "random");

            var post = new Post
            {
                Id = 301,
                BoardId = board.Id,
                PublicId = "my-random-thread-301",
                Title = "Random Thread",
                Content = "Hello random",
                Author = "Alice",
                CreatedAt = baseTime,
                BumpedAt = baseTime.AddMinutes(30)
            };
            db.Posts.Add(post);

            db.Replies.AddRange(
                new Reply
                {
                    Id = 502,
                    PostId = 301,
                    Content = "Second reply",
                    Author = "Charlie",
                    CreatedAt = baseTime.AddMinutes(20)
                },
                new Reply
                {
                    Id = 501,
                    PostId = 301,
                    Content = "First reply",
                    Author = null, // Should default to Anonymous
                    CreatedAt = baseTime.AddMinutes(10)
                },
                new Reply
                {
                    Id = 503,
                    PostId = 301,
                    Content = "Third reply",
                    Author = "Bob",
                    CreatedAt = baseTime.AddMinutes(30)
                }
            );
        });

        var response = await _client.GetAsync("/boards/random/posts/my-random-thread-301");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var thread = await response.Content.ReadFromJsonAsync<ThreadResponse>();
        Assert.NotNull(thread);
        Assert.Equal("my-random-thread-301", thread.PublicId);
        Assert.Equal("Random Thread", thread.Title);
        Assert.Equal("Hello random", thread.Content);
        Assert.Equal("Alice", thread.Author);

        Assert.Equal(3, thread.Replies.Count);
        // Ascending order check: 501, 502, 503
        Assert.Equal(501, thread.Replies[0].Id);
        Assert.Equal("First reply", thread.Replies[0].Content);
        Assert.Equal("Anonymous", thread.Replies[0].Author);

        Assert.Equal(502, thread.Replies[1].Id);
        Assert.Equal("Second reply", thread.Replies[1].Content);
        Assert.Equal("Charlie", thread.Replies[1].Author);

        Assert.Equal(503, thread.Replies[2].Id);
        Assert.Equal("Third reply", thread.Replies[2].Content);
        Assert.Equal("Bob", thread.Replies[2].Author);
    }

    [Fact]
    public async Task GetThread_UnknownBoard_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/boards/nonexistent/posts/some-post-123");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetThread_UnknownPost_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/boards/tech/posts/nonexistent-post-9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetThread_PostBelongingToDifferentBoard_ReturnsNotFound()
    {
        _factory.Seed(db =>
        {
            var randomBoard = db.Boards.First(b => b.Slug == "random");
            db.Posts.Add(new Post
            {
                Id = 777,
                BoardId = randomBoard.Id,
                PublicId = "exclusive-random-777",
                Title = "Random only",
                Content = "Text",
                Author = "Anon"
            });
        });

        // Requesting the random post under "tech" board must return 404
        var response = await _client.GetAsync("/boards/tech/posts/exclusive-random-777");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBoardPosts_EmptyBoard_ReturnsEmptyArray()
    {
        using var factory = new ForumTestFactory(); // fresh DB: seeded boards, no posts
        var client = factory.CreateClient();

        var response = await client.GetAsync("/boards/random/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetBoards_JsonIsCamelCase()
    {
        using var factory = new ForumTestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/boards");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = doc.RootElement[0];
        Assert.Equal(
            ["id", "slug", "name", "description"],
            first.EnumerateObject().Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task GetBoardPosts_JsonIsCamelCase_WithUtcTimestamps()
    {
        using var factory = new ForumTestFactory();
        var time = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        factory.Seed(db =>
        {
            db.Posts.Add(new Post
            {
                Id = 1, BoardId = 1, PublicId = "json-post-1",
                Title = "Json", Content = "Body", Author = "Anonymous",
                CreatedAt = time, BumpedAt = time
            });
        });
        var client = factory.CreateClient();

        using var doc = JsonDocument.Parse(await client.GetStringAsync("/boards/tech/posts"));
        var post = doc.RootElement[0];

        Assert.Equal(
            ["id", "publicId", "title", "content", "author", "createdAt", "bumpedAt", "replyCount"],
            post.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal("2026-11-01T12:00:00Z", post.GetProperty("createdAt").GetString());
        Assert.Equal("2026-11-01T12:00:00Z", post.GetProperty("bumpedAt").GetString());
        Assert.Equal(0, post.GetProperty("replyCount").GetInt32());
    }

    [Fact]
    public async Task GetThread_JsonIsCamelCase_WithUtcTimestamps()
    {
        using var factory = new ForumTestFactory();
        var time = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        factory.Seed(db =>
        {
            db.Posts.Add(new Post
            {
                Id = 1, BoardId = 1, PublicId = "json-thread-1",
                Title = "Json", Content = "Body", Author = "Anonymous",
                CreatedAt = time, BumpedAt = time
            });
            db.Replies.Add(new Reply
            {
                Id = 2, PostId = 1, Content = "Reply", Author = null, CreatedAt = time
            });
        });
        var client = factory.CreateClient();

        using var doc = JsonDocument.Parse(await client.GetStringAsync("/boards/tech/posts/json-thread-1"));
        var thread = doc.RootElement;

        Assert.Equal(
            ["id", "publicId", "title", "content", "author", "createdAt", "bumpedAt", "replies"],
            thread.EnumerateObject().Select(p => p.Name).ToArray());

        var reply = thread.GetProperty("replies")[0];
        Assert.Equal(
            ["id", "content", "author", "createdAt"],
            reply.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal("Anonymous", reply.GetProperty("author").GetString());
        Assert.Equal("2026-11-01T12:00:00Z", reply.GetProperty("createdAt").GetString());
    }
}
