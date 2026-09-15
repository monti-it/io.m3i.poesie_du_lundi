using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Api.Public;

public sealed record SeriesLinkDto(Guid Id, string Title, string Slug);

public sealed record PublicPoemSummaryDto(Guid Id, string Title, string Slug, DateOnly PublicationDate);

public sealed record PublicPoemDto(
    Guid Id, string Title, string Body, string Slug, DateOnly PublicationDate, SeriesLinkDto? Series);

public sealed record PagedPoemsDto(
    IReadOnlyCollection<PublicPoemSummaryDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>One endpoint per public poem-reading use case (issue #19) — thin handlers over the
/// <see cref="Infrastructure.Public"/> read models, every route anonymous
/// (docs/ENGINEERING_PRACTICES.md "Identity, tenancy, and public vs admin").</summary>
public static class PublicPoemEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    public static IEndpointRouteBuilder MapPublicPoems(this IEndpointRouteBuilder api)
    {
        var poems = api.MapGroup("/poems").WithTags("Public Poems");

        poems.MapGet("/", async (
                int? page, int? pageSize, ListPublishedPoemsQuery query, CancellationToken cancellationToken) =>
            {
                var result = await query.HandleAsync(
                    Math.Max(page ?? 1, 1), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize), cancellationToken);
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
                string slug, GetPublishedPoemBySlugQuery query, CancellationToken cancellationToken) =>
            {
                var poem = await query.HandleAsync(slug, cancellationToken);
                return poem is null ? Results.NotFound(new ErrorDto("Poem not found.")) : Results.Ok(ToDto(poem));
            })
            .Produces<PublicPoemDto>()
            .Produces<ErrorDto>(StatusCodes.Status404NotFound);

        return api;
    }

    private static PagedPoemsDto ToDto(PagedPoems paged) => new(
        paged.Items.Select(ToDto).ToList(), paged.Page, paged.PageSize, paged.TotalCount);

    internal static PublicPoemSummaryDto ToDto(PublicPoemSummary summary) =>
        new(summary.Id, summary.Title, summary.Slug, summary.PublicationDate);

    private static PublicPoemDto ToDto(PublicPoem poem) => new(
        poem.Id, poem.Title, poem.Body, poem.Slug, poem.PublicationDate,
        poem.Series is null ? null : new SeriesLinkDto(poem.Series.Id, poem.Series.Title, poem.Series.Slug));
}
