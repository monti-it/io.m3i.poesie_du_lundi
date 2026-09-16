using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Public;

public sealed class PublicFeedEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    private readonly PoesieDuLundiApiFactory _factory;
    private readonly HttpClient _client;

    public PublicFeedEndpointsTests(PoesieDuLundiApiFactory factory)
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

    private async Task<Poem> SeedFutureScheduledPoemAsync(string title, DateOnly publicationDate)
    {
        var poem = new Poem(title, "Un corps.");
        poem.Schedule(publicationDate);
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
    public async Task Rss_is_well_formed_and_contains_a_published_poem_with_a_stable_guid()
    {
        var poem = await SeedPublishedPoemAsync("Poème RSS", new DateOnly(2020, 1, 6));

        var response = await _client.GetAsync("/api/feed.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/rss+xml", response.Content.Headers.ContentType?.MediaType);
        var document = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = document.Root!.Element("channel")!.Elements("item")
            .Single(i => i.Element("title")!.Value == poem.Title);
        Assert.Equal($"urn:uuid:{poem.Id}", item.Element("guid")!.Value);
    }

    [Fact]
    public async Task Atom_is_well_formed_and_contains_a_published_poem_with_a_stable_id()
    {
        var poem = await SeedPublishedPoemAsync("Poème Atom", new DateOnly(2020, 1, 13));

        var response = await _client.GetAsync("/api/atom.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/atom+xml", response.Content.Headers.ContentType?.MediaType);
        var document = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var entry = document.Root!.Elements(Atom + "entry").Single(e => e.Element(Atom + "title")!.Value == poem.Title);
        Assert.Equal($"urn:uuid:{poem.Id}", entry.Element(Atom + "id")!.Value);
    }

    [Fact]
    public async Task Json_feed_is_well_formed_and_contains_a_published_poem_with_a_stable_id()
    {
        var poem = await SeedPublishedPoemAsync("Poème JSON", new DateOnly(2020, 1, 20));

        var response = await _client.GetAsync("/api/feed.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://jsonfeed.org/version/1.1", document.RootElement.GetProperty("version").GetString());
        var items = document.RootElement.GetProperty("items").EnumerateArray();
        var item = items.Single(i => i.GetProperty("title").GetString() == poem.Title);
        Assert.Equal($"urn:uuid:{poem.Id}", item.GetProperty("id").GetString());
    }

    [Fact]
    public async Task A_poem_scheduled_for_the_future_is_absent_from_every_feed_format()
    {
        var future = await SeedFutureScheduledPoemAsync("Poème futur", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        var rss = XDocument.Parse(await (await _client.GetAsync("/api/feed.xml")).Content.ReadAsStringAsync());
        var atom = XDocument.Parse(await (await _client.GetAsync("/api/atom.xml")).Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await (await _client.GetAsync("/api/feed.json")).Content.ReadAsStringAsync());

        Assert.DoesNotContain(
            rss.Root!.Element("channel")!.Elements("item"), item => item.Element("title")!.Value == future.Title);
        Assert.DoesNotContain(
            atom.Root!.Elements(Atom + "entry"), entry => entry.Element(Atom + "title")!.Value == future.Title);
        Assert.DoesNotContain(
            json.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("title").GetString() == future.Title);
    }

    [Fact]
    public async Task Feeds_order_items_newest_first()
    {
        var older = await SeedPublishedPoemAsync("Plus ancien pour flux", new DateOnly(2019, 1, 7));
        var newer = await SeedPublishedPoemAsync("Plus récent pour flux", new DateOnly(2019, 1, 14));

        using var json = JsonDocument.Parse(await (await _client.GetAsync("/api/feed.json")).Content.ReadAsStringAsync());

        var titles = json.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("title").GetString())
            .ToList();
        Assert.True(titles.IndexOf(newer.Title) < titles.IndexOf(older.Title));
    }
}
