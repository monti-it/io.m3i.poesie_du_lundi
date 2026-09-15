using MimeKit;

namespace PoesieDuLundi.Tests.Infrastructure.EmailImport;

/// <summary>Writes a real, MimeKit-round-tripped <c>.eml</c> file to disk for import tests —
/// avoids hand-authoring quoted-printable/MIME boundaries for every fixture while still exercising
/// the actual parser against actual MIME bytes (not an in-memory <c>MimeMessage</c>).</summary>
internal static class EmlFixtureBuilder
{
    public static string Write(
        string directory,
        string fileName,
        string subject,
        string body,
        DateTimeOffset date,
        string? messageId = null)
    {
        var message = new MimeMessage
        {
            Subject = subject,
            Date = date,
            Body = new TextPart("plain") { Text = body },
        };
        message.From.Add(new MailboxAddress("Marjan Monti", "marjan.monti@example.com"));
        message.To.Add(new MailboxAddress("Christophe Monti", "christophe.monti@example.com"));
        if (messageId is not null)
        {
            message.MessageId = messageId;
        }

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        message.WriteTo(path);
        return path;
    }
}
