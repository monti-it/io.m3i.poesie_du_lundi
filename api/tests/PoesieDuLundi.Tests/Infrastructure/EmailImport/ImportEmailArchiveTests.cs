using NSubstitute;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure.EmailImport;

namespace PoesieDuLundi.Tests.Infrastructure.EmailImport;

public class ImportEmailArchiveTests
{
    private static readonly DateTimeOffset AMonday = new(2021, 1, 4, 9, 0, 0, TimeSpan.FromHours(1));
    private static readonly DateTimeOffset ATuesday = new(2021, 1, 5, 9, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public async Task Dry_run_reports_the_plan_without_writing_anything()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var manifestPath = Path.Combine(root, "manifest.json");
        try
        {
            EmlFixtureBuilder.Write(root, "poem.eml", "Un poeme", "Corps", AMonday, "id-1@example.com");
            var repository = Substitute.For<IPoemRepository>();

            var report = await new ImportEmailArchive(repository)
                .ExecuteAsync(root, manifestPath, dryRun: true);

            Assert.True(report.DryRun);
            Assert.Single(report.Plan.Candidates);
            Assert.Empty(report.Imported);
            await repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
            Assert.False(File.Exists(manifestPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Real_run_creates_a_published_backdated_poem_per_candidate()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var manifestPath = Path.Combine(root, "manifest.json");
        try
        {
            EmlFixtureBuilder.Write(root, "poem.eml", "Un poeme", "Corps du poeme", AMonday, "id-1@example.com");
            var repository = Substitute.For<IPoemRepository>();
            Poem? saved = null;
            repository.AddAsync(Arg.Do<Poem>(poem => saved = poem), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var report = await new ImportEmailArchive(repository).ExecuteAsync(root, manifestPath, dryRun: false);

            var imported = Assert.Single(report.Imported);
            await repository.Received(1).AddAsync(Arg.Any<Poem>(), Arg.Any<CancellationToken>());
            await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
            Assert.NotNull(saved);
            Assert.Equal("Un poeme", saved!.Title);
            Assert.Equal("Corps du poeme", saved.Body);
            Assert.Equal(PoemStatus.Published, saved.Status);
            Assert.Equal(new DateOnly(2021, 1, 4), saved.PublicationDate);
            Assert.Equal(saved.Id, imported.PoemId);

            var manifest = ImportManifest.Load(manifestPath);
            Assert.True(manifest.Contains("id-1@example.com"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Backdates_a_non_Monday_email_without_going_through_the_Monday_only_rule()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var manifestPath = Path.Combine(root, "manifest.json");
        try
        {
            EmlFixtureBuilder.Write(root, "poem.eml", "Un poeme", "Corps", ATuesday, "id-1@example.com");
            var repository = Substitute.For<IPoemRepository>();

            var report = await new ImportEmailArchive(repository).ExecuteAsync(root, manifestPath, dryRun: false);

            Assert.Single(report.Imported);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Disambiguates_slugs_for_poems_that_share_the_same_recurring_title()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var manifestPath = Path.Combine(root, "manifest.json");
        try
        {
            EmlFixtureBuilder.Write(root, "a.eml", "la poésie du lundi", "Corps A", AMonday, "a@example.com");
            EmlFixtureBuilder.Write(root, "b.eml", "la poésie du lundi", "Corps B", ATuesday, "b@example.com");
            var repository = Substitute.For<IPoemRepository>();

            var report = await new ImportEmailArchive(repository).ExecuteAsync(root, manifestPath, dryRun: false);

            var slugs = report.Imported.Select(poem => poem.Slug).ToList();
            Assert.Equal(2, slugs.Distinct().Count());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Second_run_skips_emails_already_recorded_in_the_manifest()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var manifestPath = Path.Combine(root, "manifest.json");
        try
        {
            EmlFixtureBuilder.Write(root, "poem.eml", "Un poeme", "Corps", AMonday, "id-1@example.com");
            var repository = Substitute.For<IPoemRepository>();
            await new ImportEmailArchive(repository).ExecuteAsync(root, manifestPath, dryRun: false);

            var secondReport = await new ImportEmailArchive(repository).ExecuteAsync(root, manifestPath, dryRun: false);

            Assert.Empty(secondReport.Imported);
            Assert.Single(secondReport.Plan.SkippedAlreadyImported);
            await repository.Received(1).AddAsync(Arg.Any<Poem>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
