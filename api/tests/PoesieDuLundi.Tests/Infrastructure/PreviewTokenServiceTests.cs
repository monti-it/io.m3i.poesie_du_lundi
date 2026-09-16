using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Tests.Infrastructure;

public class PreviewTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    private static PreviewTokenService CreateService(DateTimeOffset now, TimeSpan? lifetime = null) =>
        new(
            new PreviewLinkOptions("a-test-signing-key", lifetime ?? TimeSpan.FromDays(7)),
            new FakeTimeProvider(now));

    [Fact]
    public void An_issued_token_validates_for_the_same_poem()
    {
        var service = CreateService(Now);
        var poemId = Guid.NewGuid();

        var issued = service.Issue(poemId);

        Assert.True(service.Validate(poemId, issued.Token));
        Assert.Equal(Now + TimeSpan.FromDays(7), issued.ExpiresAt);
    }

    [Fact]
    public void A_token_does_not_validate_for_a_different_poem()
    {
        var service = CreateService(Now);
        var issued = service.Issue(Guid.NewGuid());

        Assert.False(service.Validate(Guid.NewGuid(), issued.Token));
    }

    [Fact]
    public void A_tampered_token_does_not_validate()
    {
        var service = CreateService(Now);
        var poemId = Guid.NewGuid();
        var issued = service.Issue(poemId);
        var tampered = issued.Token[..^1] + (issued.Token[^1] == 'a' ? 'b' : 'a');

        Assert.False(service.Validate(poemId, tampered));
    }

    [Fact]
    public void An_expired_token_does_not_validate()
    {
        var issuer = CreateService(Now, TimeSpan.FromDays(1));
        var poemId = Guid.NewGuid();
        var issued = issuer.Issue(poemId);
        var afterExpiry = CreateService(Now + TimeSpan.FromDays(2));

        Assert.False(afterExpiry.Validate(poemId, issued.Token));
    }

    [Fact]
    public void A_token_signed_with_a_different_key_does_not_validate()
    {
        var issuer = CreateService(Now);
        var poemId = Guid.NewGuid();
        var issued = issuer.Issue(poemId);
        var otherKeyService = new PreviewTokenService(
            new PreviewLinkOptions("a-different-signing-key", TimeSpan.FromDays(7)), new FakeTimeProvider(Now));

        Assert.False(otherKeyService.Validate(poemId, issued.Token));
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("")]
    [InlineData("only.two-parts")]
    public void A_malformed_token_does_not_validate(string token)
    {
        var service = CreateService(Now);

        Assert.False(service.Validate(Guid.NewGuid(), token));
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
