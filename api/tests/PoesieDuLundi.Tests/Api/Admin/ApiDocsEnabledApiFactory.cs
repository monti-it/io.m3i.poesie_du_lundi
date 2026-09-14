using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Tests.Api.Admin;

/// <summary>
/// Same shape as <see cref="PoesieDuLundi.Tests.Api.PoesieDuLundiApiFactory"/>, but outside
/// "Testing" — the one environment <see cref="ApiDocumentationPolicy"/> always turns the docs off
/// in — so this is the factory that actually exercises the gated <c>/api/admin/swagger</c>
/// endpoints. Not "Development": <c>MigrationStartupPolicy</c> would try to run EF Core
/// migrations there, which the InMemory provider below doesn't support.
/// </summary>
public sealed class ApiDocsEnabledApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Staging");
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:InMemoryDatabaseName", _databaseName);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>().Database.EnsureCreated();

        return host;
    }
}
