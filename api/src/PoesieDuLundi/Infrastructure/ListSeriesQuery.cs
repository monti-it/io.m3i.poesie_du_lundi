using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure;

/// <summary>The read side of the admin series picker (issue #22's editor needs every series'
/// id/title to populate it) — an <c>AsNoTracking()</c> query projected straight to
/// <see cref="SeriesSummary"/>, not a method on a repository
/// (docs/ENGINEERING_PRACTICES.md "Read side vs. write side").</summary>
public sealed class ListSeriesQuery(PoesieDuLundiDbContext dbContext)
{
    public async Task<IReadOnlyCollection<SeriesSummary>> HandleAsync(CancellationToken cancellationToken)
    {
        var series = await dbContext.Series.AsNoTracking()
            .OrderBy(series => series.Order)
            .ThenBy(series => series.Title)
            .ToListAsync(cancellationToken);

        return series.Select(s => new SeriesSummary(s.Id, s.Title, s.Slug.Value)).ToList();
    }
}
