using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>Builds a <see cref="PublicPoem"/> from a loaded <see cref="Poem"/>, resolving its
/// <see cref="SeriesLink"/> and prev/next neighbours with further queries — shared by every query
/// object that returns a single full poem, since <c>Poem</c> carries no navigation to
/// <c>Series</c> (docs on <see cref="Series"/>) or to its publication-date neighbours.</summary>
internal static class PublicPoemProjection
{
    public static async Task<PublicPoem> BuildAsync(
        PoesieDuLundiDbContext dbContext, Poem poem, DateOnly today, CancellationToken cancellationToken)
    {
        SeriesLink? seriesLink = null;
        if (poem.SeriesId is { } seriesId)
        {
            seriesLink = await dbContext.Series.AsNoTracking()
                .Where(series => series.Id == seriesId)
                .Select(series => new SeriesLink(series.Id, series.Title, series.Slug.Value))
                .SingleOrDefaultAsync(cancellationToken);
        }

        var published = dbContext.Poems.AsNoTracking().Where(PublishedPoems.AsOf(today));

        var previous = await published
            .Where(other => other.PublicationDate < poem.PublicationDate)
            .OrderByDescending(other => other.PublicationDate)
            .Select(other => new PublicPoemNeighbor(other.Slug.Value, other.Title))
            .FirstOrDefaultAsync(cancellationToken);

        var next = await published
            .Where(other => other.PublicationDate > poem.PublicationDate)
            .OrderBy(other => other.PublicationDate)
            .Select(other => new PublicPoemNeighbor(other.Slug.Value, other.Title))
            .FirstOrDefaultAsync(cancellationToken);

        return new PublicPoem(
            poem.Id, poem.Title, poem.Body, poem.Slug.Value, poem.PublicationDate!.Value, seriesLink,
            previous, next);
    }
}
