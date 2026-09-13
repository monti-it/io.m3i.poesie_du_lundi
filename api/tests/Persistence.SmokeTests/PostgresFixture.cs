using Testcontainers.PostgreSql;
using Xunit;

namespace PoesieDuLundi.Persistence.SmokeTests;

/// <summary>
/// One real <c>postgres:18</c> container shared by every smoke test in the collection. The
/// InMemory suite is the primary one — this exists purely to catch the InMemory-vs-Npgsql
/// divergences ledgy hit before (strongly-typed-id filters, converted collections, native
/// arrays). Needs a Docker daemon; runs only in the <c>test-api-postgres</c> CI job.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
