using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Infrastructure;
using Xunit;

namespace PoesieDuLundi.Persistence.SmokeTests;

/// <summary>
/// Real-Postgres gate for the migration pipe (docs/ENGINEERING_PRACTICES.md "Database strategy").
/// No aggregate exists yet (`Poem` lands in #14), so this proves the plumbing itself — migrate,
/// then round-trip a query through Npgsql — rather than a domain row; extend it into an aggregate
/// round-trip the moment the first `DbSet&lt;T&gt;` appears.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class DatabaseMigrationSmokeTests(PostgresFixture postgres)
{
    private PoesieDuLundiDbContext NewContext() => new(
        new DbContextOptionsBuilder<PoesieDuLundiDbContext>().UseNpgsql(postgres.ConnectionString).Options);

    [Fact]
    public async Task Migrates_and_round_trips_a_query_through_npgsql()
    {
        await using (var migrate = NewContext())
        {
            await migrate.Database.MigrateAsync();
        }

        await using var read = NewContext();

        var appliedMigrations = await read.Database.GetAppliedMigrationsAsync();
        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));

        var canConnect = await read.Database.CanConnectAsync();
        Assert.True(canConnect);
    }
}
