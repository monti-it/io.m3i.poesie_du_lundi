namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>One poem an <c>import-emails</c> run created (or would create, in a dry run).</summary>
public sealed record ImportedPoem(Guid PoemId, string Title, string Slug, DateOnly PublicationDate, string SourceFile);

/// <summary>What an <c>import-emails</c> run did (or, for a dry run, would do) — printed to the
/// console by <c>ImportEmailsCommandLine</c> (issue #52).</summary>
public sealed record EmailImportReport(bool DryRun, EmailImportPlan Plan, IReadOnlyList<ImportedPoem> Imported);
