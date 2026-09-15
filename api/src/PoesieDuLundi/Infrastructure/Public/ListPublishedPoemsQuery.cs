using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>The public poem index — published poems, newest first, paged
/// (docs/ENGINEERING_PRACTICES.md "Read side vs. write side").</summary>
public sealed class ListPublishedPoemsQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<PagedPoems> HandleAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Poems.AsNoTracking().Where(PublishedPoems.AsOf(Clock.Today(timeProvider)));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(poem => poem.PublicationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(poem => new PublicPoemSummary(poem.Id, poem.Title, poem.Slug.Value, poem.PublicationDate!.Value))
            .ToListAsync(cancellationToken);

        return new PagedPoems(items, page, pageSize, totalCount);
    }
}
