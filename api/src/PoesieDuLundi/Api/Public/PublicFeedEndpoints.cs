using System.ServiceModel.Syndication;
using System.Text.Json.Serialization;
using System.Xml;
using Microsoft.AspNetCore.OutputCaching;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Api.Public;

public sealed record JsonFeedItemDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content_html")] string ContentHtml,
    [property: JsonPropertyName("date_published")] DateTimeOffset DatePublished);

public sealed record JsonFeedDto(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("home_page_url")] string HomePageUrl,
    [property: JsonPropertyName("feed_url")] string FeedUrl,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("items")] IReadOnlyList<JsonFeedItemDto> Items);

/// <summary>
/// RSS 2.0 + Atom + JSON Feed of published poems (docs/ARCHITECTURE.md "Feeds & SEO") — first-class
/// MVP, not an afterthought. Responses carry the "Feeds" output-cache policy (<c>Program.cs</c>),
/// busted by <see cref="FeedCacheInvalidationHandler"/> whenever a poem is published or unpublished.
/// </summary>
public static class PublicFeedEndpoints
{
    private const string FeedTitle = "La poésie du lundi";
    private const string FeedDescription = "Un poème chaque lundi.";
    private const string CachePolicy = "Feeds";

    public static IEndpointRouteBuilder MapPublicFeeds(this IEndpointRouteBuilder api)
    {
        api.MapGet("/feed.xml", async (HttpContext httpContext, ListFeedPoemsQuery query, CancellationToken cancellationToken) =>
            {
                var feed = await BuildSyndicationFeedAsync(httpContext, query, cancellationToken);
                return WriteXml(feed, asAtom: false);
            })
            .WithTags("Public Feeds")
            .CacheOutput(CachePolicy);

        api.MapGet("/atom.xml", async (HttpContext httpContext, ListFeedPoemsQuery query, CancellationToken cancellationToken) =>
            {
                var feed = await BuildSyndicationFeedAsync(httpContext, query, cancellationToken);
                return WriteXml(feed, asAtom: true);
            })
            .WithTags("Public Feeds")
            .CacheOutput(CachePolicy);

        api.MapGet("/feed.json", async (HttpContext httpContext, ListFeedPoemsQuery query, CancellationToken cancellationToken) =>
            {
                var poems = await query.HandleAsync(cancellationToken);
                var baseUrl = AbsoluteUrl.BaseUrl(httpContext.Request);
                var jsonFeed = new JsonFeedDto(
                    "https://jsonfeed.org/version/1.1", FeedTitle, baseUrl, $"{baseUrl}/api/feed.json", FeedDescription,
                    poems.Select(poem => ToJsonFeedItem(poem, baseUrl)).ToList());
                return Results.Json(jsonFeed, contentType: "application/feed+json; charset=utf-8");
            })
            .WithTags("Public Feeds")
            .CacheOutput(CachePolicy);

        return api;
    }

    private static async Task<SyndicationFeed> BuildSyndicationFeedAsync(
        HttpContext httpContext, ListFeedPoemsQuery query, CancellationToken cancellationToken)
    {
        var poems = await query.HandleAsync(cancellationToken);
        var baseUrl = AbsoluteUrl.BaseUrl(httpContext.Request);
        var items = poems.Select(poem => ToSyndicationItem(poem, baseUrl)).ToList();

        var updated = poems.Count > 0
            ? PublishedAt(poems[0].PublicationDate)
            : DateTimeOffset.UtcNow;

        return new SyndicationFeed(FeedTitle, FeedDescription, new Uri(baseUrl), items)
        {
            Id = baseUrl,
            LastUpdatedTime = updated,
        };
    }

    private static SyndicationItem ToSyndicationItem(FeedPoem poem, string baseUrl)
    {
        var published = PublishedAt(poem.PublicationDate);
        var item = new SyndicationItem(
            poem.Title,
            new TextSyndicationContent(poem.BodyHtml, TextSyndicationContentKind.Html),
            new Uri($"{baseUrl}/poems/{poem.Slug}"),
            StableId(poem.Id),
            published)
        {
            PublishDate = published,
            LastUpdatedTime = published,
        };
        return item;
    }

    private static JsonFeedItemDto ToJsonFeedItem(FeedPoem poem, string baseUrl) => new(
        StableId(poem.Id), $"{baseUrl}/poems/{poem.Slug}", poem.Title, poem.BodyHtml, PublishedAt(poem.PublicationDate));

    // A stable, permanent identifier (RFC 4122's "urn:uuid:" namespace) built from the poem id
    // itself — valid both as an RSS <guid> (any opaque string) and an Atom/JSON Feed id (must be a
    // URI/IRI), so the same value serves all three formats.
    private static string StableId(Guid poemId) => $"urn:uuid:{poemId}";

    private static DateTimeOffset PublishedAt(DateOnly publicationDate) =>
        new(publicationDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static IResult WriteXml(SyndicationFeed feed, bool asAtom)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = System.Text.Encoding.UTF8, Indent = true };
        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            if (asAtom)
            {
                new Atom10FeedFormatter(feed).WriteTo(writer);
            }
            else
            {
                new Rss20FeedFormatter(feed).WriteTo(writer);
            }

            writer.WriteEndDocument();
        }

        var contentType = asAtom ? "application/atom+xml; charset=utf-8" : "application/rss+xml; charset=utf-8";
        return Results.Bytes(stream.ToArray(), contentType);
    }
}
