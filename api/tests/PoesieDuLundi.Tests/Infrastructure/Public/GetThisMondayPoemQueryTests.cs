using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class GetThisMondayPoemQueryTests
{
    // A Wednesday, so "this week's Monday" (2026-09-21) differs from "today".
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 8, 0, 0, TimeSpan.Zero);

    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Returns_the_poem_scheduled_for_this_weeks_Monday()
    {
        await using var dbContext = CreateDbContext();
        var thisWeek = new Poem("Cette semaine", "Un corps.");
        thisWeek.Schedule(new DateOnly(2026, 9, 21));
        var lastWeek = new Poem("La semaine dernière", "Un corps.");
        lastWeek.Schedule(new DateOnly(2026, 9, 14));
        lastWeek.Publish();
        await dbContext.Poems.AddRangeAsync(thisWeek, lastWeek);
        await dbContext.SaveChangesAsync();
        var query = new GetThisMondayPoemQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(thisWeek.Id, result.Id);
    }

    [Fact]
    public async Task Falls_back_to_the_most_recent_poem_when_nothing_is_published_this_week()
    {
        await using var dbContext = CreateDbContext();
        var mostRecent = new Poem("Le plus récent", "Un corps.");
        mostRecent.Schedule(new DateOnly(2026, 9, 7));
        mostRecent.Publish();
        var older = new Poem("Plus ancien", "Un corps.");
        older.Schedule(new DateOnly(2026, 8, 31));
        older.Publish();
        await dbContext.Poems.AddRangeAsync(mostRecent, older);
        await dbContext.SaveChangesAsync();
        var query = new GetThisMondayPoemQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(mostRecent.Id, result.Id);
    }

    [Fact]
    public async Task Returns_null_when_nothing_has_ever_been_published()
    {
        await using var dbContext = CreateDbContext();
        var draft = new Poem("Brouillon", "Un corps.");
        await dbContext.Poems.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var query = new GetThisMondayPoemQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(CancellationToken.None);

        Assert.Null(result);
    }
}
