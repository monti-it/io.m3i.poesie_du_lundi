namespace PoesieDuLundi.Api.Admin;

/// <summary>
/// Resolves the current author from the <c>Remote-User</c> header that Traefik's
/// <c>forwardAuth</c> middleware sets — after verifying the session with Authelia
/// (<c>auth.m3i.io</c>, owned by the sibling io.m3i.auth repo, referenced cross-namespace from
/// <c>k8s/ingress.yaml</c>) — on requests reaching <c>/admin</c> and <c>/api/admin</c> only.
/// Listed in that Middleware's <c>authResponseHeaders</c>, so Traefik overwrites any
/// client-supplied value and the header can be trusted here.
///
/// This app has one shared content space, not tenant-per-user (docs/ENGINEERING_PRACTICES.md,
/// "Identity, tenancy, and public vs admin") — the resolved subject is authoring attribution
/// only, never a data-isolation filter.
/// </summary>
public sealed class ForwardAuthIdentityProvider(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment,
    IConfiguration configuration)
{
    public const string UserHeader = "Remote-User";

    /// <summary>
    /// The resolved author subject, or <see langword="null"/> when the request carries no
    /// identity — outside <c>Development</c>, callers must treat a <see langword="null"/> result
    /// as unauthenticated rather than falling back to a shared identity.
    /// </summary>
    public string? GetCurrentSubject()
    {
        var subject = httpContextAccessor.HttpContext?.Request.Headers[UserHeader].ToString();
        if (!string.IsNullOrWhiteSpace(subject))
        {
            return subject;
        }

        // A local `dotnet run` has no forwardAuth gate in front of it, so the header is absent —
        // fall back to a fixed dev subject to keep the inner loop working with zero setup. Never
        // outside Development: a missing header there is a misrouted or unauthenticated request.
        return environment.IsDevelopment()
            ? configuration["Auth:DevSubject"] ?? "dev@localhost"
            : null;
    }
}
