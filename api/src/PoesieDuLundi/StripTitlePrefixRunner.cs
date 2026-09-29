using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Application;
using PoesieDuLundi.Infrastructure.TitleCleanup;

namespace PoesieDuLundi;

/// <summary>Runs the <c>strip-title-prefix</c> verb against the app's DI container — the
/// <c>import-emails</c> verb's sibling, see <see cref="EmailImportRunner"/> — and prints a
/// human-readable report.</summary>
public static class StripTitlePrefixRunner
{
    public static async Task RunAsync(IServiceProvider services, StripTitlePrefixOptions options)
    {
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPoemRepository>();
        var report = await new StripTitlePrefix(repository).ExecuteAsync(options.DryRun);

        Print(report);
    }

    private static void Print(TitlePrefixCleanupReport report)
    {
        Console.WriteLine(report.DryRun
            ? "Dry run — no titles were changed."
            : "Title cleanup complete.");
        Console.WriteLine($"  {(report.DryRun ? "Would retitle" : "Retitled")}: {report.Retitled.Count}");

        foreach (var poem in report.Retitled)
        {
            Console.WriteLine($"    - {poem.PoemId}: '{poem.OldTitle}' -> '{poem.NewTitle}'");
        }
    }
}
