using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PoesieDuLundi.Api.Admin;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Admin;

public sealed class AdminSeriesEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private const string Subject = "alice@example.com";
    private readonly PoesieDuLundiApiFactory _factory;
    private readonly HttpClient _client;

    public AdminSeriesEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage AsAdmin(HttpMethod method, string url) =>
        new(method, url) { Headers = { { ForwardAuthIdentityProvider.UserHeader, Subject } } };

    private async Task<Series> SeedSeriesAsync(string title, int order)
    {
        var series = new Series(title, order);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>();
        await dbContext.Series.AddAsync(series);
        await dbContext.SaveChangesAsync();
        return series;
    }

    [Fact]
    public async Task List_without_a_Remote_User_header_is_unauthorized()
    {
        var response = await _client.GetAsync("/api/admin/series");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_returns_every_seeded_series()
    {
        var series = await SeedSeriesAsync("Saison listée", 1);
        using var request = AsAdmin(HttpMethod.Get, "/api/admin/series");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<SeriesSummaryDto>>();
        Assert.Contains(body!, s => s.Id == series.Id && s.Title == "Saison listée");
    }
}
