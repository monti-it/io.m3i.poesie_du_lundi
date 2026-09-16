using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.OutputCaching;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Api.Public;

/// <summary>
/// <c>sitemap.xml</c> + <c>robots.txt</c> (docs/ARCHITECTURE.md "Feeds & SEO"). Mapped at the
/// document root, not under <c>/api</c> — robots.txt is only honoured there by convention, and the
/// sitemap sits next to it for the same reason — so the public Ingress and frontend nginx.conf
/// route these two paths to the API specifically (see their routing comments).
/// </summary>
public static class PublicSeoEndpoints
{
    private const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private const string CachePolicy = "Feeds";

    public static IEndpointRouteBuilder MapPublicSeo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitemap.xml", async (
                HttpContext httpContext, ListSitemapPoemsQuery poemsQuery, ListSeriesQuery seriesQuery,
                CancellationToken cancellationToken) =>
            {
                var baseUrl = AbsoluteUrl.BaseUrl(httpContext.Request);
                var poems = await poemsQuery.HandleAsync(cancellationToken);
                var series = await seriesQuery.HandleAsync(cancellationToken);
                var document = BuildSitemap(baseUrl, poems, series);
                return WriteXml(document);
            })
            .WithTags("Public SEO")
            .CacheOutput(CachePolicy);

        app.MapGet("/robots.txt", (HttpContext httpContext) =>
            {
                var baseUrl = AbsoluteUrl.BaseUrl(httpContext.Request);
                var body = $"User-agent: *\nAllow: /\n\nSitemap: {baseUrl}/sitemap.xml\n";
                return Results.Text(body, "text/plain");
            })
            .WithTags("Public SEO");

        return app;
    }

    private static XDocument BuildSitemap(
        string baseUrl, IReadOnlyList<SitemapPoem> poems, IReadOnlyCollection<SeriesSummary> series)
    {
        XNamespace ns = SitemapNamespace;

        var staticUrls = new[] { "/", "/archive" }.Select(path => Url(ns, $"{baseUrl}{path}"));
        var poemUrls = poems.Select(poem =>
            Url(ns, $"{baseUrl}/poems/{poem.Slug}", poem.PublicationDate));
        var seriesUrls = series.Select(s => Url(ns, $"{baseUrl}/series/{s.Slug}"));

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "urlset", staticUrls.Concat(poemUrls).Concat(seriesUrls)));
    }

    private static XElement Url(XNamespace ns, string loc, DateOnly? lastmod = null)
    {
        var element = new XElement(ns + "url", new XElement(ns + "loc", loc));
        if (lastmod is { } date)
        {
            element.Add(new XElement(ns + "lastmod", date.ToString("yyyy-MM-dd")));
        }

        return element;
    }

    private static IResult WriteXml(XDocument document)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
        using (var writer = XmlWriter.Create(stream, settings))
        {
            document.WriteTo(writer);
        }

        return Results.Bytes(stream.ToArray(), "application/xml; charset=utf-8");
    }
}
