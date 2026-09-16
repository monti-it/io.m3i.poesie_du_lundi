using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure;

/// <summary>
/// Wires the dispatch pipe docs/ENGINEERING_PRACTICES.md "Domain events" describes: captures every
/// tracked <see cref="AggregateRoot"/>'s buffered events before the write lands, then — only once
/// the transaction has actually committed — dispatches them and clears the buffers. Registered
/// scoped (see <see cref="InfrastructureServiceCollectionExtensions"/>), one instance per
/// <c>DbContext</c>, so the pending-events buffer below is never shared across concurrent requests
/// the way a singleton interceptor's would be.
/// </summary>
public sealed class DomainEventDispatchInterceptor(DomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    private List<IDomainEvent> _pendingEvents = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        _pendingEvents = CollectPendingEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var events = _pendingEvents;
        _pendingEvents = [];
        ClearPendingEvents(eventData.Context);

        await dispatcher.DispatchAsync(events, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private static List<IDomainEvent> CollectPendingEvents(DbContext? context) =>
        context is null
            ? []
            : context.ChangeTracker.Entries<AggregateRoot>()
                .SelectMany(entry => entry.Entity.DomainEvents)
                .ToList();

    private static void ClearPendingEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<AggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }
    }
}
