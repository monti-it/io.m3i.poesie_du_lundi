using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Infrastructure;

/// <summary>A flat read record for the admin poem list — the read side of the CQRS-lite split
/// (docs/ENGINEERING_PRACTICES.md "Read side vs. write side"), not a tracked <see cref="Poem"/>.</summary>
public sealed record PoemSummary(
    Guid Id, string Title, string Slug, PoemStatus Status, DateOnly? PublicationDate, Guid? SeriesId,
    IReadOnlyCollection<string> Tags);
