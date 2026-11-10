using System.ComponentModel.DataAnnotations;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests;

public class ModelDefaultsTests
{
    [Fact]
    public void New_post_defaults_to_anonymous_author()
    {
        Assert.Equal("Anonymous", new Post().Author);
    }

    [Fact]
    public void Title_longer_than_100_characters_is_invalid()
    {
        var post = new Post { Title = new string('a', 101), Content = "x", PublicId = "p" };

        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(post, new ValidationContext(post), results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Posts_and_replies_share_the_same_id_sequence()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("test-sequence-model")
            .Options;
        using var context = new AppDbContext(options);
        var model = context.Model;

        var sequence = model.FindSequence("PostReplyNumbers");
        Assert.NotNull(sequence);

        var postDefaultSql = model.FindEntityType(typeof(Post))!
            .FindProperty(nameof(Post.Id))!
            .GetDefaultValueSql();

        var replyDefaultSql = model.FindEntityType(typeof(Reply))!
            .FindProperty(nameof(Reply.Id))!
            .GetDefaultValueSql();

        Assert.Equal("nextval('\"PostReplyNumbers\"')", postDefaultSql);
        Assert.Equal("nextval('\"PostReplyNumbers\"')", replyDefaultSql);
    }
}
