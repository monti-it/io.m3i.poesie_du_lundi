using System.Net;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Public;

public sealed class PublicSeoEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private static readonly XNamespace Sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly PoesieDuLundiApiFactory _factory;
    private readonly HttpClient _client;

    public PublicSeoEndpointsTests(PoesieDuLundiApiFactory factory)
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

    private async Task<Series> SeedSeriesAsync(string title)
    {
        var series = new Series(title, order: 1);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Series.AddAsync(series);
        await dbContext.SaveChangesAsync();
        return series;
    }

    private async Task AddAsync(Poem poem)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Sitemap_is_well_formed_and_lists_home_archive_a_poem_and_a_series()
    {
        var poem = await SeedPublishedPoemAsync("Poème sitemap", new DateOnly(2020, 2, 3));
        var series = await SeedSeriesAsync("Saison sitemap");

        var response = await _client.GetAsync("/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        var document = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var locations = document.Root!.Elements(Sitemap + "url")
            .Select(url => url.Element(Sitemap + "loc")!.Value)
            .ToList();
        Assert.Contains(locations, loc => loc.EndsWith("/", StringComparison.Ordinal) && !loc.Contains("poems") && !loc.Contains("series"));
        Assert.Contains(locations, loc => loc.EndsWith("/archive", StringComparison.Ordinal));
        Assert.Contains(locations, loc => loc.EndsWith($"/poems/{poem.Slug.Value}", StringComparison.Ordinal));
        Assert.Contains(locations, loc => loc.EndsWith($"/series/{series.Slug.Value}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Sitemap_excludes_a_poem_scheduled_for_the_future()
    {
        var future = await SeedFutureScheduledPoemAsync(
            "Poème futur sitemap", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        var response = await _client.GetAsync("/sitemap.xml");

        var document = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var locations = document.Root!.Elements(Sitemap + "url").Select(url => url.Element(Sitemap + "loc")!.Value);
        Assert.DoesNotContain(locations, loc => loc.EndsWith($"/poems/{future.Slug.Value}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Robots_txt_allows_everything_and_points_at_the_sitemap()
    {
        var response = await _client.GetAsync("/robots.txt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("User-agent: *", body);
        Assert.Contains("Allow: /", body);
        Assert.Matches(@"Sitemap: https?://[^\s]+/sitemap\.xml", body);
    }
}
