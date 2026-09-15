using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Api.Public;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.SharedKernel;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Public;

public sealed class PublicTagEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private readonly PoesieDuLundiApiFactory _factory;
    private readonly HttpClient _client;

    public PublicTagEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedPublishedPoemAsync(string title, DateOnly publicationDate, params string[] tags)
    {
        var poem = new Poem(title, "Un corps.");
        poem.ChangeTags(tags.Select(tag => new Slug(tag)).ToList());
        poem.Schedule(publicationDate);
        poem.Publish();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Poems.AddAsync(poem);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Returns_every_tag_with_its_count()
    {
        await SeedPublishedPoemAsync("Poème tag un", new DateOnly(2020, 6, 1), "amour", "ete");
        await SeedPublishedPoemAsync("Poème tag deux", new DateOnly(2020, 6, 8), "amour");

        var response = await _client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TagCountDto>>();
        Assert.Contains(body!, tag => tag.Tag == "amour" && tag.Count >= 2);
        Assert.Contains(body!, tag => tag.Tag == "ete" && tag.Count >= 1);
    }
}
