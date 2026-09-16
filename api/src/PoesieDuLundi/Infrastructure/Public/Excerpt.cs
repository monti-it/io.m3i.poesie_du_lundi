using System.Text.RegularExpressions;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>Plain-text preview of a poem's Markdown body — a <c>&lt;meta name="description"&gt;</c>
/// or JSON-LD <c>description</c> needs prose, not Markdown syntax, and neither wants the whole
/// poem. Mirrors <c>frontend/src/shared/lib/excerpt.ts</c> exactly (same regexes, same 160-char
/// word-boundary truncation), since the client-rendered <c>PoemPage</c> and the crawler-facing
/// unfurl HTML (issue #83, <see cref="PoesieDuLundi.Api.Public.PublicPoemEndpoints"/>) must agree
/// on the description for the same poem.</summary>
internal static partial class Excerpt
{
    private const int MaxLength = 160;

    public static string From(string markdown, int maxLength = MaxLength)
    {
        var plainText = WhitespaceRun().Replace(
            MarkdownLink().Replace(MarkdownSyntax().Replace(markdown, ""), "$1"), " ").Trim();

        if (plainText.Length <= maxLength)
        {
            return plainText;
        }

        var truncated = plainText[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        return $"{truncated[..(lastSpace > 0 ? lastSpace : maxLength)]}…";
    }

    [GeneratedRegex(@"[#>*_`~]")]
    private static partial Regex MarkdownSyntax();

    [GeneratedRegex(@"\[(.*?)\]\(.*?\)")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
