using System.Reflection;
using PoesieDuLundi;
using PoesieDuLundi.Infrastructure;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var database = new DatabaseOptions(
    builder.Configuration.GetConnectionString("Default"),
    builder.Configuration["Database:Provider"],
    builder.Configuration["Database:InMemoryDatabaseName"]);
builder.Services.AddPoesieDuLundiInfrastructure(database);

var app = builder.Build();

// `dotnet PoesieDuLundi.dll migrate` — apply pending migrations, then exit without serving. The
// deployed k8s `api` Deployment runs this as an init container before the app container starts.
if (MigrationCommandLine.IsMigrateOnly(args))
{
    DatabaseMigrator.Migrate(app.Services);
    return;
}

// Never in "Testing" (WebApplicationFactory-based tests swap in EF Core InMemory, which doesn't
// support Migrate()), always in Development (zero-setup local `dotnet run`), and only on explicit
// opt-in (RunMigrationsOnStartup=true) everywhere else — production migrates through the
// `migrate` init container above instead.
if (MigrationStartupPolicy.ShouldRunMigrationsOnStartup(app.Environment, app.Configuration))
{
    DatabaseMigrator.Migrate(app.Services);
}

async Task<bool> IsDatabaseReachableAsync(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return false;
    }

    try
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return true;
    }
    catch (NpgsqlException)
    {
        return false;
    }
}

// Opens a raw Postgres connection — the k8s readiness probe. Not exposed on the public Ingress
// routing (it's covered by the `/api` prefix — fine, it just isn't linked anywhere). A Postgres
// hiccup should stop traffic, not restart the pod, so this must stay the readiness probe, never
// liveness.
app.MapGet("/healthz", async (IConfiguration configuration) =>
{
    var reachable = await IsDatabaseReachableAsync(configuration);
    return reachable
        ? Results.Ok(new HealthDto("ok", "connected"))
        : Results.Problem("Database unreachable", statusCode: StatusCodes.Status503ServiceUnavailable);
});

// No DB dependency — the k8s liveness probe.
app.MapGet("/api/hello", () => Results.Ok(new HelloDto("Hello from PoesieDuLundi")));

app.MapGet("/api/status", () =>
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";
    return Results.Ok(new StatusDto(version, app.Environment.EnvironmentName));
});

app.Run();

// Exposed so future Api integration tests can boot this app via WebApplicationFactory<Program> —
// top-level statements generate an internal Program class otherwise, invisible to another
// assembly.
public partial class Program;
