using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>Published poems for the RSS/Atom/JSON feeds — newest first, capped the same way the
/// poem index page-sizes (<see cref="ListPublishedPoemsQuery"/>'s <c>MaxPageSize</c>), since a
/// feed reader wants the recent run, not the whole archive.</summary>
public sealed class ListFeedPoemsQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    private const int MaxItems = 50;

    public async Task<IReadOnlyList<FeedPoem>> HandleAsync(CancellationToken cancellationToken)
    {
        var poems = await dbContext.Poems.AsNoTracking()
            .Where(PublishedPoems.AsOf(Clock.Today(timeProvider)))
            .OrderByDescending(poem => poem.PublicationDate)
            .Take(MaxItems)
            .Select(poem => new
            {
                poem.Id, poem.Title, poem.Body, Slug = poem.Slug.Value, PublicationDate = poem.PublicationDate!.Value,
            })
            .ToListAsync(cancellationToken);

        return poems
            .Select(poem => new FeedPoem(
                poem.Id, poem.Title, MarkdownRenderer.ToHtml(poem.Body), poem.Slug, poem.PublicationDate))
            .ToList();
    }
}
