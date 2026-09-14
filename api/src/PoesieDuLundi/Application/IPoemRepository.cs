using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Application;

/// <summary>
/// The write-side port for <see cref="Poem"/> — defined here (Application), fulfilled by
/// Infrastructure, per docs/ENGINEERING_PRACTICES.md "Layer responsibilities". One bounded
/// context, one aggregate repository: no generic base, add methods as a use case needs them.
/// </summary>
public interface IPoemRepository
{
    Task<Poem?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Poem poem, CancellationToken cancellationToken);

    /// <summary>One poem per Monday: true if a Scheduled or Published poem already occupies
    /// <paramref name="publicationDate"/>.</summary>
    Task<bool> HasScheduledOrPublishedForDateAsync(DateOnly publicationDate, CancellationToken cancellationToken);

    /// <summary>Scheduled poems whose date has arrived — what the materialising job transitions.</summary>
    Task<IReadOnlyCollection<Poem>> GetDueForPublicationAsync(DateOnly asOf, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
