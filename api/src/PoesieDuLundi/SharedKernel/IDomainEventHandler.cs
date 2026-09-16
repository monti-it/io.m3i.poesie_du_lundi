namespace PoesieDuLundi.SharedKernel;

/// <summary>
/// Reacts to one <see cref="IDomainEvent"/> type once dispatch has actually landed (see
/// <see cref="IDomainEvent"/>). Per docs/ENGINEERING_PRACTICES.md "Domain events": delivery is
/// in-process, synchronous, post-commit — a handler must be idempotent and best-effort, never the
/// only path to a must-happen-exactly-once side effect.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
