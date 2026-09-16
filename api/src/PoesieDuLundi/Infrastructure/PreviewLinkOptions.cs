namespace PoesieDuLundi.Infrastructure;

/// <summary>How <see cref="PreviewTokenService"/> signs and expires preview links.</summary>
/// <param name="SigningKey">The server-wide HMAC secret. Must stay consistent across replicas —
/// the token carries no DB row, so any replica needs the same key to validate one another's
/// tokens.</param>
/// <param name="LinkLifetime">How long an issued token stays valid.</param>
public sealed record PreviewLinkOptions(string SigningKey, TimeSpan LinkLifetime)
{
    public static readonly TimeSpan DefaultLinkLifetime = TimeSpan.FromDays(7);
}
