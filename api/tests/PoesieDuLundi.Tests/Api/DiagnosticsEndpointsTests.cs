using System.Net;
using System.Net.Http.Json;

namespace PoesieDuLundi.Tests.Api;

public sealed class DiagnosticsEndpointsTests : IClassFixture<PoesieDuLundiApiFactory>
{
    private readonly HttpClient _client;

    public DiagnosticsEndpointsTests(PoesieDuLundiApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Hello_returns_ok_with_no_database()
    {
        var response = await _client.GetAsync("/api/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HelloDto>();
        Assert.Equal("Hello from PoesieDuLundi", body!.Message);
    }

    [Fact]
    public async Task Status_returns_build_info()
    {
        var response = await _client.GetAsync("/api/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StatusDto>();
        Assert.Equal("Testing", body!.Environment);
        Assert.False(string.IsNullOrWhiteSpace(body.Version));
    }

    [Fact]
    public async Task Healthz_reports_service_unavailable_without_a_reachable_database()
    {
        // The test host runs on EF Core InMemory with no ConnectionStrings:Default configured, so
        // the readiness check's raw Postgres connection has nothing to reach — the same shape a
        // real database outage takes in production.
        var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
