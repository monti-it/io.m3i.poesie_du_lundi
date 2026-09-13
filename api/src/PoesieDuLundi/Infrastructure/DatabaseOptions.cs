namespace PoesieDuLundi.Infrastructure;

/// <summary>
/// How <see cref="PoesieDuLundiDbContext"/> should be wired. Production takes the Postgres branch
/// built from <see cref="ConnectionString"/>; a test host that sets
/// <c>Database:Provider=InMemory</c> gets the EF Core InMemory store keyed by
/// <see cref="InMemoryDatabaseName"/> instead — the one seam a test factory uses to swap the
/// provider, no reflective <c>IServiceCollection</c> surgery (mirrors io.m3i.ledgy's
/// <c>ModuleDatabaseOptions</c>, issue #249 there).
/// </summary>
/// <param name="ConnectionString">The Postgres connection string, or <see langword="null"/> when a
/// test host has opted into the InMemory provider.</param>
/// <param name="Provider">The configured <c>Database:Provider</c> — <c>Npgsql</c> (or unset) for
/// production, <c>InMemory</c> for the test host.</param>
/// <param name="InMemoryDatabaseName">A per-factory name so parallel test classes never share an
/// in-memory store; ignored unless the InMemory provider is selected.</param>
public sealed record DatabaseOptions(
    string? ConnectionString,
    string? Provider = null,
    string? InMemoryDatabaseName = null)
{
    public bool UsesInMemoryProvider =>
        string.Equals(Provider, "InMemory", StringComparison.OrdinalIgnoreCase);
}
