using Microsoft.AspNetCore.OutputCaching;
using NSubstitute;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Tests.Infrastructure.Public;

public class FeedCacheInvalidationHandlerTests
{
    [Fact]
    public async Task PoemPublished_evicts_the_feeds_cache_tag()
    {
        var outputCacheStore = Substitute.For<IOutputCacheStore>();
        var handler = new FeedCacheInvalidationHandler(outputCacheStore);

        await handler.HandleAsync(new PoemPublished(Guid.NewGuid(), new DateOnly(2026, 9, 21)), CancellationToken.None);

        await outputCacheStore.Received(1).EvictByTagAsync("feeds", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PoemUnpublished_evicts_the_feeds_cache_tag()
    {
        var outputCacheStore = Substitute.For<IOutputCacheStore>();
        var handler = new FeedCacheInvalidationHandler(outputCacheStore);

        await handler.HandleAsync(new PoemUnpublished(Guid.NewGuid()), CancellationToken.None);

        await outputCacheStore.Received(1).EvictByTagAsync("feeds", Arg.Any<CancellationToken>());
    }
}
