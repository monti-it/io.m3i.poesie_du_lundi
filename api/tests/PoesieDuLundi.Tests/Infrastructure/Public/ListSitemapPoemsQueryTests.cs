using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class ListSitemapPoemsQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Excludes_drafts_and_poems_scheduled_for_the_future()
    {
        await using var dbContext = CreateDbContext();
        var draft = new Poem("Brouillon", "Un corps.");
        var futureScheduled = new Poem("Futur", "Un corps.");
        futureScheduled.Schedule(new DateOnly(2026, 9, 28));
        var published = new Poem("Publié", "Un corps.");
        published.Schedule(new DateOnly(2026, 9, 14));
        published.Publish();
        await dbContext.Poems.AddRangeAsync(draft, futureScheduled, published);
        await dbContext.SaveChangesAsync();
        var query = new ListSitemapPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(published.Slug.Value, item.Slug);
    }

    [Fact]
    public async Task Returns_every_published_poem_not_just_a_recent_run()
    {
        await using var dbContext = CreateDbContext();
        for (var i = 0; i < 60; i++)
        {
            var poem = new Poem($"Poème {i}", "Un corps.");
            poem.Schedule(new DateOnly(2020, 1, 1).AddDays(i * 7));
            poem.Publish();
            await dbContext.Poems.AddAsync(poem);
        }

        await dbContext.SaveChangesAsync();
        var query = new ListSitemapPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        Assert.Equal(60, result.Count);
    }
}
