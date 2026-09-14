using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Tests.Infrastructure;

/// <summary>Proves the EF Core mapping in <c>PoemConfiguration</c> round-trips a <see cref="Poem"/>
/// through its constructor-binding materialisation (no parameterless constructor, no public
/// setters) — the part a mocked-repository Application test can't cover.</summary>
public class PoemRepositoryTests
{
    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Adding_then_getting_round_trips_a_poem()
    {
        await using var dbContext = CreateDbContext();
        var repository = new PoemRepository(dbContext);
        var poem = new Poem("Un titre", "Un corps.");

        await repository.AddAsync(poem, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var reloaded = await repository.GetAsync(poem.Id, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal(poem.Title, reloaded.Title);
        Assert.Equal(poem.Slug, reloaded.Slug);
        Assert.Equal(PoemStatus.Draft, reloaded.Status);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_an_unknown_id()
    {
        await using var dbContext = CreateDbContext();
        var repository = new PoemRepository(dbContext);

        var result = await repository.GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HasScheduledOrPublishedForDateAsync_ignores_drafts_and_other_dates()
    {
        await using var dbContext = CreateDbContext();
        var repository = new PoemRepository(dbContext);
        var draft = new Poem("Brouillon", "Un corps.");
        var scheduled = new Poem("Programmé", "Un corps.");
        scheduled.Schedule(new DateOnly(2026, 9, 21));
        await repository.AddAsync(draft, CancellationToken.None);
        await repository.AddAsync(scheduled, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.False(await repository.HasScheduledOrPublishedForDateAsync(
            new DateOnly(2026, 9, 28), CancellationToken.None));
        Assert.True(await repository.HasScheduledOrPublishedForDateAsync(
            new DateOnly(2026, 9, 21), CancellationToken.None));
    }

    [Fact]
    public async Task GetDueForPublicationAsync_returns_only_scheduled_poems_at_or_before_the_date()
    {
        await using var dbContext = CreateDbContext();
        var repository = new PoemRepository(dbContext);
        var due = new Poem("Dû", "Un corps.");
        due.Schedule(new DateOnly(2026, 9, 21));
        var notYetDue = new Poem("Pas encore", "Un corps.");
        notYetDue.Schedule(new DateOnly(2026, 9, 28));
        var alreadyPublished = new Poem("Déjà publié", "Un corps.");
        alreadyPublished.Schedule(new DateOnly(2026, 9, 14));
        alreadyPublished.Publish();
        await repository.AddAsync(due, CancellationToken.None);
        await repository.AddAsync(notYetDue, CancellationToken.None);
        await repository.AddAsync(alreadyPublished, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.GetDueForPublicationAsync(
            new DateOnly(2026, 9, 21), CancellationToken.None);

        var dueId = Assert.Single(result).Id;
        Assert.Equal(due.Id, dueId);
    }
}
