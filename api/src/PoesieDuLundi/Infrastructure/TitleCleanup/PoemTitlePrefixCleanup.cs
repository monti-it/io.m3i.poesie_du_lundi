using System.Text.RegularExpressions;

namespace PoesieDuLundi.Infrastructure.TitleCleanup;

/// <summary>
/// Detects and removes the redundant "La poésie du lundi :" site-name prefix that most poem
/// titles still carry (issue #62, reopened) even after issue #52's corrected reimport. The
/// archive's subjects are inconsistent (<c>La</c>/<c>la</c>/<c>Ma</c>/<c>l a</c>, a leading
/// number, straight vs full-width colon or a stray "/", extra spacing, "poesie" vs "poésie"), so
/// the match is as tolerant as <see cref="EmailImport.EmailTitleResolver"/>'s.
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

    [GeneratedRegex(
        @"^\s*(?:\d+\s*°?\s*)?(?:(?:la|l\s*a|laa|ma)\s+)?po[eé]sie\s*,?\s*(?:du|di)\s+(?:lundi|oundi)\s*[:：/]\s*",
        RegexOptions.IgnoreCase)]
    private static partial Regex SitePrefix();
}
