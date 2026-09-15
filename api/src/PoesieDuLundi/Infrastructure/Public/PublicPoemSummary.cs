namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>A flat read record for a published poem in a list (the poem index, an archive month, a
/// series) — no body, since a list never needs the full text.</summary>
public sealed record PublicPoemSummary(Guid Id, string Title, string Slug, DateOnly PublicationDate);

/// <summary>The series a published poem belongs to, as much as a permalink page needs to link to
/// it — not the full <see cref="SeriesWithPoems"/> read record.</summary>
public sealed record SeriesLink(Guid Id, string Title, string Slug);

/// <summary>A flat read record for a single published poem, full body included.</summary>
public sealed record PublicPoem(
    Guid Id, string Title, string Body, string Slug, DateOnly PublicationDate, SeriesLink? Series);

public sealed record PagedPoems(
    IReadOnlyCollection<PublicPoemSummary> Items, int Page, int PageSize, int TotalCount);

public sealed record ArchiveGroup(int Year, int Month, int Count);

public sealed record Archive(
    IReadOnlyCollection<ArchiveGroup> Groups, IReadOnlyCollection<PublicPoemSummary> Entries);

public sealed record SeriesWithPoems(
    Guid Id, string Title, string Slug, string? Description, IReadOnlyCollection<PublicPoemSummary> Poems);
