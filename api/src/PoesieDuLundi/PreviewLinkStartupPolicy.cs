using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi;

/// <summary>
/// Resolves <see cref="PreviewLinkOptions"/> at startup. The signing key must come from
/// <c>Preview:SigningKey</c> outside Development/Testing — a well-known default key in production
/// would let anyone forge a preview link — so a deployed environment without one fails fast rather
/// than silently signing with a public default. Development and the WebApplicationFactory-based
/// test host (<c>Testing</c>) fall back to a fixed key, mirroring
/// <see cref="PoesieDuLundi.Api.Admin.ForwardAuthIdentityProvider"/>'s <c>Auth:DevSubject</c>
/// fallback, to keep the inner loop working with zero setup.
/// </summary>
public static class PreviewLinkStartupPolicy
{
    private const string DevSigningKey = "dev-preview-signing-key-not-for-production";

    public static PreviewLinkOptions GetOptions(IHostEnvironment environment, IConfiguration configuration)
    {
        var signingKey = configuration["Preview:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            {
                throw new InvalidOperationException(
                    "Preview:SigningKey must be configured outside Development/Testing.");
            }

            signingKey = DevSigningKey;
        }

        var lifetime = configuration.GetValue("Preview:LinkLifetime", PreviewLinkOptions.DefaultLinkLifetime);
        return new PreviewLinkOptions(signingKey, lifetime);
    }
}
