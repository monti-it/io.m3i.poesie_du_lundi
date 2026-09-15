using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Tests.Application;

public class CreateDraftPoemTests
{
    [Fact]
    public async Task Creates_a_draft_poem_and_returns_its_id()
    {
        var repository = Substitute.For<IPoemRepository>();
        var useCase = new CreateDraftPoem(repository);

        var result = await useCase.HandleAsync("Un titre", "Un corps.", null);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await repository.Received(1).AddAsync(
            Arg.Is<Poem>(poem => poem.Title == "Un titre" && poem.Status == PoemStatus.Draft),
            Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_title_is_empty()
    {
        var repository = Substitute.For<IPoemRepository>();
        var useCase = new CreateDraftPoem(repository);

        var result = await useCase.HandleAsync("", "Un corps.", null);

        Assert.True(result.IsFailure);
        await repository.DidNotReceive().AddAsync(Arg.Any<Poem>(), Arg.Any<CancellationToken>());
    }
}
