using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PoesieDuLundi.Application;

namespace PoesieDuLundi.Infrastructure;

/// <summary>HMAC-SHA256 over <c>"{poemId}.{expiryUnixSeconds}"</c> with a server secret — no DB
/// row, so any replica sharing <see cref="PreviewLinkOptions.SigningKey"/> can validate a token
/// another replica minted.</summary>
public sealed class PreviewTokenService(PreviewLinkOptions options, TimeProvider timeProvider) : IPreviewTokenService
{
    public IssuedPreviewToken Issue(Guid poemId)
    {
        var expiresAt = timeProvider.GetUtcNow() + options.LinkLifetime;
        var payload = Payload(poemId, expiresAt);
        return new IssuedPreviewToken($"{payload}.{Sign(payload)}", expiresAt);
    }

    public bool Validate(Guid poemId, string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        var (idPart, expiryPart, signaturePart) = (parts[0], parts[1], parts[2]);
        if (!Guid.TryParse(idPart, out var tokenPoemId) || tokenPoemId != poemId)
        {
            return false;
        }

        if (!long.TryParse(expiryPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expirySeconds))
        {
            return false;
        }

        var expectedSignature = Sign($"{idPart}.{expiryPart}");
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(signaturePart)))
        {
            return false;
        }

        return DateTimeOffset.FromUnixTimeSeconds(expirySeconds) > timeProvider.GetUtcNow();
    }

    private static string Payload(Guid poemId, DateTimeOffset expiresAt) =>
        $"{poemId:N}.{expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}";

    private string Sign(string payload)
    {
        var signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(options.SigningKey), Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(signature).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
