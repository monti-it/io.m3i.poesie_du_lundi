using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.Application;

public class UpdatePoemTests
{
    [Fact]
    public async Task Updates_title_body_and_series()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var seriesId = Guid.NewGuid();
        var useCase = new UpdatePoem(repository);

        var result = await useCase.HandleAsync(poem.Id, "Nouveau titre", "Nouveau corps.", null, seriesId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nouveau titre", poem.Title);
        Assert.Equal("Nouveau corps.", poem.Body);
        Assert.Equal(seriesId, poem.SeriesId);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Updates_the_slug_when_one_is_given()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new UpdatePoem(repository);

        var result = await useCase.HandleAsync(
            poem.Id, poem.Title, poem.Body, "un-slug-choisi-a-la-main", null);

        Assert.True(result.IsSuccess);
        Assert.Equal("un-slug-choisi-a-la-main", poem.Slug.Value);
    }

    [Fact]
    public async Task Fails_when_the_given_slug_is_invalid()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new UpdatePoem(repository);

        var result = await useCase.HandleAsync(poem.Id, poem.Title, poem.Body, "Not A Valid Slug!", null);

        Assert.True(result.IsFailure);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Updates_the_tags_when_given()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new UpdatePoem(repository);

        var result = await useCase.HandleAsync(
            poem.Id, poem.Title, poem.Body, null, null, tags: ["hiver", "amour"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(["amour", "hiver"], poem.Tags.Select(tag => tag.Value));
    }

    [Fact]
    public async Task Leaves_tags_untouched_when_none_are_given()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        poem.ChangeTags([new Slug("amour")]);
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new UpdatePoem(repository);

        var result = await useCase.HandleAsync(poem.Id, poem.Title, poem.Body, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(["amour"], poem.Tags.Select(tag => tag.Value));
    }

    [Fact]
    public async Task Fails_when_a_given_tag_is_not_a_valid_slug()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new UpdatePoem(repository);

        var result = await useCase.HandleAsync(
            poem.Id, poem.Title, poem.Body, null, null, tags: ["Not A Valid Tag!"]);

        Assert.True(result.IsFailure);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_new_title_is_empty()
    {
        var repository = Substitute.For<IPoemRepository>();
        var poem = new Poem("Un titre", "Un corps.");
        repository.GetAsync(poem.Id, Arg.Any<CancellationToken>()).Returns(poem);
        var useCase = new UpdatePoem(repository);

        var result = await useCase.HandleAsync(poem.Id, "", "Un corps.", null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Un titre", poem.Title);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_poem_does_not_exist()
    {
        var repository = Substitute.For<IPoemRepository>();
        repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Poem?)null);
        var useCase = new UpdatePoem(repository);

        var result = await useCase.HandleAsync(Guid.NewGuid(), "Un titre", "Un corps.", null, null);

        Assert.True(result.IsFailure);
    }
}
