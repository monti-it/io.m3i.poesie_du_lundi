using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class GetArchiveQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedAsync(PoesieDuLundiDbContext dbContext, params DateOnly[] publicationDates)
    {
        foreach (var date in publicationDates)
        {
            var poem = new Poem($"Poème du {date}", "Un corps.");
            poem.Schedule(date);
            poem.Publish();
            await dbContext.Poems.AddAsync(poem);
        }

        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task With_no_year_returns_grouped_counts_and_no_entries()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 9), new DateOnly(2025, 12, 1));
        var query = new GetArchiveQuery(dbContext, new FakeTimeProvider(Now));

        var archive = await query.HandleAsync(null, null, CancellationToken.None);

        Assert.Empty(archive.Entries);
        Assert.Equal(2, archive.Groups.Count);
        Assert.Contains(archive.Groups, g => g is { Year: 2026, Month: 3, Count: 2 });
        Assert.Contains(archive.Groups, g => g is { Year: 2025, Month: 12, Count: 1 });
        // Newest year/month first.
        Assert.Equal(2026, archive.Groups.First().Year);
    }

    [Fact]
    public async Task With_a_year_returns_every_entry_in_that_year()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, new DateOnly(2026, 3, 2), new DateOnly(2026, 6, 1), new DateOnly(2025, 12, 1));
        var query = new GetArchiveQuery(dbContext, new FakeTimeProvider(Now));

        var archive = await query.HandleAsync(2026, null, CancellationToken.None);

        Assert.Equal(2, archive.Entries.Count);
        Assert.All(archive.Entries, entry => Assert.Equal(2026, entry.PublicationDate.Year));
    }

    [Fact]
    public async Task With_a_year_and_month_returns_only_that_months_entries()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 9), new DateOnly(2026, 6, 1));
        var query = new GetArchiveQuery(dbContext, new FakeTimeProvider(Now));

        var archive = await query.HandleAsync(2026, 3, CancellationToken.None);

        Assert.Equal(2, archive.Entries.Count);
        Assert.All(archive.Entries, entry => Assert.Equal(3, entry.PublicationDate.Month));
    }

    [Fact]
    public async Task Excludes_drafts_and_future_scheduled_poems_from_groups()
    {
        await using var dbContext = CreateDbContext();
        var draft = new Poem("Brouillon", "Un corps.");
        var future = new Poem("Futur", "Un corps.");
        future.Schedule(new DateOnly(2026, 9, 28));
        await dbContext.Poems.AddRangeAsync(draft, future);
        await dbContext.SaveChangesAsync();
        var query = new GetArchiveQuery(dbContext, new FakeTimeProvider(Now));

        var archive = await query.HandleAsync(null, null, CancellationToken.None);

        Assert.Empty(archive.Groups);
    }
}
