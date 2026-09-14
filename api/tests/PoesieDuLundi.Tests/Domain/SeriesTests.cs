using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Tests.Domain;

public class SeriesTests
{
    private static Series CreateSeries(string title = "Saison des pluies", int order = 1, string? description = null) =>
        new(title, order, description);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_title_is_rejected(string title)
    {
        Assert.Throws<ArgumentException>(() => new Series(title, order: 1));
    }

    [Fact]
    public void Title_is_trimmed()
    {
        var series = new Series("  Saison des pluies  ", order: 1);

        Assert.Equal("Saison des pluies", series.Title);
    }

    [Fact]
    public void Slug_is_derived_from_the_title()
    {
        var series = new Series("Saison des pluies", order: 1);

        Assert.Equal("saison-des-pluies", series.Slug.Value);
    }

    [Fact]
    public void Description_defaults_to_null_when_not_given()
    {
        var series = CreateSeries();

        Assert.Null(series.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_description_is_normalized_to_null(string description)
    {
        var series = CreateSeries(description: description);

        Assert.Null(series.Description);
    }

    [Fact]
    public void Description_is_kept_when_given()
    {
        var series = CreateSeries(description: "Des poèmes écrits sous la pluie.");

        Assert.Equal("Des poèmes écrits sous la pluie.", series.Description);
    }

    [Fact]
    public void Order_is_set_at_construction()
    {
        var series = CreateSeries(order: 3);

        Assert.Equal(3, series.Order);
    }

    [Fact]
    public void EnsureCanBeDeleted_succeeds_when_the_series_has_no_poems()
    {
        var series = CreateSeries();

        var result = series.EnsureCanBeDeleted(hasPoems: false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void EnsureCanBeDeleted_fails_when_the_series_still_has_poems()
    {
        var series = CreateSeries();

        var result = series.EnsureCanBeDeleted(hasPoems: true);

        Assert.True(result.IsFailure);
    }
}
