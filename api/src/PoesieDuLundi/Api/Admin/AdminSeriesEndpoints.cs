using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Api.Admin;

public sealed record SeriesSummaryDto(Guid Id, string Title, string Slug);

/// <summary>Read-only series listing for the admin editor's series picker (issue #22) — series are
/// themselves seeded outside this app for now (no admin series CRUD yet), so this is the only
/// admin series route.</summary>
public static class AdminSeriesEndpoints
{
    public static IEndpointRouteBuilder MapAdminSeries(this IEndpointRouteBuilder admin)
    {
        admin.MapGet("/series", async (ListSeriesQuery query, CancellationToken cancellationToken) =>
            {
                var series = await query.HandleAsync(cancellationToken);
                return Results.Ok(series.Select(ToDto));
            })
            .WithTags("Admin Series")
            .Produces<IReadOnlyCollection<SeriesSummaryDto>>();

        return admin;
    }

    private static SeriesSummaryDto ToDto(SeriesSummary summary) =>
        new(summary.Id, summary.Title, summary.Slug);
}
