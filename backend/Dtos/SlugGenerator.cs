using System.Text.RegularExpressions;

namespace Backend;

public static partial class SlugGenerator
{
    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "post";

        var lower = title.ToLowerInvariant();
        var slug = NonAlphanumericRegex().Replace(lower, "-").Trim('-');

        return string.IsNullOrEmpty(slug) ? "post" : slug;
    }

    public static string GeneratePublicId(string title, long id)
    {
        var slug = GenerateSlug(title);
        return $"{slug}-{id}";
    }
}