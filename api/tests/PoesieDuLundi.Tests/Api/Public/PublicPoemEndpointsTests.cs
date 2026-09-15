using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Api.Public;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Public;

public sealed class PublicPoemEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private readonly PoesieDuLundiApiFactory _factory;
    private readonly HttpClient _client;

    public PublicPoemEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Poem> SeedPublishedPoemAsync(string title, DateOnly publicationDate)
    {
        var poem = new Poem(title, "Un corps.");
        poem.Schedule(publicationDate);
        poem.Publish();
        await AddAsync(poem);
        return poem;
    }

    private async Task<Poem> SeedDraftPoemAsync(string title)
    {
        var poem = new Poem(title, "Un corps.");
        await AddAsync(poem);
        return poem;
    }

    private async Task AddAsync(Poem poem)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task List_returns_only_published_poems()
    {
        var published = await SeedPublishedPoemAsync("Publié un", new DateOnly(2020, 1, 6));
        await SeedDraftPoemAsync("Brouillon un");

        var response = await _client.GetAsync("/api/poems");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedPoemsDto>();
        Assert.Contains(body!.Items, poem => poem.Id == published.Id);
        Assert.DoesNotContain(body.Items, poem => poem.Title == "Brouillon un");
    }

    [Fact]
    public async Task Get_by_slug_returns_the_poem()
    {
        var poem = await SeedPublishedPoemAsync("Poème détail public", new DateOnly(2020, 2, 3));

        var response = await _client.GetAsync($"/api/poems/{poem.Slug.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PublicPoemDto>();
        Assert.Equal(poem.Id, body!.Id);
        Assert.Equal(poem.Body, body.Body);
    }

    [Fact]
    public async Task Get_by_slug_of_a_draft_returns_404()
    {
        var draft = await SeedDraftPoemAsync("Brouillon détail public");

        var response = await _client.GetAsync($"/api/poems/{draft.Slug.Value}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_an_unknown_slug_returns_404()
    {
        var response = await _client.GetAsync("/api/poems/un-slug-inconnu");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task This_monday_returns_the_current_weeks_poem_when_one_exists()
    {
        var poem = await SeedPublishedPoemAsync("Poème de cette semaine", CurrentWeeksMonday());

        var response = await _client.GetAsync("/api/poems/this-monday");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PublicPoemDto>();
        Assert.Equal(poem.Id, body!.Id);
    }

    private static DateOnly CurrentWeeksMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var offset = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-offset);
    }
}
