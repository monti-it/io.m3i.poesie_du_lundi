using PoesieDuLundi.Infrastructure.TitleCleanup;

namespace PoesieDuLundi.Tests.Infrastructure.TitleCleanup;

public class PoemTitlePrefixCleanupTests
{
    [Theory]
    [InlineData("La poésie du lundi : A Saint Exupéry", "A Saint Exupéry")]
    [InlineData("la poésie du lundi : Le Peintre", "Le Peintre")]
    [InlineData("LA POÉSIE DU LUNDI : Petit oiseau (comptine)", "Petit oiseau (comptine)")]
    [InlineData("La poesie du lundi : Sans accent", "Sans accent")]
    [InlineData("La  poésie   du    lundi  :   Espaces multiples", "Espaces multiples")]
    [InlineData("La poésie du lundi:Sans espace", "Sans espace")]
    [InlineData("La poésie du lundi ： Colonne pleine largeur", "Colonne pleine largeur")]
    [InlineData("   La poésie du lundi : Titre avec espace de tête", "Titre avec espace de tête")]
    public void Strips_the_site_prefix_regardless_of_casing_spacing_colon_style_or_accents(
        string title, string expected)
    {
        Assert.Equal(expected, PoemTitlePrefixCleanup.StripSitePrefix(title));
    }

    [Theory]
    [InlineData("Un poème sans rapport")]
    [InlineData("La poésie du lundi")]
    [InlineData("la poésie du lundi")]
    [InlineData("La poésie du lundi : ")]
    [InlineData("Fwd: La poésie du lundi : Le Château des Croix")]
    [InlineData("Ma poésie du lundi : Titre")]
    public void Leaves_titles_without_a_genuine_leading_prefix_untouched(string title)
    {
        Assert.Null(PoemTitlePrefixCleanup.StripSitePrefix(title));
    }
}
