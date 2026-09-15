using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>A series and its published poems, oldest first — the reading order of the cycle
/// (<c>Series</c> carries no ordering of its own poems, see the note on the aggregate).</summary>
public sealed class GetSeriesBySlugQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<SeriesWithPoems?> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        Slug parsedSlug;
        try
        {
            parsedSlug = new Slug(slug);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var series = await dbContext.Series.AsNoTracking()
            .SingleOrDefaultAsync(series => series.Slug == parsedSlug, cancellationToken);
        if (series is null)
        {
            return null;
        }

        var poems = await dbContext.Poems.AsNoTracking()
            .Where(PublishedPoems.AsOf(Clock.Today(timeProvider)))
            .Where(poem => poem.SeriesId == series.Id)
            .OrderBy(poem => poem.PublicationDate)
            .Select(poem => new PublicPoemSummary(poem.Id, poem.Title, poem.Slug.Value, poem.PublicationDate!.Value))
            .ToListAsync(cancellationToken);

        return new SeriesWithPoems(series.Id, series.Title, series.Slug.Value, series.Description, poems);
    }
}
