namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>One <c>.eml</c> file successfully parsed into poem-shaped data (issue #52). Not yet a
/// <see cref="Domain.Poem"/> — slug assignment and persistence happen once the whole archive has
/// been scanned, so duplicates across the batch can be resolved first.</summary>
public sealed record ParsedEmail(
    string MessageId,
    string Title,
    string Body,
    DateOnly PublicationDate,
    string SourceFile);
