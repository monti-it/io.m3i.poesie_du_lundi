using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using Xunit;

namespace PoesieDuLundi.Persistence.SmokeTests;

/// <summary>
/// Real-Postgres gate for the migration pipe and the `Poem` mapping
/// (docs/ENGINEERING_PRACTICES.md "Database strategy") — the `Slug`/`Status` `HasConversion`
/// mappings and the `DateOnly` column are exactly the kind of thing that passes EF Core InMemory
/// and throws against real Npgsql.
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
        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_AddPoems", StringComparison.Ordinal));

        var canConnect = await read.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public async Task Round_trips_a_scheduled_poem_through_npgsql()
    {
        await using (var migrate = NewContext())
        {
            await migrate.Database.MigrateAsync();
        }

        var poem = new Poem("Au pied de mon arbre", "Des mots simples.");
        poem.Schedule(new DateOnly(2026, 9, 21));

        await using (var write = NewContext())
        {
            write.Poems.Add(poem);
            await write.SaveChangesAsync();
        }

        await using var read2 = NewContext();
        var reloaded = await read2.Poems.SingleAsync(p => p.Id == poem.Id);

        Assert.Equal(poem.Slug, reloaded.Slug);
        Assert.Equal(PoemStatus.Scheduled, reloaded.Status);
        Assert.Equal(poem.PublicationDate, reloaded.PublicationDate);
    }
}
