namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>A flat read record for a published poem in a list (the poem index, an archive month, a
/// series) — no body, since a list never needs the full text.</summary>
public sealed record PublicPoemSummary(Guid Id, string Title, string Slug, DateOnly PublicationDate);

/// <summary>The series a published poem belongs to, as much as a permalink page needs to link to
/// it — not the full <see cref="SeriesWithPoems"/> read record.</summary>
public sealed record SeriesLink(Guid Id, string Title, string Slug);

/// <summary>The adjacent poem in publication-date order — what a permalink page's prev/next
/// navigation links to.</summary>
public sealed record PublicPoemNeighbor(string Slug, string Title);

/// <summary>A flat read record for a single published poem, full body included.</summary>
public sealed record PublicPoem(
    Guid Id, string Title, string Body, string Slug, DateOnly PublicationDate, SeriesLink? Series,
    PublicPoemNeighbor? Previous, PublicPoemNeighbor? Next);

public sealed record PagedPoems(
    IReadOnlyCollection<PublicPoemSummary> Items, int Page, int PageSize, int TotalCount);

public sealed record ArchiveGroup(int Year, int Month, int Count);

public sealed record Archive(
    IReadOnlyCollection<ArchiveGroup> Groups, IReadOnlyCollection<PublicPoemSummary> Entries);

public sealed record SeriesWithPoems(
    Guid Id, string Title, string Slug, string? Description, IReadOnlyCollection<PublicPoemSummary> Poems);

/// <summary>One tag, and how many published poems carry it — <see cref="ListTagsQuery"/>'s read
/// record for <c>GET /api/tags</c>.</summary>
public sealed record TagCount(string Tag, int Count);
