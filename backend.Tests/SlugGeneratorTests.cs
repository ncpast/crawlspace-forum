using Backend;
using Xunit;

namespace Backend.Tests;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("My Cool Setup", "my-cool-setup")]
    [InlineData("  Hello,   World!  ", "hello-world")]
    [InlineData("arch vs debian: which one?", "arch-vs-debian-which-one")]
    [InlineData("C# 14 & .NET 10", "c-14-net-10")]
    public void Normalizes_ascii_titles(string title, string expected) =>
        Assert.Equal(expected, SlugGenerator.GenerateSlug(title));

    [Theory]
    [InlineData("Zażółć gęślą jaźń", "zazolc-gesla-jazn")]
    [InlineData("Łódź", "lodz")]
    [InlineData("Café déjà vu", "cafe-deja-vu")]
    public void Transliterates_diacritics(string title, string expected) =>
        Assert.Equal(expected, SlugGenerator.GenerateSlug(title));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("日本語")]
    [InlineData("🔥🔥🔥")]
    public void Falls_back_to_post_when_nothing_usable(string title) =>
        Assert.Equal("post", SlugGenerator.GenerateSlug(title));

    [Fact]
    public void Caps_length_and_never_ends_with_hyphen()
    {
        var slug = SlugGenerator.GenerateSlug(string.Concat(Enumerable.Repeat("abcd ", 20)));

        Assert.True(slug.Length <= 60);
        Assert.False(slug.EndsWith('-'));
    }

    [Fact]
    public void PublicId_is_slug_dash_id() =>
        Assert.Equal("my-cool-setup-4727482", SlugGenerator.GeneratePublicId("My Cool Setup", 4727482));

    [Fact]
    public void PublicId_with_empty_title_still_works() =>
        Assert.Equal("post-42", SlugGenerator.GeneratePublicId("", 42));

    [Fact]
    public void Id_is_whatever_follows_the_last_hyphen()
    {
        var publicId = SlugGenerator.GeneratePublicId("Windows 11 vs 10", 4727482);

        Assert.Equal("4727482", publicId[(publicId.LastIndexOf('-') + 1)..]);
    }
}