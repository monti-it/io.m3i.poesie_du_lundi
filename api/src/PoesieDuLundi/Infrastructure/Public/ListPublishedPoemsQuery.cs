using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>The public poem index — published poems, newest first, paged, optionally narrowed to
/// one tag (docs/ENGINEERING_PRACTICES.md "Read side vs. write side").</summary>
public sealed class ListPublishedPoemsQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<PagedPoems> HandleAsync(
        int page, int pageSize, string? tag, CancellationToken cancellationToken)
    {
        var query = dbContext.Poems.AsNoTracking().Where(PublishedPoems.AsOf(Clock.Today(timeProvider)));

        if (tag is null)
        {
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(poem => poem.PublicationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(poem => new PublicPoemSummary(poem.Id, poem.Title, poem.Slug.Value, poem.PublicationDate!.Value))
                .ToListAsync(cancellationToken);

            return new PagedPoems(items, page, pageSize, totalCount);
        }

        // Membership on the jsonb-backed Tags collection doesn't translate against Npgsql (the
        // "HasConversion'd collection property" gotcha docs/ENGINEERING_PRACTICES.md warns about) —
        // materialise the (small) published corpus and filter client-side, mirroring GetArchiveQuery.
        var published = await query
            .OrderByDescending(poem => poem.PublicationDate)
            .Select(poem => new
            {
                poem.Id, poem.Title, Slug = poem.Slug.Value, PublicationDate = poem.PublicationDate!.Value,
                poem.Tags,
            })
            .ToListAsync(cancellationToken);

        var matching = published
            .Where(poem => poem.Tags.Any(t => t.Value == tag))
            .ToList();
        var pagedMatches = matching
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(poem => new PublicPoemSummary(poem.Id, poem.Title, poem.Slug, poem.PublicationDate))
            .ToList();

        return new PagedPoems(pagedMatches, page, pageSize, matching.Count);
    }
}
