namespace PoesieDuLundi.Api.Public;

/// <summary>The request's own origin as an absolute URL — shared by every endpoint that has to
/// emit absolute links (feeds, sitemap, robots.txt). Same-origin by design
/// (docs/ENGINEERING_PRACTICES.md "Identity, tenancy, and public vs admin") — nginx forwards the
/// original Host and sets X-Forwarded-Proto (frontend/nginx.conf), so reading it directly here is
/// enough without a full ForwardedHeaders middleware/trusted-proxy setup.</summary>
internal static class AbsoluteUrl
{
    public static string BaseUrl(HttpRequest request)
    {
        var scheme = request.Headers.TryGetValue("X-Forwarded-Proto", out var forwardedProto)
            ? forwardedProto.ToString()
            : request.Scheme;
        return $"{scheme}://{request.Host}";
    }
}
