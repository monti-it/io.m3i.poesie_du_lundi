using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Api.Public;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Public;

public sealed class PublicSeriesEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private readonly PoesieDuLundiApiFactory _factory;
    private readonly HttpClient _client;

    public PublicSeriesEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Series Series, Poem Poem)> SeedSeriesWithAPublishedPoemAsync()
    {
        var series = new Series("Une saison publique", 1, "Description de la saison");
        var poem = new Poem("Un poème de la saison", "Un corps.", series.Id);
        poem.Schedule(new DateOnly(2020, 5, 4));
        poem.Publish();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Series.AddAsync(series);
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();

        return (series, poem);
    }

    [Fact]
    public async Task Returns_the_series_and_its_published_poems()
    {
        var (series, poem) = await SeedSeriesWithAPublishedPoemAsync();

        var response = await _client.GetAsync($"/api/series/{series.Slug.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SeriesDto>();
        Assert.Equal(series.Id, body!.Id);
        Assert.Equal(series.Description, body.Description);
        Assert.Contains(body.Poems, p => p.Id == poem.Id);
    }

    [Fact]
    public async Task An_unknown_slug_returns_404()
    {
        var response = await _client.GetAsync("/api/series/une-saison-inconnue");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
