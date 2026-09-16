using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Api.Public;

public sealed record SeriesLinkDto(Guid Id, string Title, string Slug);

public sealed record PublicPoemSummaryDto(Guid Id, string Title, string Slug, DateOnly PublicationDate);

public sealed record PublicPoemNeighborDto(string Slug, string Title);

public sealed record PublicPoemDto(
    Guid Id, string Title, string Body, string Slug, DateOnly PublicationDate, SeriesLinkDto? Series,
    PublicPoemNeighborDto? Previous, PublicPoemNeighborDto? Next);

public sealed record PagedPoemsDto(
    IReadOnlyCollection<PublicPoemSummaryDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>One endpoint per public poem-reading use case (issue #19) — thin handlers over the
/// <see cref="Infrastructure.Public"/> read models, every route anonymous
/// (docs/ENGINEERING_PRACTICES.md "Identity, tenancy, and public vs admin").</summary>
public static class PublicPoemEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;
    private const string SiteName = "La poésie du lundi";

    // The default HtmlEncoder only leaves Basic Latin unescaped, turning every accented letter
    // into a numeric entity — technically valid but noisy in view-source. The page declares
    // charset=utf-8, so letting the rest of Unicode through unescaped (still HTML-special chars
    // <>&"' are encoded) keeps this readable and matching the plain UTF-8 PoemHead emits client-side.
    private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

    public static IEndpointRouteBuilder MapPublicPoems(this IEndpointRouteBuilder api)
    {
        var poems = api.MapGroup("/poems").WithTags("Public Poems");

        poems.MapGet("/", async (
                int? page, int? pageSize, string? tag, ListPublishedPoemsQuery query,
                CancellationToken cancellationToken) =>
            {
                var result = await query.HandleAsync(
                    Math.Max(page ?? 1, 1), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize), tag,
                    cancellationToken);
                return Results.Ok(ToDto(result));
            })
            .Produces<PagedPoemsDto>();

        // Registered before "/{slug}" so a literal "this-monday" request can't be mistaken for a
        // slug — ASP.NET Core's routing already prefers the literal segment either way, but the
        // order documents the intent.
        poems.MapGet("/this-monday", async (GetThisMondayPoemQuery query, CancellationToken cancellationToken) =>
            {
                var poem = await query.HandleAsync(cancellationToken);
                return poem is null ? Results.NotFound(new ErrorDto("No poem has been published yet.")) : Results.Ok(ToDto(poem));
            })
            .Produces<PublicPoemDto>()
            .Produces<ErrorDto>(StatusCodes.Status404NotFound);

        poems.MapGet("/{slug}", async (
                string slug, string? preview, GetPublishedPoemBySlugQuery query,
                CancellationToken cancellationToken) =>
            {
                var poem = await query.HandleAsync(slug, preview, cancellationToken);
                return poem is null ? Results.NotFound(new ErrorDto("Poem not found.")) : Results.Ok(ToDto(poem));
            })
            .Produces<PublicPoemDto>()
            .Produces<ErrorDto>(StatusCodes.Status404NotFound);

        // Server-rendered OpenGraph/Twitter HTML for a single poem (issue #83) — link-unfurling
        // bots (Twitter/X, Facebook, Slack, Discord, …) read meta tags straight out of the initial
        // HTML and never execute JS, so `PoemPage`'s React-hoisted tags (docs/ARCHITECTURE.md
        // "Feeds & SEO") never reach them. `frontend/nginx.conf` routes those User-Agents to this
        // route instead of the SPA for `/poems/{slug}`; everyone else keeps getting the SPA.
        poems.MapGet("/{slug}/unfurl", async (
                HttpContext httpContext, string slug, string? preview, GetPublishedPoemBySlugQuery query,
                CancellationToken cancellationToken) =>
            {
                var poem = await query.HandleAsync(slug, preview, cancellationToken);
                if (poem is null)
                {
                    return Results.NotFound(new ErrorDto("Poem not found."));
                }

                var baseUrl = AbsoluteUrl.BaseUrl(httpContext.Request);
                var html = BuildUnfurlHtml(poem, baseUrl, isPreview: !string.IsNullOrEmpty(preview));
                return Results.Text(html, "text/html");
            })
            .Produces<string>()
            .Produces<ErrorDto>(StatusCodes.Status404NotFound);

        return api;
    }

    private static string BuildUnfurlHtml(PublicPoem poem, string baseUrl, bool isPreview)
    {
        var canonicalUrl = $"{baseUrl}/poems/{poem.Slug}";
        var description = Excerpt.From(poem.Body);
        var title = Html.Encode($"{poem.Title} — {SiteName}");
        var encodedPoemTitle = Html.Encode(poem.Title);
        var encodedDescription = Html.Encode(description);
        var encodedSiteName = Html.Encode(SiteName);
        var encodedCanonicalUrl = Html.Encode(canonicalUrl);
        var robotsMeta = isPreview ? "<meta name=\"robots\" content=\"noindex, nofollow\">\n    " : "";
        // Left on the default JavaScriptEncoder (not the permissive Html one above) — it escapes
        // '<', '>', '&' as \uXXXX, which is what keeps a poem title containing "</script>" from
        // breaking out of the <script> block below.
        var jsonLd = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "CreativeWork",
            ["name"] = poem.Title,
            ["url"] = canonicalUrl,
            ["datePublished"] = poem.PublicationDate.ToString("yyyy-MM-dd"),
            ["description"] = description,
            ["isPartOf"] = new Dictionary<string, object?>
            {
                ["@type"] = "WebSite", ["name"] = SiteName, ["url"] = baseUrl,
            },
        });

        return $"""
            <!doctype html>
            <html lang="fr">
            <head>
            <meta charset="utf-8">
            <title>{title}</title>
            {robotsMeta}<meta name="description" content="{encodedDescription}">
            <link rel="canonical" href="{encodedCanonicalUrl}">
            <meta property="og:type" content="article">
            <meta property="og:site_name" content="{encodedSiteName}">
            <meta property="og:title" content="{encodedPoemTitle}">
            <meta property="og:description" content="{encodedDescription}">
            <meta property="og:url" content="{encodedCanonicalUrl}">
            <meta name="twitter:card" content="summary">
            <meta name="twitter:title" content="{encodedPoemTitle}">
            <meta name="twitter:description" content="{encodedDescription}">
            <script type="application/ld+json">{jsonLd}</script>
            </head>
            <body></body>
            </html>
            """;
    }

    private static PagedPoemsDto ToDto(PagedPoems paged) => new(
        paged.Items.Select(ToDto).ToList(), paged.Page, paged.PageSize, paged.TotalCount);

    internal static PublicPoemSummaryDto ToDto(PublicPoemSummary summary) =>
        new(summary.Id, summary.Title, summary.Slug, summary.PublicationDate);

    private static PublicPoemDto ToDto(PublicPoem poem) => new(
        poem.Id, poem.Title, poem.Body, poem.Slug, poem.PublicationDate,
        poem.Series is null ? null : new SeriesLinkDto(poem.Series.Id, poem.Series.Title, poem.Series.Slug),
        poem.Previous is null ? null : new PublicPoemNeighborDto(poem.Previous.Slug, poem.Previous.Title),
        poem.Next is null ? null : new PublicPoemNeighborDto(poem.Next.Slug, poem.Next.Title));
}
