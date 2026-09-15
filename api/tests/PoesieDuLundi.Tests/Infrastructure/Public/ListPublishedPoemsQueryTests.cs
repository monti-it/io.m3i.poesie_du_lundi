using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class ListPublishedPoemsQueryTests
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
        var query = new ListPublishedPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(1, 20, null, CancellationToken.None);

        var summary = Assert.Single(result.Items);
        Assert.Equal(published.Id, summary.Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Includes_a_scheduled_poem_whose_date_has_arrived()
    {
        await using var dbContext = CreateDbContext();
        var due = new Poem("Dû", "Un corps.");
        due.Schedule(new DateOnly(2026, 9, 21));
        await dbContext.Poems.AddAsync(due);
        await dbContext.SaveChangesAsync();
        var query = new ListPublishedPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(1, 20, null, CancellationToken.None);

        Assert.Contains(result.Items, poem => poem.Id == due.Id);
    }

    [Fact]
    public async Task Orders_newest_first_and_pages()
    {
        await using var dbContext = CreateDbContext();
        var older = new Poem("Plus ancien", "Un corps.");
        older.Schedule(new DateOnly(2026, 9, 7));
        older.Publish();
        var newer = new Poem("Plus récent", "Un corps.");
        newer.Schedule(new DateOnly(2026, 9, 14));
        newer.Publish();
        await dbContext.Poems.AddRangeAsync(older, newer);
        await dbContext.SaveChangesAsync();
        var query = new ListPublishedPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var firstPage = await query.HandleAsync(1, 1, null, CancellationToken.None);
        var secondPage = await query.HandleAsync(2, 1, null, CancellationToken.None);

        Assert.Equal(newer.Id, Assert.Single(firstPage.Items).Id);
        Assert.Equal(older.Id, Assert.Single(secondPage.Items).Id);
        Assert.Equal(2, firstPage.TotalCount);
    }

    [Fact]
    public async Task Filters_by_tag()
    {
        await using var dbContext = CreateDbContext();
        var tagged = new Poem("Étiqueté", "Un corps.");
        tagged.ChangeTags([new Slug("amour")]);
        tagged.Schedule(new DateOnly(2026, 9, 14));
        tagged.Publish();
        var untagged = new Poem("Sans étiquette", "Un corps.");
        untagged.Schedule(new DateOnly(2026, 9, 7));
        untagged.Publish();
        await dbContext.Poems.AddRangeAsync(tagged, untagged);
        await dbContext.SaveChangesAsync();
        var query = new ListPublishedPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(1, 20, "amour", CancellationToken.None);

        var summary = Assert.Single(result.Items);
        Assert.Equal(tagged.Id, summary.Id);
        Assert.Equal(1, result.TotalCount);
    }
}
