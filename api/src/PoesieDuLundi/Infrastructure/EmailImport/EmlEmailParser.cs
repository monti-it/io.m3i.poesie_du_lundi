using System.Net;
using System.Text.RegularExpressions;
using MimeKit;

namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>Parses one raw RFC 822 <c>.eml</c> file (a real Gmail export: multipart, quoted-printable,
/// RFC 2047 encoded-word subjects) into a <see cref="ParsedEmail"/> — MimeKit does the MIME/charset
/// decoding, this class maps the result onto what a <see cref="Domain.Poem"/> needs: title via
/// <see cref="EmailTitleResolver"/>, body via <see cref="EmailBodyCleanup"/>.</summary>
public static partial class EmlEmailParser
{
    public static EmailParseOutcome Parse(string filePath)
    {
        MimeMessage message;
        try
        {
            message = MimeMessage.Load(filePath);
        }
        catch (Exception exception) when (exception is FormatException or IOException)
        {
            return EmailParseOutcome.Skip($"could not be parsed as an email ({exception.Message}).");
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            return EmailParseOutcome.Skip("has no Subject header to use as a title.");
        }

        var body = message.TextBody;
        if (string.IsNullOrWhiteSpace(body))
        {
            body = message.HtmlBody is { Length: > 0 } html ? HtmlToText(html) : null;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return EmailParseOutcome.Skip("has no text or HTML body to use as poem content.");
        }

        body = EmailBodyCleanup.StripAvgSignature(body.Trim());
        if (string.IsNullOrWhiteSpace(body))
        {
            return EmailParseOutcome.Skip("has no content left once the AVG signature footer is stripped.");
        }

        if (message.Date == default)
        {
            return EmailParseOutcome.Skip("has no Date header to use as a publication date.");
        }

        var (title, skipReason) = EmailTitleResolver.Resolve(message.Subject, body);
        if (skipReason is not null)
        {
            return EmailParseOutcome.Skip(skipReason);
        }

        // MessageId is the Gmail export's own de-dup key — the same email can appear in more than
        // one Takeout batch (issue #52's overlapping `Gmail`/`Gmail(N)` folders). Fall back to the
        // file path when it's missing so the email still imports, just without cross-batch de-dup.
        var messageId = string.IsNullOrWhiteSpace(message.MessageId)
            ? $"missing-message-id:{filePath}"
            : message.MessageId;

        return EmailParseOutcome.Success(new ParsedEmail(
            messageId,
            title!,
            body,
            DateOnly.FromDateTime(message.Date.Date),
            filePath));
    }

    private static string HtmlToText(string html) =>
        WebUtility.HtmlDecode(HtmlTag().Replace(html, string.Empty)).Trim();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();
}
