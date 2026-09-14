using System.Net;
using System.Net.Http.Json;
using PoesieDuLundi.Api.Admin;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Admin;

public sealed class AdminAuthEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private readonly HttpClient _client;

    public AdminAuthEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Anonymous_public_route_returns_ok()
    {
        var response = await _client.GetAsync("/api/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_route_without_a_Remote_User_header_is_unauthorized()
    {
        var response = await _client.GetAsync("/api/admin/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_route_with_a_Remote_User_header_echoes_the_resolved_subject()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/me");
        request.Headers.Add(ForwardAuthIdentityProvider.UserHeader, "alice@example.com");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MeDto>();
        Assert.Equal("alice@example.com", body!.Subject);
    }
}
