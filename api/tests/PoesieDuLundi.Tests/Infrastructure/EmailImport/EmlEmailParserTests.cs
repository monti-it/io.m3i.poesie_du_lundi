using System.Runtime.CompilerServices;
using PoesieDuLundi.Infrastructure.EmailImport;

namespace PoesieDuLundi.Tests.Infrastructure.EmailImport;

public class EmlEmailParserTests
{
    private static string FixturePath(string fileName, [CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "Fixtures", fileName);

    [Fact]
    public void Parses_subject_body_date_and_message_id_from_a_real_gmail_style_eml()
    {
        var outcome = EmlEmailParser.Parse(FixturePath("encoded-quoted-printable.eml"));

        Assert.True(outcome.IsSuccess);
        var email = outcome.Email!;
        Assert.Equal("la poésie du lundi", email.Title);
        Assert.Contains("Sous les étoiles d'un ciel devénu.", email.Body);
        Assert.Equal(new DateOnly(2021, 1, 4), email.PublicationDate);
        Assert.Equal("fixture-encoded-quoted-printable@example.com", email.MessageId);
    }

    [Fact]
    public void Falls_back_to_the_HTML_body_when_there_is_no_text_part()
    {
        var outcome = EmlEmailParser.Parse(FixturePath("html-only.eml"));

        Assert.True(outcome.IsSuccess);
        Assert.Equal("Un poeme seulement en HTML.", outcome.Email!.Body);
    }

    [Fact]
    public void Skips_an_email_with_no_subject()
    {
        var outcome = EmlEmailParser.Parse(FixturePath("no-subject.eml"));

        Assert.False(outcome.IsSuccess);
        Assert.Contains("Subject", outcome.SkipReason);
    }

    [Fact]
    public void Skips_an_email_with_no_date()
    {
        var outcome = EmlEmailParser.Parse(FixturePath("no-date.eml"));

        Assert.False(outcome.IsSuccess);
        Assert.Contains("Date", outcome.SkipReason);
    }

    [Fact]
    public void Falls_back_to_a_path_derived_id_when_message_id_is_missing()
    {
        var path = FixturePath("missing-message-id.eml");

        var outcome = EmlEmailParser.Parse(path);

        Assert.True(outcome.IsSuccess);
        Assert.Contains(path, outcome.Email!.MessageId);
    }

    [Fact]
    public void Skips_a_file_that_is_not_a_valid_email_instead_of_throwing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.eml");
        File.WriteAllBytes(path, [0x00, 0x01, 0x02, 0xFF, 0xFE]);
        try
        {
            var outcome = EmlEmailParser.Parse(path);

            Assert.False(outcome.IsSuccess);
            Assert.NotNull(outcome.SkipReason);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
