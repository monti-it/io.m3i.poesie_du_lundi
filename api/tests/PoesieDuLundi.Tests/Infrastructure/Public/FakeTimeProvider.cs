namespace PoesieDuLundi.Tests.Infrastructure.Public;

/// <summary>A fixed clock, shared across the public read-model query tests in this namespace —
/// each needs "today" pinned to assert "effectively published" and "this week's Monday" logic.</summary>
internal sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
