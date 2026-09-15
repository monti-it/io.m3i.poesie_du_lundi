using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>One published poem by slug — 404-shaped as <c>null</c> for a draft/scheduled-future
/// poem or a malformed slug alike, never a thrown exception for the latter (a value object still
/// validates itself once, at construction — it just isn't the caller's problem here).</summary>
public sealed class GetPublishedPoemBySlugQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<PublicPoem?> HandleAsync(string slug, CancellationToken cancellationToken)
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

        var poem = await dbContext.Poems.AsNoTracking()
            .Where(PublishedPoems.AsOf(Clock.Today(timeProvider)))
            .SingleOrDefaultAsync(poem => poem.Slug == parsedSlug, cancellationToken);

        return poem is null ? null : await PublicPoemProjection.BuildAsync(dbContext, poem, cancellationToken);
    }
}
