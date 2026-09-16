using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class ListFeedPoemsQueryTests
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
        var query = new ListFeedPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(published.Id, item.Id);
    }

    [Fact]
    public async Task Includes_a_scheduled_poem_whose_date_has_arrived()
    {
        await using var dbContext = CreateDbContext();
        var due = new Poem("Dû", "Un corps.");
        due.Schedule(new DateOnly(2026, 9, 21));
        await dbContext.Poems.AddAsync(due);
        await dbContext.SaveChangesAsync();
        var query = new ListFeedPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        Assert.Contains(result, poem => poem.Id == due.Id);
    }

    [Fact]
    public async Task Orders_newest_first()
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
        var query = new ListFeedPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], result.Select(poem => poem.Id));
    }

    [Fact]
    public async Task Renders_the_body_from_markdown_to_html()
    {
        await using var dbContext = CreateDbContext();
        var poem = new Poem("Publié", "Première ligne.\nDeuxième ligne.");
        poem.Schedule(new DateOnly(2026, 9, 14));
        poem.Publish();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
        var query = new ListFeedPoemsQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("<p>Première ligne.<br />\nDeuxième ligne.</p>\n", item.BodyHtml);
    }
}
