using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>The archive: year/month counts for the whole corpus (a browsable tree — cheap, this
/// corpus is small) plus the entries for one year (and, narrowed further, one month) once the
/// caller has picked one to drill into. No <paramref name="year"/> means "just the tree" — entries
/// come back empty rather than repeating what <see cref="ListPublishedPoemsQuery"/> already
/// serves.</summary>
public sealed class GetArchiveQuery(PoesieDuLundiDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<Archive> HandleAsync(int? year, int? month, CancellationToken cancellationToken)
    {
        var published = dbContext.Poems.AsNoTracking().Where(PublishedPoems.AsOf(Clock.Today(timeProvider)));

        // Materialised as an anonymous type first, then mapped to ArchiveGroup client-side —
        // projecting straight into ArchiveGroup's constructor from a GroupBy/Count() doesn't
        // translate against Npgsql (it does against EF Core InMemory, one of the
        // InMemory-vs-real-Postgres gotchas docs/ENGINEERING_PRACTICES.md warns about).
        var rawGroups = await published
            .GroupBy(poem => new { poem.PublicationDate!.Value.Year, poem.PublicationDate.Value.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var groups = rawGroups
            .Select(group => new ArchiveGroup(group.Year, group.Month, group.Count))
            .OrderByDescending(group => group.Year)
            .ThenByDescending(group => group.Month)
            .ToList();

        if (year is null)
        {
            return new Archive(groups, []);
        }

        var entriesQuery = published.Where(poem => poem.PublicationDate!.Value.Year == year);
        if (month is not null)
        {
            entriesQuery = entriesQuery.Where(poem => poem.PublicationDate!.Value.Month == month);
        }

        var entries = await entriesQuery
            .OrderByDescending(poem => poem.PublicationDate)
            .Select(poem => new PublicPoemSummary(poem.Id, poem.Title, poem.Slug.Value, poem.PublicationDate!.Value))
            .ToListAsync(cancellationToken);

        return new Archive(groups, entries);
    }
}
