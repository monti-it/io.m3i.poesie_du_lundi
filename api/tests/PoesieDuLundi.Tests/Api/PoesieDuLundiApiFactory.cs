using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Tests.Api;

/// <summary>
/// Boots the real app with <see cref="PoesieDuLundiDbContext"/> on EF Core InMemory — a single
/// config flag (<c>Database:Provider=InMemory</c>) that <c>AddPoesieDuLundiInfrastructure</c>
/// reads, no service-descriptor surgery (mirrors io.m3i.ledgy's <c>LedgyApiFactory</c>, issue #249
/// there). Everything else — endpoint mapping, DI — runs exactly as in production.
/// </summary>
public sealed class PoesieDuLundiApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // UseSetting flows into WebApplicationBuilder.Configuration, so Program.cs reads InMemory
        // when it registers the DbContext. ConfigureAppConfiguration would land too late — the
        // provider is chosen at service-registration time, before builder.Build().
        builder.UseSetting("Database:Provider", "InMemory");
        // Per-factory so parallel test classes never collide in the shared in-memory store.
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
