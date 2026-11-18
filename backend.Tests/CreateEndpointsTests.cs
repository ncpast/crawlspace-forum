using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Dtos;
using Backend.Models;
using Xunit;

namespace Backend.Tests;

public class CreateEndpointsTests : IClassFixture<ForumTestFactory>
{
    private readonly ForumTestFactory _factory;
    private readonly HttpClient _client;

    public CreateEndpointsTests(ForumTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePost_ValidInput_Returns201_WithCorrectObject()
    {
        var request = new CreatePostRequest("Hello World", "This is the content.", "Alice");

        var response = await _client.PostAsJsonAsync("/boards/tech/posts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.StartsWith("hello-world-", created.PublicId);
        Assert.Equal("Hello World", created.Title);
        Assert.Equal("This is the content.", created.Content);
        Assert.Equal("Alice", created.Author);
        Assert.Equal(created.CreatedAt, created.BumpedAt);
        Assert.Equal(0, created.ReplyCount);

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(created.PublicId, response.Headers.Location.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePost_AuthorDefaultsToAnonymous_WhenOmittedOrWhitespace(string? author)
    {
        var request = new CreatePostRequest("Anonymous Post Title", "Content text", author);

        var response = await _client.PostAsJsonAsync("/boards/tech/posts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(created);
        Assert.Equal("Anonymous", created.Author);
    }

    [Fact]
    public async Task CreatePost_AuthorIsTrimmed()
    {
        var request = new CreatePostRequest("Trimmed Author", "Content text", "   CoolUser   ");

        var response = await _client.PostAsJsonAsync("/boards/tech/posts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(created);
        Assert.Equal("CoolUser", created.Author);
    }

    [Fact]
    public async Task CreatePost_UnknownBoard_Returns404()
    {
        var request = new CreatePostRequest("Title", "Content", "Author");

        var response = await _client.PostAsJsonAsync("/boards/nonexistent-board/posts", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(null, "Content")]
    [InlineData("", "Content")]
    [InlineData("   ", "Content")]
    public async Task CreatePost_MissingOrEmptyTitle_Returns400(string? title, string content)
    {
        var request = new CreatePostRequest(title!, content, "Author");

        var response = await _client.PostAsJsonAsync("/boards/tech/posts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePost_TitleExceeding100Chars_Returns400()
    {
        var longTitle = new string('a', 101);
        var request = new CreatePostRequest(longTitle, "Content", "Author");

        var response = await _client.PostAsJsonAsync("/boards/tech/posts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("100", body);
    }

    [Theory]
    [InlineData("Title", null)]
    [InlineData("Title", "")]
    [InlineData("Title", "   ")]
    public async Task CreatePost_MissingOrEmptyContent_Returns400(string title, string? content)
    {
        var request = new CreatePostRequest(title, content!, "Author");

        var response = await _client.PostAsJsonAsync("/boards/tech/posts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Content", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePost_ContentExceeding5000Chars_Returns400()
    {
        var longContent = new string('c', 5001);
        var request = new CreatePostRequest("Valid Title", longContent, "Author");

        var response = await _client.PostAsJsonAsync("/boards/tech/posts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("5000", body);
    }

    [Fact]
    public async Task CreatePost_AuthorExceeding30Chars_Returns400()
    {
        var longAuthor = new string('u', 31);
        var request = new CreatePostRequest("Valid Title", "Valid Content", longAuthor);

        var response = await _client.PostAsJsonAsync("/boards/tech/posts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("30", body);
    }

    [Fact]
    public async Task CreatePost_AppearsFirstOnItsBoard()
    {
        using var factory = new ForumTestFactory();
        var client = factory.CreateClient();

        // Create older post
        var older = await client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Older Post", "Older content", null));
        Assert.Equal(HttpStatusCode.Created, older.StatusCode);
        var olderPost = await older.Content.ReadFromJsonAsync<PostResponse>();

        // Small delay to ensure timestamp progression
        await Task.Delay(10);

        // Create newer post
        var newer = await client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Newer Post", "Newer content", null));
        Assert.Equal(HttpStatusCode.Created, newer.StatusCode);
        var newerPost = await newer.Content.ReadFromJsonAsync<PostResponse>();

        // Check board posts
        var boardPosts = await client.GetFromJsonAsync<List<PostResponse>>("/boards/tech/posts");
        Assert.NotNull(boardPosts);
        Assert.True(boardPosts.Count >= 2);
        Assert.Equal(newerPost!.PublicId, boardPosts[0].PublicId);
        Assert.Equal(olderPost!.PublicId, boardPosts[1].PublicId);
    }

    [Fact]
    public async Task CreateReply_ValidInput_Returns201_WithCorrectObject()
    {
        var postRes = await _client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Thread for Reply", "Content", "OP"));
        var post = await postRes.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        var replyRequest = new CreateReplyRequest("First reply!", "Replier");
        var response = await _client.PostAsJsonAsync($"/boards/tech/posts/{post.PublicId}/replies", replyRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var reply = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        Assert.NotNull(reply);
        Assert.True(reply.Id > 0);
        Assert.Equal("First reply!", reply.Content);
        Assert.Equal("Replier", reply.Author);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateReply_AuthorDefaultsToAnonymous_WhenOmittedOrWhitespace(string? author)
    {
        var postRes = await _client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Thread for Anon Reply", "Content", null));
        var post = await postRes.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        var replyRequest = new CreateReplyRequest("Anon reply", author);
        var response = await _client.PostAsJsonAsync($"/boards/tech/posts/{post.PublicId}/replies", replyRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reply = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        Assert.NotNull(reply);
        Assert.Equal("Anonymous", reply.Author);
    }

    [Fact]
    public async Task CreateReply_AuthorIsTrimmed()
    {
        var postRes = await _client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Thread for Trimmed Reply", "Content", null));
        var post = await postRes.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        var replyRequest = new CreateReplyRequest("Trimmed author reply", "   SpacedAuthor   ");
        var response = await _client.PostAsJsonAsync($"/boards/tech/posts/{post.PublicId}/replies", replyRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reply = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        Assert.NotNull(reply);
        Assert.Equal("SpacedAuthor", reply.Author);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateReply_MissingOrEmptyContent_Returns400(string? content)
    {
        var postRes = await _client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Thread for Bad Reply", "Content", null));
        var post = await postRes.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        var replyRequest = new CreateReplyRequest(content!, "Author");
        var response = await _client.PostAsJsonAsync($"/boards/tech/posts/{post.PublicId}/replies", replyRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Content", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateReply_ContentExceeding5000Chars_Returns400()
    {
        var postRes = await _client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Thread for Huge Reply", "Content", null));
        var post = await postRes.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        var longContent = new string('r', 5001);
        var replyRequest = new CreateReplyRequest(longContent, "Author");
        var response = await _client.PostAsJsonAsync($"/boards/tech/posts/{post.PublicId}/replies", replyRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("5000", body);
    }

    [Fact]
    public async Task CreateReply_AuthorExceeding30Chars_Returns400()
    {
        var postRes = await _client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Thread for Long Author", "Content", null));
        var post = await postRes.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        var longAuthor = new string('x', 31);
        var replyRequest = new CreateReplyRequest("Valid content", longAuthor);
        var response = await _client.PostAsJsonAsync($"/boards/tech/posts/{post.PublicId}/replies", replyRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("30", body);
    }

    [Fact]
    public async Task CreateReply_UnknownBoard_Returns404()
    {
        var replyRequest = new CreateReplyRequest("Reply content", "Author");
        var response = await _client.PostAsJsonAsync("/boards/nonexistent/posts/some-post-1/replies", replyRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateReply_UnknownPost_Returns404()
    {
        var replyRequest = new CreateReplyRequest("Reply content", "Author");
        var response = await _client.PostAsJsonAsync("/boards/tech/posts/nonexistent-post-9999/replies", replyRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateReply_MovesItsPostToTheTop_AndIncrementsReplyCount()
    {
        using var factory = new ForumTestFactory();
        var client = factory.CreateClient();

        // Create first post (Post A)
        var resA = await client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Post A", "Content A", null));
        var postA = await resA.Content.ReadFromJsonAsync<PostResponse>();

        await Task.Delay(20);

        // Create second post (Post B) - this one is now at the top
        var resB = await client.PostAsJsonAsync("/boards/tech/posts", new CreatePostRequest("Post B", "Content B", null));
        var postB = await resB.Content.ReadFromJsonAsync<PostResponse>();

        // Verify Post B is at top
        var listBefore = await client.GetFromJsonAsync<List<PostResponse>>("/boards/tech/posts");
        Assert.Equal(postB!.PublicId, listBefore![0].PublicId);
        Assert.Equal(postA!.PublicId, listBefore[1].PublicId);

        await Task.Delay(20);

        // Add reply to Post A (bumping Post A)
        var replyRes = await client.PostAsJsonAsync($"/boards/tech/posts/{postA.PublicId}/replies", new CreateReplyRequest("Bump Post A!", null));
        Assert.Equal(HttpStatusCode.Created, replyRes.StatusCode);

        // Verify Post A is now moved to the top!
        var listAfter = await client.GetFromJsonAsync<List<PostResponse>>("/boards/tech/posts");
        Assert.NotNull(listAfter);
        Assert.Equal(postA.PublicId, listAfter[0].PublicId);
        Assert.Equal(1, listAfter[0].ReplyCount);
        Assert.True(listAfter[0].BumpedAt > listAfter[0].CreatedAt);

        // Post B is now second
        Assert.Equal(postB.PublicId, listAfter[1].PublicId);

        // Verify reply is visible inside the thread
        var thread = await client.GetFromJsonAsync<ThreadResponse>($"/boards/tech/posts/{postA.PublicId}");
        Assert.NotNull(thread);
        Assert.Single(thread.Replies);
        Assert.Equal("Bump Post A!", thread.Replies[0].Content);
    }
}
