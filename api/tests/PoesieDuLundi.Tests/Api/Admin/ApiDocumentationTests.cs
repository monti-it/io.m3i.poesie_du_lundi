using System.Net;
using PoesieDuLundi.Api.Admin;
using PoesieDuLundi.Tests.Api;

namespace PoesieDuLundi.Tests.Api.Admin;

public sealed class ApiDocumentationTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private readonly HttpClient _client;

    public ApiDocumentationTests(PoesieDuLundiApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_json_is_absent_in_Testing()
    {
        var response = await _client.GetAsync("/api/admin/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class ApiDocumentationEnabledTests : IClassFixture<ApiDocsEnabledApiFactory>
{
    private readonly HttpClient _client;

    public ApiDocumentationEnabledTests(ApiDocsEnabledApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_json_without_a_Remote_User_header_is_unauthorized()
    {
        var response = await _client.GetAsync("/api/admin/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_json_with_a_Remote_User_header_renders_the_OpenAPI_document()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/swagger/v1/swagger.json");
        request.Headers.Add(ForwardAuthIdentityProvider.UserHeader, "alice@example.com");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_UI_with_a_Remote_User_header_renders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/swagger/index.html");
        request.Headers.Add(ForwardAuthIdentityProvider.UserHeader, "alice@example.com");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
