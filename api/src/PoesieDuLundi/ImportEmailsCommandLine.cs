namespace PoesieDuLundi;

/// <summary>Options for the <c>import-emails</c> verb (issue #52), parsed by
/// <see cref="ImportEmailsCommandLine"/>.</summary>
public sealed record ImportEmailsOptions(string RootPath, string ManifestPath, bool DryRun);

/// <summary>
/// Recognises the one-time <c>import-emails</c> command
/// (<c>dotnet PoesieDuLundi.dll import-emails &lt;path&gt; [--dry-run] [--manifest &lt;path&gt;]</c>):
/// converts the <c>datasource/Gmail*</c> email archive into published <c>Poem</c>s (issue #52),
/// then exits without serving — the <c>migrate</c> verb's sibling (see
/// <see cref="MigrationCommandLine"/>).
/// </summary>
public static class ImportEmailsCommandLine
{
    public const string Verb = "import-emails";

    public static bool IsImportEmails(string[] args) =>
        args.Length > 0 && string.Equals(args[0], Verb, StringComparison.OrdinalIgnoreCase);

    public static ImportEmailsOptions Parse(string[] args)
    {
        var positional = new List<string>();
        var dryRun = false;
        string? manifestPath = null;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--manifest":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("--manifest requires a path.");
                    }

                    manifestPath = args[++i];
                    break;
                default:
                    positional.Add(args[i]);
                    break;
            }
        }

        if (positional.Count != 1)
        {
            throw new ArgumentException(
                $"Usage: {Verb} <path-to-Gmail-archive> [--dry-run] [--manifest <path>]");
        }

        var rootPath = positional[0];
        // Defaults next to the archive, not the working directory, so a manifest from one run is
        // found by the next run even if it's invoked from somewhere else.
        manifestPath ??= Path.Combine(rootPath, "import-emails-manifest.json");
        return new ImportEmailsOptions(rootPath, manifestPath, dryRun);
    }
}
