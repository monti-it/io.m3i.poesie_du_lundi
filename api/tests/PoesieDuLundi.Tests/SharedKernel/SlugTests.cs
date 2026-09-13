using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.SharedKernel;

public class SlugTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_is_rejected(string value)
    {
        Assert.Throws<ArgumentException>(() => new Slug(value));
    }

    [Theory]
    [InlineData("Not A Slug")]
    [InlineData("has_underscore")]
    [InlineData("double--hyphen")]
    [InlineData("-leading-hyphen")]
    [InlineData("trailing-hyphen-")]
    [InlineData("has-accent-é")]
    public void Malformed_values_are_rejected(string value)
    {
        Assert.Throws<ArgumentException>(() => new Slug(value));
    }

    [Fact]
    public void Well_formed_values_are_accepted_as_is()
    {
        var slug = new Slug("au-pied-de-mon-arbre");

        Assert.Equal("au-pied-de-mon-arbre", slug.Value);
        Assert.Equal("au-pied-de-mon-arbre", slug);
    }

    [Fact]
    public void FromText_strips_accents_and_punctuation()
    {
        var slug = Slug.FromText("Au pied de mon arbre, après la pluie…");

        Assert.Equal("au-pied-de-mon-arbre-apres-la-pluie", slug.Value);
    }

    [Fact]
    public void FromText_collapses_repeated_separators()
    {
        var slug = Slug.FromText("La poésie du lundi : les quatre saisons");

        Assert.Equal("la-poesie-du-lundi-les-quatre-saisons", slug.Value);
    }

    [Fact]
    public void FromText_with_no_slug_able_characters_throws()
    {
        Assert.Throws<ArgumentException>(() => Slug.FromText("…!!!"));
    }

    [Fact]
    public void Slugs_with_the_same_value_are_equal()
    {
        Assert.Equal(new Slug("les-quatre-saisons"), new Slug("les-quatre-saisons"));
    }
}
