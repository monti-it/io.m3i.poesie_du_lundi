using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class GetPublishedPoemBySlugQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Returns_a_published_poem_by_slug()
    {
        await using var dbContext = CreateDbContext();
        var poem = new Poem("Un poème", "Un corps.");
        poem.Schedule(new DateOnly(2026, 9, 14));
        poem.Publish();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(poem.Slug.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(poem.Id, result.Id);
        Assert.Equal(poem.Body, result.Body);
        Assert.Null(result.Series);
        Assert.Null(result.Previous);
        Assert.Null(result.Next);
    }

    [Fact]
    public async Task Includes_prev_and_next_neighbours_by_publication_date()
    {
        await using var dbContext = CreateDbContext();
        var older = new Poem("Plus ancien", "Un corps.");
        older.Schedule(new DateOnly(2026, 9, 7));
        older.Publish();
        var middle = new Poem("Milieu", "Un corps.");
        middle.Schedule(new DateOnly(2026, 9, 14));
        middle.Publish();
        var newer = new Poem("Plus récent", "Un corps.");
        newer.Schedule(new DateOnly(2026, 9, 21));
        newer.Publish();
        await dbContext.Poems.AddRangeAsync(older, middle, newer);
        await dbContext.SaveChangesAsync();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(middle.Slug.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(older.Slug.Value, result.Previous?.Slug);
        Assert.Equal(newer.Slug.Value, result.Next?.Slug);
    }

    [Fact]
    public async Task Has_no_next_neighbour_for_the_most_recent_poem()
    {
        await using var dbContext = CreateDbContext();
        var older = new Poem("Plus ancien", "Un corps.");
        older.Schedule(new DateOnly(2026, 9, 7));
        older.Publish();
        var mostRecent = new Poem("Le plus récent", "Un corps.");
        mostRecent.Schedule(new DateOnly(2026, 9, 14));
        mostRecent.Publish();
        await dbContext.Poems.AddRangeAsync(older, mostRecent);
        await dbContext.SaveChangesAsync();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(mostRecent.Slug.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(older.Slug.Value, result.Previous?.Slug);
        Assert.Null(result.Next);
    }

    [Fact]
    public async Task Includes_the_series_link_when_the_poem_belongs_to_one()
    {
        await using var dbContext = CreateDbContext();
        var series = new Series("Une saison", 1);
        var poem = new Poem("Un poème de saison", "Un corps.", series.Id);
        poem.Schedule(new DateOnly(2026, 9, 14));
        poem.Publish();
        await dbContext.Series.AddAsync(series);
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(poem.Slug.Value, CancellationToken.None);

        Assert.NotNull(result?.Series);
        Assert.Equal(series.Id, result.Series.Id);
        Assert.Equal(series.Slug.Value, result.Series.Slug);
    }

    [Fact]
    public async Task Returns_null_for_a_draft_poem()
    {
        await using var dbContext = CreateDbContext();
        var draft = new Poem("Brouillon", "Un corps.");
        await dbContext.Poems.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(draft.Slug.Value, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_for_a_poem_scheduled_in_the_future()
    {
        await using var dbContext = CreateDbContext();
        var future = new Poem("Futur", "Un corps.");
        future.Schedule(new DateOnly(2026, 9, 28));
        await dbContext.Poems.AddAsync(future);
        await dbContext.SaveChangesAsync();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync(future.Slug.Value, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_for_a_malformed_slug_instead_of_throwing()
    {
        await using var dbContext = CreateDbContext();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync("Not A Valid Slug!", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_for_an_unknown_slug()
    {
        await using var dbContext = CreateDbContext();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now));

        var result = await query.HandleAsync("unknown-slug", CancellationToken.None);

        Assert.Null(result);
    }
}
