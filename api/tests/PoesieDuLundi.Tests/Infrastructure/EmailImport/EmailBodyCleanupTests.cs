using PoesieDuLundi.Infrastructure.EmailImport;

namespace PoesieDuLundi.Tests.Infrastructure.EmailImport;

public class EmailBodyCleanupTests
{
    [Fact]
    public void Strips_the_AVG_signature_block_with_a_leading_image_placeholder()
    {
        const string body =
            "Dernier vers du poème.\n\nMarjan\n\n[image: Image]\n\n" +
            "<http://www.avg.com/email-signature?utm_medium=email&utm_source=link&utm_campaign=sig-email&utm_content=webmail>\n" +
            "Garanti\nsans virus. www.avg.com\n" +
            "<http://www.avg.com/email-signature?utm_medium=email&utm_source=link&utm_campaign=sig-email&utm_content=webmail>\n" +
            "<#DAB4FAD8-2DD7-40BB-A1B8-4E2AA1F9FDF2>\n";

        var cleaned = EmailBodyCleanup.StripAvgSignature(body);

        Assert.Equal("Dernier vers du poème.\n\nMarjan", cleaned);
    }

    [Fact]
    public void Strips_the_AVG_signature_block_without_the_image_placeholder()
    {
        const string body =
            "Dernier vers du poème.\n\nMarjan\n\n" +
            "<http://www.avg.com/email-signature?utm_medium=email&utm_source=link&utm_campaign=sig-email&utm_content=webmail>\n" +
            "Garanti\nsans virus. www.avg.com\n" +
            "<http://www.avg.com/email-signature?utm_medium=email&utm_source=link&utm_campaign=sig-email&utm_content=webmail>\n" +
            "<#DAB4FAD8-2DD7-40BB-A1B8-4E2AA1F9FDF2>\n";

        var cleaned = EmailBodyCleanup.StripAvgSignature(body);

        Assert.Equal("Dernier vers du poème.\n\nMarjan", cleaned);
    }

    [Fact]
    public void Leaves_a_body_with_no_AVG_signature_untouched()
    {
        const string body = "Dernier vers du poème.\n\nMarjan";

        var cleaned = EmailBodyCleanup.StripAvgSignature(body);

        Assert.Equal(body, cleaned);
    }
}
