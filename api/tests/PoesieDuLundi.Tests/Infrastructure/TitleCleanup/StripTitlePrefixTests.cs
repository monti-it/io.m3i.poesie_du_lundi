using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure.TitleCleanup;

namespace PoesieDuLundi.Tests.Infrastructure.TitleCleanup;

public class StripTitlePrefixTests
{
    [Fact]
    public async Task Dry_run_reports_changes_without_writing_anything()
    {
        var prefixed = new Poem("La poésie du lundi : Un poème", "Corps");
        var clean = new Poem("Un autre poème", "Corps");
        var repository = Substitute.For<IPoemRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([prefixed, clean]);

        var report = await new StripTitlePrefix(repository).ExecuteAsync(dryRun: true);

        var change = Assert.Single(report.Retitled);
        Assert.Equal(prefixed.Id, change.PoemId);
        Assert.Equal("Un poème", change.NewTitle);
        Assert.Equal("La poésie du lundi : Un poème", prefixed.Title);
        await repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Real_run_retitles_only_poems_with_the_prefix_and_saves_once()
    {
        var prefixed = new Poem("la poésie du lundi : Un poème", "Corps");
        var clean = new Poem("Un autre poème", "Corps");
        var repository = Substitute.For<IPoemRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([prefixed, clean]);

        var report = await new StripTitlePrefix(repository).ExecuteAsync(dryRun: false);

        Assert.Single(report.Retitled);
        Assert.Equal("Un poème", prefixed.Title);
        Assert.Equal("Un autre poème", clean.Title);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Real_run_with_nothing_to_retitle_does_not_save()
    {
        var clean = new Poem("Un poème déjà propre", "Corps");
        var repository = Substitute.For<IPoemRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([clean]);

        var report = await new StripTitlePrefix(repository).ExecuteAsync(dryRun: false);

        Assert.Empty(report.Retitled);
        await repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
