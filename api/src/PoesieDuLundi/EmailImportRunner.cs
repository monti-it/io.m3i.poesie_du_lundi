using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Application;
using PoesieDuLundi.Infrastructure.EmailImport;

namespace PoesieDuLundi;

/// <summary>Runs the <c>import-emails</c> verb against the app's DI container — the <c>migrate</c>
/// verb's sibling, see <see cref="DatabaseMigrator"/> — and prints a human-readable report.
/// Unlike a bad migration, a bad import must never throw past a partially-written manifest: every
/// poem it creates is durable and rollback-able (issue #52) the moment it's created.</summary>
public static class EmailImportRunner
{
    public static async Task RunAsync(IServiceProvider services, ImportEmailsOptions options)
    {
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPoemRepository>();
        var report = await new ImportEmailArchive(repository)
            .ExecuteAsync(options.RootPath, options.ManifestPath, options.DryRun);

        Print(report, options);
    }

    private static void Print(EmailImportReport report, ImportEmailsOptions options)
    {
        var plan = report.Plan;
        var toImportCount = report.DryRun ? plan.Candidates.Count : report.Imported.Count;

        Console.WriteLine(report.DryRun
            ? "Dry run — no poems were created, no manifest was written."
            : $"Import complete — manifest at {options.ManifestPath}.");
        Console.WriteLine($"  {(report.DryRun ? "Would import" : "Imported")}: {toImportCount}");
        Console.WriteLine($"  Already imported (skipped): {plan.SkippedAlreadyImported.Count}");
        Console.WriteLine($"  Duplicate emails (skipped): {plan.SkippedDuplicates.Count}");
        Console.WriteLine($"  Non-email files (skipped): {plan.SkippedOtherFiles.Count}");
        Console.WriteLine($"  Emails that failed to parse (skipped): {plan.ParseFailures.Count}");

        PrintList("Non-email files", plan.SkippedOtherFiles);
        PrintList("Parse failures", plan.ParseFailures.Select(failure => $"{failure.File}: {failure.Reason}"));
        PrintList(
            "Duplicates (kept the first, by path)",
            plan.SkippedDuplicates.Select(pair => $"{pair.Skipped.SourceFile} duplicates {pair.Kept.SourceFile}"));

        if (plan.SameDateCandidates.Count > 0)
        {
            PrintList(
                "Same publication date for more than one poem — verify this is a genuine double-post",
                plan.SameDateCandidates.SelectMany(
                    group => group.Select(email => $"{group.Key}: {email.SourceFile}")));
        }
    }

    private static void PrintList(string label, IEnumerable<string> items)
    {
        var materialized = items.ToList();
        if (materialized.Count == 0)
        {
            return;
        }

        Console.WriteLine($"  {label}:");
        foreach (var item in materialized)
        {
            Console.WriteLine($"    - {item}");
        }
    }
}
