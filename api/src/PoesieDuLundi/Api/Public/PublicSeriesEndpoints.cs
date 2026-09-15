using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Api.Public;

public sealed record SeriesDto(
    Guid Id, string Title, string Slug, string? Description, IReadOnlyCollection<PublicPoemSummaryDto> Poems);

public static class PublicSeriesEndpoints
{
    public static IEndpointRouteBuilder MapPublicSeries(this IEndpointRouteBuilder api)
    {
        api.MapGet("/series/{slug}", async (
                string slug, GetSeriesBySlugQuery query, CancellationToken cancellationToken) =>
            {
                var series = await query.HandleAsync(slug, cancellationToken);
                return series is null
                    ? Results.NotFound(new ErrorDto("Series not found."))
                    : Results.Ok(ToDto(series));
            })
            .WithTags("Public Series")
            .Produces<SeriesDto>()
            .Produces<ErrorDto>(StatusCodes.Status404NotFound);

        return api;
    }

    private static SeriesDto ToDto(SeriesWithPoems series) => new(
        series.Id, series.Title, series.Slug, series.Description,
        series.Poems.Select(PublicPoemEndpoints.ToDto).ToList());
}
