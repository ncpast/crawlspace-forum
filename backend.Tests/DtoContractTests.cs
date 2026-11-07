using System.Text.Json;
using Backend.Dtos;
using Xunit;

namespace Backend.Tests;

public class DtoContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static string[] Keys(object dto) =>
        JsonDocument.Parse(JsonSerializer.Serialize(dto, Web))
            .RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray();

    [Fact]
    public void ThreadResponse_has_the_expected_json_shape()
    {
        var now = DateTime.UtcNow;
        var dto = new ThreadResponse(1, "t-1", "t", "c", "Anonymous", now, now,
            [new ReplyResponse(2, "r", "Anonymous", now)]);

        Assert.Equal(
            new[] { "author", "bumpedAt", "content", "createdAt", "id", "publicId", "replies", "title" },
            Keys(dto));
    }

    [Fact]
    public void PostResponse_has_the_expected_json_shape()
    {
        var now = DateTime.UtcNow;
        var dto = new PostResponse(1, "t-1", "t", "c", "Anonymous", now, now, 0);

        Assert.Equal(
            new[] { "author", "bumpedAt", "content", "createdAt", "id", "publicId", "replyCount", "title" },
            Keys(dto));
    }
}