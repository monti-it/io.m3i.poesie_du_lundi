using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class ListTagsQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Counts_tags_across_published_poems()
    {
        await using var dbContext = CreateDbContext();
        var first = new Poem("Poème un", "Un corps.");
        first.ChangeTags([new Slug("amour"), new Slug("hiver")]);
        first.Schedule(new DateOnly(2026, 3, 2));
        first.Publish();
        var second = new Poem("Poème deux", "Un corps.");
        second.ChangeTags([new Slug("amour")]);
        second.Schedule(new DateOnly(2026, 3, 9));
        second.Publish();
        await dbContext.Poems.AddRangeAsync(first, second);
        await dbContext.SaveChangesAsync();
        var query = new ListTagsQuery(dbContext, new FakeTimeProvider(Now));

        var tags = await query.HandleAsync(CancellationToken.None);

        Assert.Equal(2, tags.Count);
        Assert.Contains(tags, tag => tag.Tag == "amour" && tag.Count == 2);
        Assert.Contains(tags, tag => tag.Tag == "hiver" && tag.Count == 1);
    }

    [Fact]
    public async Task Excludes_tags_from_drafts_and_future_scheduled_poems()
    {
        await using var dbContext = CreateDbContext();
        var draft = new Poem("Brouillon", "Un corps.");
        draft.ChangeTags([new Slug("brouillon-tag")]);
        var future = new Poem("Futur", "Un corps.");
        future.ChangeTags([new Slug("futur-tag")]);
        future.Schedule(new DateOnly(2026, 9, 28));
        await dbContext.Poems.AddRangeAsync(draft, future);
        await dbContext.SaveChangesAsync();
        var query = new ListTagsQuery(dbContext, new FakeTimeProvider(Now));

        var tags = await query.HandleAsync(CancellationToken.None);

        Assert.Empty(tags);
    }
}
