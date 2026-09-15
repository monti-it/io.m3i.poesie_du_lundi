namespace PoesieDuLundi.Infrastructure;

/// <summary>A flat read record for the admin series picker — the read side of the CQRS-lite split
/// (docs/ENGINEERING_PRACTICES.md "Read side vs. write side"), not a tracked <c>Series</c>.</summary>
public sealed record SeriesSummary(Guid Id, string Title, string Slug);
