using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.Application;

public class GeneratePreviewLinkTests
{
    [Fact]
    public async Task Issues_a_token_for_a_scheduled_poem()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        poem.Schedule(new DateOnly(2026, 9, 21));
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var tokenService = Substitute.For<IPreviewTokenService>();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        tokenService.Issue(poem.Id).Returns(new IssuedPreviewToken("a-token", expiresAt));
        var useCase = new GeneratePreviewLink(repository, tokenService);

        var result = await useCase.HandleAsync(poem.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(poem.Slug.Value, result.Value.Slug);
        Assert.Equal("a-token", result.Value.Token);
        Assert.Equal(expiresAt, result.Value.ExpiresAt);
    }

    [Fact]
    public async Task Fails_when_the_poem_does_not_exist()
    {
        var repository = Substitute.For<IPoemRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Poem?)null);
        var tokenService = Substitute.For<IPreviewTokenService>();
        var useCase = new GeneratePreviewLink(repository, tokenService);

        var result = await useCase.HandleAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Type);
    }

    [Fact]
    public async Task Fails_when_the_poem_has_not_been_scheduled_yet()
    {
        var repository = Substitute.For<IPoemRepository>();
        var draft = new Poem("Un titre", "Un corps.");
        repository.GetAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var tokenService = Substitute.For<IPreviewTokenService>();
        var useCase = new GeneratePreviewLink(repository, tokenService);

        var result = await useCase.HandleAsync(draft.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Type);
        tokenService.DidNotReceive().Issue(Arg.Any<Guid>());
    }
}
