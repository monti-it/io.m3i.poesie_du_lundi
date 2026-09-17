using System.Text.RegularExpressions;

namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>Strips the AVG "Garanti sans virus" antivirus-signature footer that AVG's Outlook
/// add-in appended to 8 of the archive's emails — real content, but of the mail client, not of the
/// poem (issue #52's reopening). Left as a narrow, single-purpose cleanup: general body-formatting
/// normalization (e.g. collapsing soft-wrapped blank lines) was investigated and dropped — real
/// archive samples show poems that intentionally blank-line-separate every verse, so a blank line
/// can't be told apart from a soft-wrap artifact without risking mangling genuine poems.</summary>
public static partial class EmailBodyCleanup
{
    public static string StripAvgSignature(string body) => AvgSignatureFooter().Replace(body, string.Empty).TrimEnd();

    [GeneratedRegex(
        @"\s*(\[image:\s*Image\]\s*)?(<http://www\.avg\.com/email-signature\?[^>]*>\s*)?" +
        @"Garanti\s+sans\s+virus\.\s*www\.avg\.com\s*" +
        @"(<http://www\.avg\.com/email-signature\?[^>]*>\s*)?(<#[0-9A-Za-z-]+>\s*)?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex AvgSignatureFooter();
}
