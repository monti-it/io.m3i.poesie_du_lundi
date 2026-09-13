namespace PoesieDuLundi.SharedKernel;

/// <summary>
/// Marker for something an aggregate recorded about its own state change (e.g. a future
/// <c>PoemPublished</c>). Domain only records that something happened — dispatch is an
/// Infrastructure/composition-root concern, wired the day the first handler is actually needed
/// (docs/ENGINEERING_PRACTICES.md "Domain events").
/// </summary>
public interface IDomainEvent;
