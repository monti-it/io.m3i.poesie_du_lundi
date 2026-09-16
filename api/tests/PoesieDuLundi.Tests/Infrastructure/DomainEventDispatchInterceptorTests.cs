using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.Infrastructure;

/// <summary>Proves the actual dispatch pipe docs/ENGINEERING_PRACTICES.md "Domain events" describes:
/// a domain event raised on a tracked aggregate reaches its handler only once <c>SaveChangesAsync</c>
/// has committed, and is never redelivered on a later, unrelated save.</summary>
public class DomainEventDispatchInterceptorTests
{
    private static PoesieDuLundiDbContext CreateDbContext(DomainEventDispatchInterceptor interceptor) =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options);

    private static (DomainEventDispatchInterceptor Interceptor, IDomainEventHandler<PoemPublished> Handler)
        CreateInterceptorWithHandler()
    {
        var handler = Substitute.For<IDomainEventHandler<PoemPublished>>();
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider);
        return (new DomainEventDispatchInterceptor(dispatcher), handler);
    }

    [Fact]
    public async Task Dispatches_a_raised_event_to_its_handler_once_the_save_commits()
    {
        var (interceptor, handler) = CreateInterceptorWithHandler();
        await using var dbContext = CreateDbContext(interceptor);
        var poem = new Poem("Un titre", "Un corps.");
        poem.Schedule(new DateOnly(2026, 9, 21));
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();

        poem.Publish();
        await dbContext.SaveChangesAsync();

        await handler.Received(1).HandleAsync(
            Arg.Is<PoemPublished>(e => e.PoemId == poem.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clears_dispatched_events_so_a_later_unrelated_save_does_not_redeliver_them()
    {
        var (interceptor, handler) = CreateInterceptorWithHandler();
        await using var dbContext = CreateDbContext(interceptor);
        var poem = new Poem("Un titre", "Un corps.");
        poem.Schedule(new DateOnly(2026, 9, 21));
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
        poem.Publish();
        await dbContext.SaveChangesAsync();
        handler.ClearReceivedCalls();

        poem.ChangeSlug(new Slug("nouveau-slug"));
        dbContext.Poems.Update(poem);
        await dbContext.SaveChangesAsync();

        await handler.DidNotReceive().HandleAsync(Arg.Any<PoemPublished>(), Arg.Any<CancellationToken>());
    }
}
