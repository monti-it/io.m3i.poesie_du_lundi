namespace PoesieDuLundi;

/// <summary>
/// Recognises the <c>migrate</c> command (<c>dotnet PoesieDuLundi.dll migrate</c>): apply pending
/// migrations as an ordered, pre-serving step, then exit without serving. The deployed k8s
/// <c>api</c> Deployment runs this as an init container before the app container starts.
/// </summary>
public static class MigrationCommandLine
{
    public const string Verb = "migrate";

    public static bool IsMigrateOnly(string[] args) =>
        args.Contains(Verb, StringComparer.OrdinalIgnoreCase);
}
