using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PoesieDuLundi.Application;
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

    // Never previewing in these tests — a substitute that always rejects makes that explicit.
    private static IPreviewTokenService NoPreviewAccess()
    {
        var service = Substitute.For<IPreviewTokenService>();
        service.Validate(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);
        return service;
    }

    [Fact]
    public async Task Returns_a_published_poem_by_slug()
    {
        await using var dbContext = CreateDbContext();
        var poem = new Poem("Un poème", "Un corps.");
        poem.Schedule(new DateOnly(2026, 9, 14));
        poem.Publish();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), NoPreviewAccess());

        var result = await query.HandleAsync(poem.Slug.Value, previewToken: null, CancellationToken.None);

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
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), NoPreviewAccess());

        var result = await query.HandleAsync(middle.Slug.Value, previewToken: null, CancellationToken.None);

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
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), NoPreviewAccess());

        var result = await query.HandleAsync(mostRecent.Slug.Value, previewToken: null, CancellationToken.None);

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
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), NoPreviewAccess());

        var result = await query.HandleAsync(poem.Slug.Value, previewToken: null, CancellationToken.None);

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
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), NoPreviewAccess());

        var result = await query.HandleAsync(draft.Slug.Value, previewToken: null, CancellationToken.None);

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
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), NoPreviewAccess());

        var result = await query.HandleAsync(future.Slug.Value, previewToken: null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_for_a_malformed_slug_instead_of_throwing()
    {
        await using var dbContext = CreateDbContext();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), NoPreviewAccess());

        var result = await query.HandleAsync("Not A Valid Slug!", previewToken: null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_for_an_unknown_slug()
    {
        await using var dbContext = CreateDbContext();
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), NoPreviewAccess());

        var result = await query.HandleAsync("unknown-slug", previewToken: null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task A_valid_preview_token_returns_a_poem_scheduled_in_the_future()
    {
        await using var dbContext = CreateDbContext();
        var future = new Poem("Futur en révision", "Un corps.");
        future.Schedule(new DateOnly(2026, 9, 28));
        await dbContext.Poems.AddAsync(future);
        await dbContext.SaveChangesAsync();
        var tokenService = Substitute.For<IPreviewTokenService>();
        tokenService.Validate(future.Id, "valid-token").Returns(true);
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), tokenService);

        var result = await query.HandleAsync(future.Slug.Value, "valid-token", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(future.Id, result.Id);
    }

    [Fact]
    public async Task An_invalid_preview_token_still_returns_null_for_an_unpublished_poem()
    {
        await using var dbContext = CreateDbContext();
        var future = new Poem("Futur invérifiable", "Un corps.");
        future.Schedule(new DateOnly(2026, 9, 28));
        await dbContext.Poems.AddAsync(future);
        await dbContext.SaveChangesAsync();
        var tokenService = Substitute.For<IPreviewTokenService>();
        tokenService.Validate(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), tokenService);

        var result = await query.HandleAsync(future.Slug.Value, "wrong-token", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task A_preview_token_never_reaches_a_draft_poem_with_no_publication_date()
    {
        await using var dbContext = CreateDbContext();
        var draft = new Poem("Brouillon en révision", "Un corps.");
        await dbContext.Poems.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var tokenService = Substitute.For<IPreviewTokenService>();
        tokenService.Validate(draft.Id, "valid-token").Returns(true);
        var query = new GetPublishedPoemBySlugQuery(dbContext, new FakeTimeProvider(Now), tokenService);

        var result = await query.HandleAsync(draft.Slug.Value, "valid-token", CancellationToken.None);

        Assert.Null(result);
        tokenService.DidNotReceive().Validate(Arg.Any<Guid>(), Arg.Any<string>());
    }
}
