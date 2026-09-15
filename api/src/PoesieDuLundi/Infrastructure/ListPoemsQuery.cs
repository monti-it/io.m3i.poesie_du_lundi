using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Infrastructure;

/// <summary>The read side of the admin poem list — an <c>AsNoTracking()</c> query projected
/// straight to <see cref="PoemSummary"/>, not a method on <c>IPoemRepository</c>
/// (docs/ENGINEERING_PRACTICES.md "Read side vs. write side").</summary>
public sealed class ListPoemsQuery(PoesieDuLundiDbContext dbContext)
{
    public async Task<IReadOnlyCollection<PoemSummary>> HandleAsync(
        PoemStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Poems.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(poem => poem.Status == status);
        }

        var poems = await query.ToListAsync(cancellationToken);
        return poems
            .Select(poem => new PoemSummary(
                poem.Id, poem.Title, poem.Slug.Value, poem.Status, poem.PublicationDate, poem.SeriesId))
            .ToList();
    }
}
