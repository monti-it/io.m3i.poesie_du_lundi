namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>One poem this importer created, recorded so a later run can (a) skip it — the
/// idempotency requirement — and (b) a rollback can target exactly these ids and no others
/// (issue #52's explicit rollback ask).</summary>
public sealed record ImportManifestEntry(
    string MessageId,
    string SourceFile,
    Guid PoemId,
    string Title,
    string Slug,
    DateOnly PublicationDate,
    DateTimeOffset ImportedAtUtc);
