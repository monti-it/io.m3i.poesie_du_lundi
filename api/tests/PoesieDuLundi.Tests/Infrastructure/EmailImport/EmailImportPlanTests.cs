using PoesieDuLundi.Infrastructure.EmailImport;

namespace PoesieDuLundi.Tests.Infrastructure.EmailImport;

public class EmailImportPlanTests
{
    private static readonly DateTimeOffset AMonday = new(2021, 1, 4, 9, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Keeps_the_first_by_path_and_reports_the_rest_as_duplicates_of_it()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            EmlFixtureBuilder.Write(
                Path.Combine(root, "Gmail"), "poem.eml", "Sujet", "Corps", AMonday, "same-id@example.com");
            EmlFixtureBuilder.Write(
                Path.Combine(root, "Gmail(1)"), "poem.eml", "Sujet", "Corps", AMonday, "same-id@example.com");

            var plan = EmailImportPlan.Create(EmailArchiveScan.Scan(root), ImportManifest.Load(EmptyManifestPath()));

            // Ordinal string order, not folder-creation order: "(" (0x28) sorts before "/" (0x2F),
            // so "Gmail(1)/poem.eml" is the ordinally-first path and is what "kept" means here —
            // which physical duplicate wins doesn't matter (same email), only that it's deterministic.
            var candidate = Assert.Single(plan.Candidates);
            Assert.Equal(Path.Combine(root, "Gmail(1)", "poem.eml"), candidate.SourceFile);
            var duplicate = Assert.Single(plan.SkippedDuplicates);
            Assert.Equal(Path.Combine(root, "Gmail", "poem.eml"), duplicate.Skipped.SourceFile);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Excludes_emails_already_recorded_in_the_manifest()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var path = EmlFixtureBuilder.Write(root, "poem.eml", "Sujet", "Corps", AMonday, "already@example.com");
            var manifestPath = EmptyManifestPath();
            var manifest = ImportManifest.Load(manifestPath);
            manifest.Add(new ImportManifestEntry(
                "already@example.com", path, Guid.NewGuid(), "Sujet", "sujet",
                DateOnly.FromDateTime(AMonday.Date), DateTimeOffset.UtcNow));

            var plan = EmailImportPlan.Create(EmailArchiveScan.Scan(root), manifest);

            Assert.Empty(plan.Candidates);
            Assert.Single(plan.SkippedAlreadyImported);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Flags_more_than_one_candidate_landing_on_the_same_date_without_dropping_either()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            EmlFixtureBuilder.Write(root, "a.eml", "Poeme A", "Corps A", AMonday, "a@example.com");
            EmlFixtureBuilder.Write(root, "b.eml", "Poeme B", "Corps B", AMonday, "b@example.com");

            var plan = EmailImportPlan.Create(EmailArchiveScan.Scan(root), ImportManifest.Load(EmptyManifestPath()));

            Assert.Equal(2, plan.Candidates.Count);
            var group = Assert.Single(plan.SameDateCandidates);
            Assert.Equal(2, group.Count());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Reports_non_email_files_and_parse_failures_without_touching_valid_candidates()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            EmlFixtureBuilder.Write(root, "poem.eml", "Sujet", "Corps", AMonday, "good@example.com");
            File.WriteAllText(Path.Combine(root, "attachment.odt"), "not an email");
            File.WriteAllBytes(Path.Combine(root, "broken.eml"), [0x00, 0xFF]);

            var plan = EmailImportPlan.Create(EmailArchiveScan.Scan(root), ImportManifest.Load(EmptyManifestPath()));

            Assert.Single(plan.Candidates);
            Assert.Single(plan.SkippedOtherFiles);
            Assert.Single(plan.ParseFailures);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string EmptyManifestPath() => Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
}
