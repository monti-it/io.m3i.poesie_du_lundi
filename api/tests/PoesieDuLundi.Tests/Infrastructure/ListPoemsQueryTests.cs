using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Tests.Infrastructure;

public class ListPoemsQueryTests
{
    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Lists_every_poem_when_no_status_filter_is_given()
    {
        await using var dbContext = CreateDbContext();
        var draft = new Poem("Brouillon", "Un corps.");
        var scheduled = new Poem("Programmé", "Un corps.");
        scheduled.Schedule(new DateOnly(2026, 9, 21));
        await dbContext.Poems.AddRangeAsync(draft, scheduled);
        await dbContext.SaveChangesAsync();
        var query = new ListPoemsQuery(dbContext);

        var result = await query.HandleAsync(null, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Filters_by_status_when_given()
    {
        await using var dbContext = CreateDbContext();
        var draft = new Poem("Brouillon", "Un corps.");
        var scheduled = new Poem("Programmé", "Un corps.");
        scheduled.Schedule(new DateOnly(2026, 9, 21));
        await dbContext.Poems.AddRangeAsync(draft, scheduled);
        await dbContext.SaveChangesAsync();
        var query = new ListPoemsQuery(dbContext);

        var result = await query.HandleAsync(PoemStatus.Scheduled, CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.Equal(scheduled.Id, summary.Id);
    }

    [Fact]
    public async Task Orders_drafts_first_then_by_publication_date_newest_first_then_by_title()
    {
        await using var dbContext = CreateDbContext();
        var olderPublished = new Poem("Ancien", "Un corps.");
        olderPublished.Schedule(new DateOnly(2026, 9, 7));
        olderPublished.Publish();
        var newerScheduled = new Poem("Récent", "Un corps.");
        newerScheduled.Schedule(new DateOnly(2026, 9, 28));
        var sameDateB = new Poem("B même date", "Un corps.");
        sameDateB.Schedule(new DateOnly(2026, 9, 14));
        var sameDateA = new Poem("A même date", "Un corps.");
        sameDateA.Schedule(new DateOnly(2026, 9, 14));
        var draftZ = new Poem("Z brouillon", "Un corps.");
        var draftA = new Poem("A brouillon", "Un corps.");
        await dbContext.Poems.AddRangeAsync(olderPublished, draftZ, sameDateB, newerScheduled, draftA, sameDateA);
        await dbContext.SaveChangesAsync();
        var query = new ListPoemsQuery(dbContext);

        var result = await query.HandleAsync(null, CancellationToken.None);

        Assert.Equal(
            ["A brouillon", "Z brouillon", "Récent", "A même date", "B même date", "Ancien"],
            result.Select(summary => summary.Title));
    }

    [Fact]
    public async Task Keeps_the_order_when_filtering_by_status()
    {
        await using var dbContext = CreateDbContext();
        var older = new Poem("Ancien", "Un corps.");
        older.Schedule(new DateOnly(2026, 9, 7));
        var newer = new Poem("Récent", "Un corps.");
        newer.Schedule(new DateOnly(2026, 9, 28));
        await dbContext.Poems.AddRangeAsync(older, newer);
        await dbContext.SaveChangesAsync();
        var query = new ListPoemsQuery(dbContext);

        var result = await query.HandleAsync(PoemStatus.Scheduled, CancellationToken.None);

        Assert.Equal(["Récent", "Ancien"], result.Select(summary => summary.Title));
    }
}
