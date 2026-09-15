namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>What one <c>import-emails</c> run would do, computed once and shared by dry-run
/// (report only) and the real run (report, then act on <see cref="Candidates"/>) — so the two
/// never disagree about what counts as a duplicate or an already-imported email (issue #52).</summary>
public sealed record EmailImportPlan(
    IReadOnlyList<ParsedEmail> Candidates,
    IReadOnlyList<string> SkippedOtherFiles,
    IReadOnlyList<(string File, string Reason)> ParseFailures,
    IReadOnlyList<(ParsedEmail Kept, ParsedEmail Skipped)> SkippedDuplicates,
    IReadOnlyList<ParsedEmail> SkippedAlreadyImported,
    IReadOnlyList<IGrouping<DateOnly, ParsedEmail>> SameDateCandidates)
{
    public static EmailImportPlan Create(EmailArchiveScan scan, ImportManifest manifest)
    {
        var parsed = new List<ParsedEmail>();
        var parseFailures = new List<(string, string)>();
        foreach (var file in scan.EmlFiles)
        {
            var outcome = EmlEmailParser.Parse(file);
            if (outcome.IsSuccess)
            {
                parsed.Add(outcome.Email!);
            }
            else
            {
                parseFailures.Add((file, outcome.SkipReason!));
            }
        }

        var duplicates = new List<(ParsedEmail, ParsedEmail)>();
        var deduped = new List<ParsedEmail>();
        foreach (var group in parsed.GroupBy(email => email.MessageId))
        {
            var ordered = group.OrderBy(email => email.SourceFile, StringComparer.Ordinal).ToList();
            deduped.Add(ordered[0]);
            duplicates.AddRange(ordered.Skip(1).Select(skipped => (ordered[0], skipped)));
        }

        var alreadyImported = deduped.Where(email => manifest.Contains(email.MessageId)).ToList();
        var candidates = deduped
            .Where(email => !manifest.Contains(email.MessageId))
            .OrderBy(email => email.PublicationDate)
            .ThenBy(email => email.SourceFile, StringComparer.Ordinal)
            .ToList();

        var sameDateCandidates = candidates
            .GroupBy(email => email.PublicationDate)
            .Where(group => group.Count() > 1)
            .ToList();

        return new EmailImportPlan(
            candidates, scan.OtherFiles, parseFailures, duplicates, alreadyImported, sameDateCandidates);
    }
}
