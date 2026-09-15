using System.Text.Json;

namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>The on-disk record of every poem a real (non-dry-run) <c>import-emails</c> run has
/// created, keyed by the source email's Message-ID. Loaded at the start of a run so a re-run skips
/// what's already there (idempotency) and rewritten after every poem so a crash mid-run loses no
/// tracking (issue #52).</summary>
public sealed class ImportManifest
{
    private readonly Dictionary<string, ImportManifestEntry> _byMessageId;

    private ImportManifest(IEnumerable<ImportManifestEntry> entries)
    {
        _byMessageId = entries.ToDictionary(entry => entry.MessageId);
    }

    public IReadOnlyCollection<ImportManifestEntry> Entries => _byMessageId.Values;

    public bool Contains(string messageId) => _byMessageId.ContainsKey(messageId);

    public IReadOnlySet<string> UsedSlugs => _byMessageId.Values.Select(entry => entry.Slug).ToHashSet();

    public void Add(ImportManifestEntry entry) => _byMessageId[entry.MessageId] = entry;

    public static ImportManifest Load(string path) =>
        new(File.Exists(path)
            ? JsonSerializer.Deserialize<List<ImportManifestEntry>>(File.ReadAllText(path)) ?? []
            : []);

    public void Save(string path)
    {
        var ordered = _byMessageId.Values.OrderBy(entry => entry.SourceFile, StringComparer.Ordinal);
        var json = JsonSerializer.Serialize(ordered, new JsonSerializerOptions { WriteIndented = true });

        // Write-then-move: a crash mid-write leaves the previous manifest intact rather than a
        // half-written file the next run would fail to parse.
        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}
