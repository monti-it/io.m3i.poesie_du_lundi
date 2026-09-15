using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Infrastructure;

public sealed class PoemRepository(PoesieDuLundiDbContext dbContext) : IPoemRepository
{
    public Task<Poem?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Poems.SingleOrDefaultAsync(poem => poem.Id == id, cancellationToken);

    public async Task AddAsync(Poem poem, CancellationToken cancellationToken) =>
        await dbContext.Poems.AddAsync(poem, cancellationToken);

    public void Remove(Poem poem) => dbContext.Poems.Remove(poem);

    public Task<bool> HasScheduledOrPublishedForDateAsync(
        DateOnly publicationDate, CancellationToken cancellationToken) =>
        dbContext.Poems.AnyAsync(
            poem => poem.PublicationDate == publicationDate
                && (poem.Status == PoemStatus.Scheduled || poem.Status == PoemStatus.Published),
            cancellationToken);

    public async Task<IReadOnlyCollection<Poem>> GetDueForPublicationAsync(
        DateOnly asOf, CancellationToken cancellationToken) =>
        await dbContext.Poems
            .Where(poem => poem.Status == PoemStatus.Scheduled && poem.PublicationDate <= asOf)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
