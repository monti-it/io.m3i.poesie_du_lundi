namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>What a directory tree under <c>datasource/Gmail*</c> (issue #52) contains, split by
/// whether <see cref="EmlEmailParser"/> can even attempt a file: <c>.eml</c> vs everything else
/// (the stray <c>.odt</c>/<c>.doc</c>/<c>.jpg</c> attachments Takeout leaves alongside the emails).
/// Both lists are sorted so a run is reproducible and diffable.</summary>
public sealed record EmailArchiveScan(IReadOnlyList<string> EmlFiles, IReadOnlyList<string> OtherFiles)
{
    public static EmailArchiveScan Scan(string rootPath)
    {
        var eml = new List<string>();
        var other = new List<string>();

        foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            (Path.GetExtension(file).Equals(".eml", StringComparison.OrdinalIgnoreCase) ? eml : other)
                .Add(file);
        }

        eml.Sort(StringComparer.Ordinal);
        other.Sort(StringComparer.Ordinal);
        return new EmailArchiveScan(eml, other);
    }
}
