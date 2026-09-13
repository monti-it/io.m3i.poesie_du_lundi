using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.SharedKernel;

public class AggregateRootTests
{
    private sealed record TestEvent : IDomainEvent;

    private sealed class TestAggregate : AggregateRoot
    {
        public void RecordSomething() => AddDomainEvent(new TestEvent());
    }

    [Fact]
    public void New_aggregates_have_no_domain_events()
    {
        var aggregate = new TestAggregate();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Recorded_events_accumulate_until_cleared()
    {
        var aggregate = new TestAggregate();

        aggregate.RecordSomething();
        aggregate.RecordSomething();

        Assert.Equal(2, aggregate.DomainEvents.Count);

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }
}
