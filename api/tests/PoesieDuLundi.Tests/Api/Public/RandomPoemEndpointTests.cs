using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Api.Public;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Tests.Api.Public;

/// <summary><c>GET /api/poems/random</c> (issue #96). A fresh factory — so a fresh in-memory
/// database — per test, unlike <see cref="PublicPoemEndpointsTests"/>'s shared fixture: these
/// assertions depend on exactly which poems exist.</summary>
public sealed class RandomPoemEndpointTests : IDisposable
{
    // Enough draws that a pick ignoring the filter would show up with overwhelming probability.
    private const int Draws = 20;

    private readonly PoesieDuLundiApiFactory _factory = new();
    private readonly HttpClient _client;

    public RandomPoemEndpointTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    private async Task<Poem> SeedAsync(string title, DateOnly? publishedOn)
    {
        var poem = new Poem(title, "Un corps.");
        if (publishedOn is { } date)
        {
            poem.Schedule(date);
            poem.Publish();
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
        return poem;
    }

    private async Task<IReadOnlyCollection<string>> DrawSlugsAsync(string url)
    {
        var slugs = new HashSet<string>();
        for (var i = 0; i < Draws; i++)
        {
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            slugs.Add((await response.Content.ReadFromJsonAsync<PublicPoemSummaryDto>())!.Slug);
        }

        return slugs;
    }

    [Fact]
    public async Task Random_returns_404_when_no_poem_is_published()
    {
        await SeedAsync("Brouillon seul", publishedOn: null);

        var response = await _client.GetAsync("/api/poems/random");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Random_only_picks_published_poems()
    {
        var published = await SeedAsync("Publié au hasard", new DateOnly(2020, 1, 6));
        await SeedAsync("Brouillon au hasard", publishedOn: null);

        var slugs = await DrawSlugsAsync("/api/poems/random");

        Assert.Equal([published.Slug.Value], slugs);
    }

    [Fact]
    public async Task Random_skips_excluded_slugs()
    {
        var first = await SeedAsync("Premier", new DateOnly(2020, 1, 6));
        var second = await SeedAsync("Deuxième", new DateOnly(2020, 1, 13));
        var third = await SeedAsync("Troisième", new DateOnly(2020, 1, 20));

        var slugs = await DrawSlugsAsync($"/api/poems/random?exclude={first.Slug.Value}&exclude={second.Slug.Value}");

        Assert.Equal([third.Slug.Value], slugs);
    }

    [Fact]
    public async Task Random_falls_back_to_any_published_poem_when_everything_is_excluded()
    {
        var only = await SeedAsync("Unique", new DateOnly(2020, 1, 6));

        var response = await _client.GetAsync($"/api/poems/random?exclude={only.Slug.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(only.Slug.Value, (await response.Content.ReadFromJsonAsync<PublicPoemSummaryDto>())!.Slug);
    }
}
