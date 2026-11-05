using System.ComponentModel.DataAnnotations;
using Backend.Models;
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
}