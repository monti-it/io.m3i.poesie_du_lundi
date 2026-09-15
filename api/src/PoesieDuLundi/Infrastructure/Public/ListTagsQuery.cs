using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>Every tag in use across published poems, with how many poems carry it — backs
/// <c>GET /api/tags</c>. Grouping across the jsonb-backed Tags collection doesn't translate
/// against Npgsql (the same gotcha <see cref="ListPublishedPoemsQuery"/> works around), so this
/// materialises the (small) published corpus's tags first and groups client-side.</summary>
public sealed class ListTagsQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<IReadOnlyCollection<TagCount>> HandleAsync(CancellationToken cancellationToken)
    {
        var published = dbContext.Poems.AsNoTracking().Where(PublishedPoems.AsOf(Clock.Today(timeProvider)));

        var tagLists = await published.Select(poem => poem.Tags).ToListAsync(cancellationToken);

        return tagLists
            .SelectMany(tags => tags)
            .GroupBy(tag => tag.Value, StringComparer.Ordinal)
            .Select(group => new TagCount(group.Key, group.Count()))
            .OrderBy(tagCount => tagCount.Tag, StringComparer.Ordinal)
            .ToList();
    }
}
