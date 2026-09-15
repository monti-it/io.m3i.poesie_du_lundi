using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class GetSeriesBySlugQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Returns_the_series_with_its_published_poems_oldest_first()
    {
        await using var dbContext = CreateDbContext();
        var series = new Series("Une saison", 1, "Description");
        var second = new Poem("Deuxième", "Un corps.", series.Id);
        second.Schedule(new DateOnly(2026, 9, 14));
        second.Publish();
        var first = new Poem("Premier", "Un corps.", series.Id);
        first.Schedule(new DateOnly(2026, 9, 7));
        first.Publish();
        var otherSeries = new Series("Une autre saison", 2);
        var otherPoem = new Poem("D'une autre saison", "Un corps.", otherSeries.Id);
        otherPoem.Schedule(new DateOnly(2026, 9, 7));
        otherPoem.Publish();
        await dbContext.Series.AddRangeAsync(series, otherSeries);
        await dbContext.Poems.AddRangeAsync(second, first, otherPoem);
        await dbContext.SaveChangesAsync();
        var query = new GetSeriesBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(series.Slug.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(series.Id, result.Id);
        Assert.Equal(series.Description, result.Description);
        Assert.Equal([first.Id, second.Id], result.Poems.Select(poem => poem.Id));
    }

    [Fact]
    public async Task Excludes_drafts_and_poems_from_other_series()
    {
        await using var dbContext = CreateDbContext();
        var series = new Series("Une saison", 1);
        var draft = new Poem("Brouillon", "Un corps.", series.Id);
        await dbContext.Series.AddAsync(series);
        await dbContext.Poems.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var query = new GetSeriesBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(series.Slug.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Poems);
    }

    [Fact]
    public async Task Returns_null_for_an_unknown_series_slug()
    {
        await using var dbContext = CreateDbContext();
        var query = new GetSeriesBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync("unknown-series", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_for_a_malformed_slug_instead_of_throwing()
    {
        await using var dbContext = CreateDbContext();
        var query = new GetSeriesBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync("Not A Valid Slug!", CancellationToken.None);

        Assert.Null(result);
    }
}
