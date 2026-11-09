using System.Net;
using System.Net.Http.Json;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests;

public class ForumTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }

    public void Seed(Action<AppDbContext> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
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
        _factory.Seed(_ => { });

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
        var factory = new ForumTestFactory();
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
}
