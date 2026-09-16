namespace PoesieDuLundi;

/// <summary>Options for the <c>strip-title-prefix</c> verb, parsed by
/// <see cref="StripTitlePrefixCommandLine"/>.</summary>
public sealed record StripTitlePrefixOptions(bool DryRun);

/// <summary>
/// Recognises the one-time <c>strip-title-prefix</c> command
/// (<c>dotnet PoesieDuLundi.dll strip-title-prefix [--dry-run]</c>): removes the redundant "La
/// poésie du lundi :" prefix from existing poem titles (issue #62), then exits without serving —
/// the <c>migrate</c>/<c>import-emails</c> verbs' sibling (see <see cref="MigrationCommandLine"/>).
/// </summary>
public static class StripTitlePrefixCommandLine
{
    public const string Verb = "strip-title-prefix";

    public static bool IsStripTitlePrefix(string[] args) =>
        args.Length > 0 && string.Equals(args[0], Verb, StringComparison.OrdinalIgnoreCase);

    public static StripTitlePrefixOptions Parse(string[] args)
    {
        var dryRun = false;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    throw new ArgumentException($"Usage: {Verb} [--dry-run]");
            }
        }

        return new StripTitlePrefixOptions(dryRun);
    }
}
