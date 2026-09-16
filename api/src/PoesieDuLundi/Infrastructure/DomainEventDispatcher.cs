using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure;

/// <summary>
/// Resolves and invokes every <see cref="IDomainEventHandler{TEvent}"/> registered for a given
/// event's runtime type. One dispatcher, closed over generic handler resolution by reflection
/// since the interceptor that calls it only ever sees the <see cref="IDomainEvent"/> base type.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider)
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in (IEnumerable<object>)serviceProvider.GetServices(handlerType))
            {
                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
