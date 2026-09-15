using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>Builds a <see cref="PublicPoem"/> from a loaded <see cref="Poem"/>, resolving its
/// <see cref="SeriesLink"/> with a second query — shared by every query object that returns a
/// single full poem, since <c>Poem</c> carries no navigation to <c>Series</c> (docs on
/// <see cref="Series"/>).</summary>
internal static class PublicPoemProjection
{
    public static async Task<PublicPoem> BuildAsync(
        PoesieDuLundiDbContext dbContext, Poem poem, CancellationToken cancellationToken)
    {
        SeriesLink? seriesLink = null;
        if (poem.SeriesId is { } seriesId)
        {
            seriesLink = await dbContext.Series.AsNoTracking()
                .Where(series => series.Id == seriesId)
                .Select(series => new SeriesLink(series.Id, series.Title, series.Slug.Value))
                .SingleOrDefaultAsync(cancellationToken);
        }

        return new PublicPoem(
            poem.Id, poem.Title, poem.Body, poem.Slug.Value, poem.PublicationDate!.Value, seriesLink);
    }
}
