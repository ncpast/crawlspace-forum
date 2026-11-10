using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Backend;

public static partial class SlugGenerator
{
    private const int MaxSlugLength = 60;
    private const string Fallback = "post";

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Fallback;

        var ascii = RemoveDiacritics(title.ToLowerInvariant());
        var slug = NonAlphanumericRegex().Replace(ascii, "-").Trim('-');

        if (slug.Length > MaxSlugLength)
            slug = slug[..MaxSlugLength].TrimEnd('-');

        return slug.Length == 0 ? Fallback : slug;
    }

    public static string GeneratePublicId(string title, long id) =>
        $"{GenerateSlug(title)}-{id}";

    private static string RemoveDiacritics(string text)
    {
        // ł has no decomposition, so it needs its own mapping
        var decomposed = text.Replace('ł', 'l').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }
}