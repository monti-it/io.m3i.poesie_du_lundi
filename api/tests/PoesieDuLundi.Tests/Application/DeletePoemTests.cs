using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Tests.Application;

public class DeletePoemTests
{
    [Fact]
    public async Task Removes_an_existing_poem()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new DeletePoem(repository);

        var result = await useCase.HandleAsync(poem.Id);

        Assert.True(result.IsSuccess);
        repository.Received(1).Remove(poem);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_poem_does_not_exist()
    {
        var repository = Substitute.For<IPoemRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Poem?)null);
        var useCase = new DeletePoem(repository);

        var result = await useCase.HandleAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        repository.DidNotReceive().Remove(Arg.Any<Poem>());
    }
}
