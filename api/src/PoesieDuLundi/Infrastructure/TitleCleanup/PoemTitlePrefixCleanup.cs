using System.Text.RegularExpressions;

namespace PoesieDuLundi.Infrastructure.TitleCleanup;

/// <summary>
/// Detects and removes the redundant "La poésie du lundi :" site-name prefix that most poem
/// titles carry (issue #62) — a side effect of issue #52's import, which used the raw Gmail
/// <c>Subject:</c> header as the title verbatim. The archive's subjects are inconsistent
/// (<c>La</c>/<c>la</c>/<c>Ma</c> casing, straight vs full-width colon, extra spacing, "poesie" vs
/// "poésie"), so the match is deliberately tolerant of all of those.
/// </summary>
public static partial class PoemTitlePrefixCleanup
{
    /// <summary>Strips a matching prefix and returns the remaining title, trimmed; or
    /// <see langword="null"/> if <paramref name="title"/> doesn't start with the site-name prefix,
    /// or the prefix is all there is (no real subtitle to fall back to — stripping it would leave
    /// an empty title).</summary>
    public static string? StripSitePrefix(string title)
    {
        var match = SitePrefix().Match(title);
        if (!match.Success)
        {
            return null;
        }

        var remainder = title[match.Length..].Trim();
        return remainder.Length == 0 ? null : remainder;
    }

    [GeneratedRegex(@"^\s*la\s+po[eé]sie\s+du\s+lundi\s*[:：]\s*", RegexOptions.IgnoreCase)]
    private static partial Regex SitePrefix();
}
