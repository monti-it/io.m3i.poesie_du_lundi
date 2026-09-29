using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>One published poem picked at random, for the side pane's "Au hasard" section (issue
/// #96) — skipping the <paramref name="exclude"/>d slugs (this Monday's poem, the one being viewed,
/// the recent ones) when that still leaves something to pick, falling back to the whole published
/// corpus otherwise.</summary>
public sealed class GetRandomPoemQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<PublicPoemSummary?> HandleAsync(
        IReadOnlyCollection<string> exclude, CancellationToken cancellationToken)
    {
        // Materialise the (small) published corpus and pick client-side — there's no random
        // ordering that translates to both Npgsql and the InMemory provider the tests run on.
        var published = await dbContext.Poems.AsNoTracking()
            .Where(PublishedPoems.AsOf(Clock.Today(timeProvider)))
            .Select(poem => new PublicPoemSummary(poem.Id, poem.Title, poem.Slug.Value, poem.PublicationDate!.Value))
            .ToListAsync(cancellationToken);

        var candidates = published.Where(poem => !exclude.Contains(poem.Slug)).ToList();
        if (candidates.Count == 0)
        {
            candidates = published;
        }

        return candidates.Count == 0 ? null : candidates[Random.Shared.Next(candidates.Count)];
    }
}
