using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>The current week's poem — the one scheduled/published for this week's Monday — falling
/// back to the most recently published poem when nothing is scheduled this week yet
/// (docs/ARCHITECTURE.md "Publishing model": poems are always dated a Monday, so "this week's
/// Monday" identifies at most one).</summary>
public sealed class GetThisMondayPoemQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<PublicPoem?> HandleAsync(CancellationToken cancellationToken)
    {
        var today = Clock.Today(timeProvider);
        var monday = today.AddDays(-(((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7));

        var query = dbContext.Poems.AsNoTracking().Where(PublishedPoems.AsOf(today));

        var poem = await query.SingleOrDefaultAsync(poem => poem.PublicationDate == monday, cancellationToken)
            ?? await query.OrderByDescending(poem => poem.PublicationDate).FirstOrDefaultAsync(cancellationToken);

        return poem is null ? null : await PublicPoemProjection.BuildAsync(dbContext, poem, cancellationToken);
    }
}
