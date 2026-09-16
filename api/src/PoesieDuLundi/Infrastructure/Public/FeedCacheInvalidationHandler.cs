using Microsoft.AspNetCore.OutputCaching;
using PoesieDuLundi.Domain;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>
/// The "feeds cache bust" side effect docs/ARCHITECTURE.md "Publishing model" describes: a poem
/// becoming or ceasing to be published invalidates every cached feed response (the "Feeds" output
/// cache policy — see <c>Program.cs</c>) so the next request rebuilds it. Best-effort per
/// docs/ENGINEERING_PRACTICES.md "Domain events" — a failed eviction just means the cache expires
/// on its own schedule instead of immediately.
/// </summary>
internal sealed class FeedCacheInvalidationHandler(IOutputCacheStore outputCacheStore) :
    IDomainEventHandler<PoemPublished>, IDomainEventHandler<PoemUnpublished>
{
    private const string FeedsCacheTag = "feeds";

    public Task HandleAsync(PoemPublished domainEvent, CancellationToken cancellationToken) =>
        outputCacheStore.EvictByTagAsync(FeedsCacheTag, cancellationToken).AsTask();

    public Task HandleAsync(PoemUnpublished domainEvent, CancellationToken cancellationToken) =>
        outputCacheStore.EvictByTagAsync(FeedsCacheTag, cancellationToken).AsTask();
}
