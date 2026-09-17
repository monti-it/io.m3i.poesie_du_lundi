using PoesieDuLundi.Infrastructure.EmailImport;

namespace PoesieDuLundi.Tests.Infrastructure.EmailImport;

public class EmailTitleResolverTests
{
    private const string SomeBody = "Un corps de poème sans ligne de titre particulière.\n\nMarjan";

    [Theory]
    [InlineData("La poésie du lundi : A Saint Exupéry", "A Saint Exupéry")]
    [InlineData("la poésie du lundi : Le Peintre", "Le Peintre")]
    [InlineData("LA POÉSIE DU LUNDI : Petit oiseau (comptine)", "Petit oiseau (comptine)")]
    [InlineData("La poesie du lundi : Sans accent", "Sans accent")]
    [InlineData("La  poésie   du    lundi  :   Espaces multiples", "Espaces multiples")]
    [InlineData("La poésie du lundi:Sans espace", "Sans espace")]
    [InlineData("La poésie du lundi ： Colonne pleine largeur", "Colonne pleine largeur")]
    [InlineData("   La poésie du lundi : Titre avec espace de tête", "Titre avec espace de tête")]
    [InlineData("Ma poésie du lundi : Titre", "Titre")]
    [InlineData("Fwd: La poésie du lundi : Le Château des Croix", "Le Château des Croix")]
    [InlineData("Fwd: Tr: la poésie du lundi : Titre imbriqué", "Titre imbriqué")]
    [InlineData("La Poésie du oundi : Poète d'un instant", "Poète d'un instant")]
    [InlineData("La poésie di lundi : Hymne à la Terre", "Hymne à la Terre")]
    [InlineData("l a poésie du lundi : Titre avec espace dans 'la'", "Titre avec espace dans 'la'")]
    [InlineData("la poésie du lundi / le gnome", "le gnome")]
    [InlineData("2° poésie du lundi : Toi .... et Lui", "Toi .... et Lui")]
    public void Extracts_the_remainder_after_a_tolerantly_matched_site_prefix(string subject, string expectedTitle)
    {
        var (title, skipReason) = EmailTitleResolver.Resolve(subject, SomeBody);

        Assert.Null(skipReason);
        Assert.Equal(expectedTitle, title);
    }

    [Theory]
    [InlineData("Un poème sans rapport")]
    [InlineData("Au pied de mon arbre")]
    public void Uses_a_custom_subject_verbatim_when_it_has_no_site_prefix_at_all(string subject)
    {
        var (title, skipReason) = EmailTitleResolver.Resolve(subject, SomeBody);

        Assert.Null(skipReason);
        Assert.Equal(subject, title);
    }

    [Theory]
    [InlineData("La poésie du lundi")]
    [InlineData("la poésie du lundi")]
    [InlineData("La poésie du lundi : ")]
    [InlineData("l a poésie du lundi :")]
    [InlineData("Laa poésie du lundi")]
    [InlineData("Ma poésie du lundi")]
    [InlineData("Fwd: la poésie du lundi")]
    public void Falls_back_to_an_asterisk_wrapped_first_body_line_when_the_subject_has_no_remainder(string subject)
    {
        var (title, skipReason) = EmailTitleResolver.Resolve(subject, "*Le vrai titre*\n\nLe corps du poème.");

        Assert.Null(skipReason);
        Assert.Equal("Le vrai titre", title);
    }

    [Fact]
    public void Falls_back_to_the_second_body_line_when_the_first_is_an_epigraph()
    {
        var body = "À la manière d'Edmond Rostand\n\n*La Tirade des pets*\n\nLe corps du poème.";

        var (title, skipReason) = EmailTitleResolver.Resolve("La poésie du lundi", body);

        Assert.Null(skipReason);
        Assert.Equal("La Tirade des pets", title);
    }

    [Theory]
    [InlineData("La poésie du lundi")]
    [InlineData("la poésie du lundi")]
    public void Flags_a_bare_prefix_when_no_body_line_is_asterisk_wrapped(string subject)
    {
        var (title, skipReason) = EmailTitleResolver.Resolve(subject, SomeBody);

        Assert.Null(title);
        Assert.NotNull(skipReason);
    }

    [Theory]
    [InlineData("re: La poésie du lundi")]
    [InlineData("RE: la poésie du lundi")]
    [InlineData("Re : la poésie du lundi : Titre")]
    public void Flags_a_reply_thread_subject_instead_of_importing_it_as_a_poem(string subject)
    {
        var (title, skipReason) = EmailTitleResolver.Resolve(subject, SomeBody);

        Assert.Null(title);
        Assert.Contains("reply", skipReason);
    }

    [Theory]
    [InlineData("La poésie du lundi (dernière de la saison)")]
    [InlineData("La poésie du lundi ... après la lecture d'un roman")]
    [InlineData("La poésie du lundi Madame Tramontane")]
    [InlineData("La poésie du lundi. Deux petits textes aujourd'hui.")]
    public void Flags_trailing_subject_text_that_has_no_recognised_separator_instead_of_guessing(string subject)
    {
        var (title, skipReason) = EmailTitleResolver.Resolve(subject, SomeBody);

        Assert.Null(title);
        Assert.NotNull(skipReason);
    }
}
