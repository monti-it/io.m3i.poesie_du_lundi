using PoesieDuLundi.Infrastructure.EmailImport;

namespace PoesieDuLundi.Tests.Infrastructure.EmailImport;

public class ImportManifestTests
{
    [Fact]
    public void Load_returns_an_empty_manifest_when_no_file_exists_yet()
    {
        var manifest = ImportManifest.Load(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json"));

        Assert.Empty(manifest.Entries);
        Assert.False(manifest.Contains("anything"));
    }

    [Fact]
    public void Round_trips_entries_through_save_and_load()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            var entry = new ImportManifestEntry(
                "msg-1", "source.eml", Guid.NewGuid(), "Un titre", "un-titre",
                new DateOnly(2021, 1, 4), DateTimeOffset.UtcNow);
            var manifest = ImportManifest.Load(path);
            manifest.Add(entry);
            manifest.Save(path);

            var reloaded = ImportManifest.Load(path);

            Assert.True(reloaded.Contains("msg-1"));
            Assert.Contains("un-titre", reloaded.UsedSlugs);
            Assert.Equal(entry, Assert.Single(reloaded.Entries));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
