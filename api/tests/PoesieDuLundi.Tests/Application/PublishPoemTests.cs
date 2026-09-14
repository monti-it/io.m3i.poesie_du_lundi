using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Tests.Application;

public class PublishPoemTests
{
    [Fact]
    public async Task Publishes_a_scheduled_poem()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        poem.Schedule(new DateOnly(2026, 9, 21));
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new PublishPoem(repository);

        var result = await useCase.HandleAsync(poem.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(PoemStatus.Published, poem.Status);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_poem_is_not_scheduled()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new PublishPoem(repository);

        var result = await useCase.HandleAsync(poem.Id);

        Assert.True(result.IsFailure);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_poem_does_not_exist()
    {
        var repository = Substitute.For<IPoemRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Poem?)null);
        var useCase = new PublishPoem(repository);

        var result = await useCase.HandleAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
    }
}
