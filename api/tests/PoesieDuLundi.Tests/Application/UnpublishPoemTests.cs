using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Tests.Application;

public class UnpublishPoemTests
{
    [Fact]
    public async Task Unpublishes_a_published_poem()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        poem.Schedule(new DateOnly(2026, 9, 21));
        poem.Publish();
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new UnpublishPoem(repository);

        var result = await useCase.HandleAsync(poem.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(PoemStatus.Draft, poem.Status);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_poem_is_not_published()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new UnpublishPoem(repository);

        var result = await useCase.HandleAsync(poem.Id);

        Assert.True(result.IsFailure);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
