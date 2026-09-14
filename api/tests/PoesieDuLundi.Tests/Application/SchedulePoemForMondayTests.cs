using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Tests.Application;

public class SchedulePoemForMondayTests
{
    private static readonly DateOnly AMonday = new(2026, 9, 21);
    private static readonly DateOnly ATuesday = new(2026, 9, 22);

    [Fact]
    public async Task Rejects_a_non_Monday_date()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new SchedulePoemForMonday(repository);

        var result = await useCase.HandleAsync(poem.Id, ATuesday);

        Assert.True(result.IsFailure);
        Assert.Equal(PoemStatus.Draft, poem.Status);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_Monday_that_already_has_a_scheduled_or_published_poem()
    {
        var repository = Substitute.For<IPoemRepository>();
        repository.HasScheduledOrPublishedForDateAsync(AMonday, Arg.Any<CancellationToken>()).Returns(true);
        var poem = new Poem("Un titre", "Un corps.");
        var useCase = new SchedulePoemForMonday(repository);

        var result = await useCase.HandleAsync(poem.Id, AMonday);

        Assert.True(result.IsFailure);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Schedules_a_draft_poem_for_a_free_Monday()
    {
        var repository = Substitute.For<IPoemRepository>();
        repository.HasScheduledOrPublishedForDateAsync(AMonday, Arg.Any<CancellationToken>()).Returns(false);
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new SchedulePoemForMonday(repository);

        var result = await useCase.HandleAsync(poem.Id, AMonday);

        Assert.True(result.IsSuccess);
        Assert.Equal(PoemStatus.Scheduled, poem.Status);
        Assert.Equal(AMonday, poem.PublicationDate);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_poem_does_not_exist()
    {
        var repository = Substitute.For<IPoemRepository>();
        repository.HasScheduledOrPublishedForDateAsync(AMonday, Arg.Any<CancellationToken>()).Returns(false);
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Poem?)null);
        var useCase = new SchedulePoemForMonday(repository);

        var result = await useCase.HandleAsync(Guid.NewGuid(), AMonday);

        Assert.True(result.IsFailure);
    }
}
