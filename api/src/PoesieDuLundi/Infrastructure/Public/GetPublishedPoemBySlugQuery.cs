using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Application;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>One published poem by slug — 404-shaped as <c>null</c> for a draft/scheduled-future
/// poem or a malformed slug alike, never a thrown exception for the latter (a value object still
/// validates itself once, at construction — it just isn't the caller's problem here). A valid
/// <paramref name="previewToken"/> (issue #25) bypasses the published check for that one poem, so
/// an author can share a not-yet-live poem for review.</summary>
public sealed class GetPublishedPoemBySlugQuery(
    PoesieDuLundiDbContext dbContext, TimeProvider timeProvider, IPreviewTokenService previewTokenService)
{
    public async Task<PublicPoem?> HandleAsync(string slug, string? previewToken, CancellationToken cancellationToken)
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

        var today = Clock.Today(timeProvider);

        if (!string.IsNullOrEmpty(previewToken))
        {
            var previewed = await dbContext.Poems.AsNoTracking()
                .SingleOrDefaultAsync(poem => poem.Slug == parsedSlug, cancellationToken);
            if (previewed is { PublicationDate: not null } && previewTokenService.Validate(previewed.Id, previewToken))
            {
                return await PublicPoemProjection.BuildAsync(dbContext, previewed, today, cancellationToken);
            }
        }

        var poem = await dbContext.Poems.AsNoTracking()
            .Where(PublishedPoems.AsOf(today))
            .SingleOrDefaultAsync(poem => poem.Slug == parsedSlug, cancellationToken);

        return poem is null ? null : await PublicPoemProjection.BuildAsync(dbContext, poem, today, cancellationToken);
    }
}
