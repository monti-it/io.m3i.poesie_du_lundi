using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.SharedKernel;
using Xunit;

namespace PoesieDuLundi.Persistence.SmokeTests;

/// <summary>
/// The <c>TitleFromBareSitePrefixBody</c> data migration (issue #108) is raw Postgres regex SQL,
/// so it can only be checked against real Npgsql: seed a poem at the migration before it, then
/// migrate to the latest and read it back.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TitleFromBareSitePrefixBodyMigrationTests(PostgresFixture postgres)
{
    private const string MigrationBefore = "20260929115115_StripTitleSitePrefix";

    private PoesieDuLundiDbContext NewContext() => new(
        new DbContextOptionsBuilder<PoesieDuLundiDbContext>().UseNpgsql(postgres.ConnectionString).Options);

    [Theory]
    [InlineData("La poésie du lundi", "Viens !\n\nJ'inventerai pour toi\nDes mots", "Viens !", "J'inventerai pour toi\nDes mots")]
    [InlineData("la Poésie du lundi.", "*La Vie*\r\n\r\n  \r\nPour l'enfance", "La Vie", "Pour l'enfance")]
    [InlineData("Laa poésie du lundi", "Réveil\n\n  Un rayon de soleil", "Réveil", "  Un rayon de soleil")]
    [InlineData("La poésie di lundi", "\nMa poésie\n\nDes vers", "Ma poésie", "Des vers")]
    public async Task Takes_the_title_from_a_standalone_first_body_line(
        string title, string body, string expectedTitle, string expectedBody)
    {
        var (reloaded, slug) = await SeedAndMigrate(title, body);

        Assert.Equal(expectedTitle, reloaded.Title);
        Assert.Equal(expectedBody, reloaded.Body);
        Assert.Equal(slug, reloaded.Slug);
    }

    [Theory]
    // No standalone first line: the first line runs straight into the verse.
    [InlineData("La poésie du lundi", "J'inventerai pour toi\nDes mots simples")]
    // A first line too long to be a title.
    [InlineData("La poésie du lundi", "Bonjour à tous, voici la poésie de cette semaine, écrite un dimanche soir au coin du feu\n\nDes vers")]
    // Nothing after the first line.
    [InlineData("La poésie du lundi", "Viens !\n\n")]
    // Extra text after the site name: fixed by hand, not by the migration.
    [InlineData("La poésie du lundi ... après la lecture d'un roman", "Viens !\n\nDes vers")]
    // A real title.
    [InlineData("Au pied de mon arbre", "Viens !\n\nDes vers")]
    public async Task Leaves_other_poems_alone(string title, string body)
    {
        var (reloaded, slug) = await SeedAndMigrate(title, body);

        Assert.Equal(title, reloaded.Title);
        Assert.Equal(body, reloaded.Body);
        Assert.Equal(slug, reloaded.Slug);
    }

    private async Task<(Poem Reloaded, Slug Slug)> SeedAndMigrate(string title, string body)
    {
        await using (var migrate = NewContext())
        {
            await migrate.GetService<IMigrator>().MigrateAsync(MigrationBefore);
        }

        // Many seeded poems share a bare title, so give each a unique slug.
        var poem = new Poem(title, body);
        poem.ChangeSlug(new Slug($"poeme-{Guid.NewGuid():N}"));
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
        return (await read.Poems.SingleAsync(p => p.Id == poem.Id), poem.Slug);
    }
}
