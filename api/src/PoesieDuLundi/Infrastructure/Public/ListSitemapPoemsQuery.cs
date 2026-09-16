using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>Every published poem's slug and publication date — <c>sitemap.xml</c>'s one <c>&lt;url&gt;</c>
/// per poem, unlike <see cref="ListFeedPoemsQuery"/> this is uncapped (a sitemap has to list the
/// whole indexable corpus, not just the recent run a feed reader wants).</summary>
public sealed class ListSitemapPoemsQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<SitemapPoem>> HandleAsync(CancellationToken cancellationToken) =>
        await dbContext.Poems.AsNoTracking()
            .Where(PublishedPoems.AsOf(Clock.Today(timeProvider)))
            .OrderByDescending(poem => poem.PublicationDate)
            .Select(poem => new SitemapPoem(poem.Slug.Value, poem.PublicationDate!.Value))
            .ToListAsync(cancellationToken);
}

public sealed record SitemapPoem(string Slug, DateOnly PublicationDate);
