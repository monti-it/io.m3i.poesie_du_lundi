using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using Xunit;

namespace PoesieDuLundi.Persistence.SmokeTests;

/// <summary>
/// The <c>StripTitleSitePrefix</c> data migration (issue #62) is raw Postgres regex SQL, so it can
/// only be checked against real Npgsql: seed titles at the migration before it, then migrate to
/// the latest and read them back.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class StripTitleSitePrefixMigrationTests(PostgresFixture postgres)
{
    private const string MigrationBefore = "20260915122649_AddPoemTags";

    private PoesieDuLundiDbContext NewContext() => new(
        new DbContextOptionsBuilder<PoesieDuLundiDbContext>().UseNpgsql(postgres.ConnectionString).Options);

    [Theory]
    [InlineData("La poésie du lundi : Migration simple", "Migration simple")]
    [InlineData("LA POÉSIE DU LUNDI : Migration majuscules", "Migration majuscules")]
    [InlineData("la poesie du lundi:Migration sans accent", "Migration sans accent")]
    [InlineData("Ma poésie du lundi ： Migration pleine largeur", "Migration pleine largeur")]
    [InlineData("2° poésie du lundi : Migration numérotée", "Migration numérotée")]
    [InlineData("l a poésie du lundi / Migration barre oblique", "Migration barre oblique")]
    [InlineData("La poésie du lundi :", "La poésie du lundi :")]
    [InlineData("Migration sans préfixe : la poésie du lundi", "Migration sans préfixe : la poésie du lundi")]
    public async Task Strips_the_site_prefix_from_existing_titles(string title, string expected)
    {
        await using (var migrate = NewContext())
        {
            await migrate.GetService<IMigrator>().MigrateAsync(MigrationBefore);
        }

        var poem = new Poem(title, "Des mots simples.");
        await using (var write = NewContext())
        {
            write.Poems.Add(poem);
            await write.SaveChangesAsync();
        }

        await using (var migrate = NewContext())
        {
            await migrate.Database.MigrateAsync();
        }

        await using var read = NewContext();
        var reloaded = await read.Poems.SingleAsync(p => p.Id == poem.Id);

        Assert.Equal(expected, reloaded.Title);
        Assert.Equal(poem.Slug, reloaded.Slug);
    }
}
