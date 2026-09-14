using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Tests.Application;

public class MaterialiseDuePoemsTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Publishes_every_scheduled_poem_whose_date_has_arrived()
    {
        var repository = Substitute.For<IPoemRepository>();
        var timeProvider = new FakeTimeProvider(Now);
        var due = new Poem("Un titre", "Un corps.");
        due.Schedule(new DateOnly(2026, 9, 21));
        repository.GetDueForPublicationAsync(new DateOnly(2026, 9, 21), Arg.Any<CancellationToken>())
            .Returns([due]);
        var useCase = new MaterialiseDuePoems(repository, timeProvider);

        var published = await useCase.HandleAsync();

        Assert.Equal(1, published);
        Assert.Equal(PoemStatus.Published, due.Status);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Is_idempotent_a_second_sweep_finds_nothing_left_to_publish()
    {
        var repository = Substitute.For<IPoemRepository>();
        var timeProvider = new FakeTimeProvider(Now);
        repository.GetDueForPublicationAsync(new DateOnly(2026, 9, 21), Arg.Any<CancellationToken>())
            .Returns([]);
        var useCase = new MaterialiseDuePoems(repository, timeProvider);

        var published = await useCase.HandleAsync();

        Assert.Equal(0, published);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
