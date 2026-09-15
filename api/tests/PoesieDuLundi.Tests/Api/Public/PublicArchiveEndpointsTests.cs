using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Api.Public;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Public;

public sealed class PublicArchiveEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private readonly PoesieDuLundiApiFactory _factory;
    private readonly HttpClient _client;

    public PublicArchiveEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedPublishedPoemAsync(string title, DateOnly publicationDate)
    {
        var poem = new Poem(title, "Un corps.");
        poem.Schedule(publicationDate);
        poem.Publish();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task With_no_query_returns_grouped_counts_and_no_entries()
    {
        await SeedPublishedPoemAsync("Archive mars un", new DateOnly(2020, 3, 2));
        await SeedPublishedPoemAsync("Archive mars deux", new DateOnly(2020, 3, 9));

        var response = await _client.GetAsync("/api/archive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ArchiveDto>();
        Assert.Contains(body!.Groups, group => group.Year == 2020 && group.Month == 3 && group.Count == 2);
        Assert.Empty(body.Entries);
    }

    [Fact]
    public async Task With_a_year_and_month_returns_that_months_entries()
    {
        await SeedPublishedPoemAsync("Archive avril un", new DateOnly(2021, 4, 5));
        await SeedPublishedPoemAsync("Archive avril deux", new DateOnly(2021, 4, 12));
        await SeedPublishedPoemAsync("Archive mai", new DateOnly(2021, 5, 3));

        var response = await _client.GetAsync("/api/archive?year=2021&month=4");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ArchiveDto>();
        Assert.Equal(2, body!.Entries.Count);
        Assert.All(body.Entries, entry => Assert.Equal(4, entry.PublicationDate.Month));
    }

    [Fact]
    public async Task With_an_out_of_range_month_returns_400()
    {
        var response = await _client.GetAsync("/api/archive?month=13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
