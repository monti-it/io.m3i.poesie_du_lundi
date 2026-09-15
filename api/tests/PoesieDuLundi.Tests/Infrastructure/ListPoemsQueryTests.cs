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
}
