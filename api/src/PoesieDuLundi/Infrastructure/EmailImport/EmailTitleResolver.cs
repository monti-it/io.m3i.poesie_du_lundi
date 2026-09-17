using System.Text.RegularExpressions;

namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>
/// Derives a poem title from one email's subject and (already AVG-signature-stripped) body —
/// the corrected replacement for the original importer's "use the raw Subject verbatim" logic
/// (issue #52's reopening). The archive's subjects follow the site's "la poésie du lundi : &lt;title&gt;"
/// convention inconsistently — casing, accents, typos ("oundi", "di", "l a"), and separator
/// (colon, full-width colon, or a stray "/") all vary — and roughly half the archive has no
/// subtitle after the prefix at all, in which case the real title is the first line of the body,
/// usually set off with *asterisks*.
/// </summary>
/// <remarks>
/// This is deliberately conservative rather than exhaustively clever: real archive review (see the
/// issue) showed that guessing beyond what's below produces wrong titles — trailing subject text
/// with no recognised separator is sometimes the real subtitle and sometimes just commentary (e.g.
/// "... après la lecture d'un roman" turned out to describe the poem, not name it), and a body
/// whose opening lines carry no *asterisk-wrapped* line can't be told apart from a personal note
/// ("Bonjour à tous, ...") preceding the actual poem. Both are reported as a skip instead of a
/// guess, per the issue's "explicitly called out, not silently imported" requirement — same spirit
/// as the existing duplicate handling.
/// </remarks>
public static partial class EmailTitleResolver
{
    /// <summary>Resolves <paramref name="subject"/> (optionally using <paramref name="body"/> as a
    /// fallback) to a title, or a human-readable reason it can't be resolved without guessing.</summary>
    public static (string? Title, string? SkipReason) Resolve(string subject, string body)
    {
        var withoutForwardMarkers = ForwardMarkers().Replace(subject.Trim(), string.Empty).Trim();

        if (ReplyMarker().IsMatch(withoutForwardMarkers))
        {
            return (null, $"subject '{subject}' looks like a reply in the conversation thread, not the poem itself.");
        }

        var coreMatch = SitePrefixCore().Match(withoutForwardMarkers);
        if (!coreMatch.Success)
        {
            // No recognisable "la poésie du lundi" prefix at all — a genuine custom subject
            // (e.g. "Au pied de mon arbre"), used verbatim as it always has been.
            return (withoutForwardMarkers, null);
        }

        var rest = withoutForwardMarkers[coreMatch.Length..];
        var separatorMatch = TitleSeparator().Match(rest);
        if (separatorMatch.Success)
        {
            var remainder = separatorMatch.Groups[1].Value.Trim();
            if (remainder.Length > 0)
            {
                return (remainder, null);
            }
        }
        else if (rest.Trim().Length > 0)
        {
            return (null,
                $"subject '{subject}' has text after 'la poésie du lundi' that isn't colon-separated — " +
                "ambiguous whether it's the real title or commentary, needs manual review.");
        }

        // Bare prefix, nothing usable in the subject: fall back to a pseudo-title in the body.
        var pseudoTitle = PseudoTitleFromBody(body);
        if (pseudoTitle is null)
        {
            return (null,
                $"subject '{subject}' has no title after 'la poésie du lundi', and the body's opening " +
                "lines don't show a clear *title* line — needs manual review.");
        }

        return (pseudoTitle, null);
    }

    /// <summary>The real title, for a bare-prefix subject, is almost always the first non-blank
    /// body line when it's wrapped in *asterisks* (the sender's way of marking it as a title) — or,
    /// when that first line is itself an epigraph (e.g. "À la manière d'Edmond Rostand"), the second
    /// line. Anything else can't be told apart from an ordinary opening line of verse or a personal
    /// note, so it isn't guessed.</summary>
    private static string? PseudoTitleFromBody(string body)
    {
        var lines = body
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Take(2)
            .ToList();

        if (lines.Count > 0 && AsteriskWrappedLine().IsMatch(lines[0]))
        {
            return lines[0][1..^1].Trim();
        }

        if (lines.Count > 1 && AsteriskWrappedLine().IsMatch(lines[1]))
        {
            return lines[1][1..^1].Trim();
        }

        return null;
    }

    [GeneratedRegex(@"^(?:(?:fwd|tr)\s*:\s*)+", RegexOptions.IgnoreCase)]
    private static partial Regex ForwardMarkers();

    [GeneratedRegex(@"^re\s*:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex ReplyMarker();

    [GeneratedRegex(@"^(?:\d+\s*°?\s*)?(?:(?:la|l\s*a|laa|ma)\s+)?po[eé]sie\s*,?\s*(?:du|di)\s+(?:lundi|oundi)",
        RegexOptions.IgnoreCase)]
    private static partial Regex SitePrefixCore();

    [GeneratedRegex(@"^\s*[:：/]\s*(.*)$", RegexOptions.Singleline)]
    private static partial Regex TitleSeparator();

    [GeneratedRegex(@"^\*[^*]+\*$")]
    private static partial Regex AsteriskWrappedLine();
}
