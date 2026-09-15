using PoesieDuLundi.Infrastructure.EmailImport;

namespace PoesieDuLundi.Tests.Infrastructure.EmailImport;

public class EmailArchiveScanTests
{
    [Fact]
    public void Separates_eml_files_from_everything_else_recursively_and_sorted()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Gmail(1)"));
            File.WriteAllText(Path.Combine(root, "Gmail(1)", "b.eml"), "b");
            File.WriteAllText(Path.Combine(root, "a.eml"), "a");
            File.WriteAllText(Path.Combine(root, "Resistance.odt"), "not an email");
            File.WriteAllText(Path.Combine(root, "photo.jpg"), "not an email either");

            var scan = EmailArchiveScan.Scan(root);

            Assert.Equal(
                new[] { Path.Combine(root, "Gmail(1)", "b.eml"), Path.Combine(root, "a.eml") }
                    .OrderBy(p => p, StringComparer.Ordinal),
                scan.EmlFiles);
            Assert.Equal(2, scan.OtherFiles.Count);
            Assert.Contains(Path.Combine(root, "Resistance.odt"), scan.OtherFiles);
            Assert.Contains(Path.Combine(root, "photo.jpg"), scan.OtherFiles);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
