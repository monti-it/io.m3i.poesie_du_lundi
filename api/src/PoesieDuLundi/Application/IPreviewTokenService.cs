namespace PoesieDuLundi.Application;

/// <summary>A signed, expiring token that grants access to one poem's preview (issue #25).</summary>
public sealed record IssuedPreviewToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// The stateless preview-token port — defined here (Application), fulfilled by Infrastructure
/// with an HMAC signature over the poem id and expiry, per docs/ENGINEERING_PRACTICES.md "Layer
/// responsibilities". No DB row: <see cref="Validate"/> re-derives the signature from
/// <paramref name="poemId"/> and the token's own embedded expiry rather than looking anything up.
/// </summary>
public interface IPreviewTokenService
{
    IssuedPreviewToken Issue(Guid poemId);

    bool Validate(Guid poemId, string token);
}
