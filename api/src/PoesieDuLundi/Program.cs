using System.Reflection;
using PoesieDuLundi;
using PoesieDuLundi.Api.Admin;
using PoesieDuLundi.Api.Public;
using PoesieDuLundi.Infrastructure;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var database = new DatabaseOptions(
    builder.Configuration.GetConnectionString("Default"),
    builder.Configuration["Database:Provider"],
    builder.Configuration["Database:InMemoryDatabaseName"]);
var publicationJob = new PublicationJobOptions(PublicationJobStartupPolicy.GetInterval(builder.Configuration));
var previewLink = PreviewLinkStartupPolicy.GetOptions(builder.Environment, builder.Configuration);
builder.Services.AddPoesieDuLundiInfrastructure(database, publicationJob, previewLink);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ForwardAuthIdentityProvider>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PoesieDuLundi API", Version = "v1" }));

// One tag ("feeds") shared by every feed format — a PoemPublished/PoemUnpublished handler
// (FeedCacheInvalidationHandler) evicts the whole tag rather than tracking per-format keys.
builder.Services.AddOutputCache(options =>
    options.AddPolicy("Feeds", policy => policy.Tag("feeds").Expire(TimeSpan.FromHours(1))));

var app = builder.Build();

app.UseOutputCache();

// `dotnet PoesieDuLundi.dll migrate` — apply pending migrations, then exit without serving. The
// deployed k8s `api` Deployment runs this as an init container before the app container starts.
if (MigrationCommandLine.IsMigrateOnly(args))
{
    DatabaseMigrator.Migrate(app.Services);
    return;
}

// `dotnet PoesieDuLundi.dll import-emails <path> [--dry-run]` — the one-time Gmail-archive
// import (issue #52), then exit without serving.
if (ImportEmailsCommandLine.IsImportEmails(args))
{
    await EmailImportRunner.RunAsync(app.Services, ImportEmailsCommandLine.Parse(args));
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
})
.WithTags("Diagnostics")
.Produces<HealthDto>()
.ProducesProblem(StatusCodes.Status503ServiceUnavailable);

// No DB dependency — the k8s liveness probe.
app.MapGet("/api/hello", () => Results.Ok(new HelloDto("Hello from PoesieDuLundi")))
    .WithTags("Diagnostics")
    .Produces<HelloDto>();

app.MapGet("/api/status", () =>
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";
    return Results.Ok(new StatusDto(version, app.Environment.EnvironmentName));
})
.WithTags("Diagnostics")
.Produces<StatusDto>();

if (ApiDocumentationPolicy.ShouldExposeDocs(app.Environment, app.Configuration))
{
    app.MapAdminApiDocs();
}

// Root-level, not under /api — see PublicSeoEndpoints for why.
app.MapPublicSeo();
app.MapPublicApi();
app.MapAdminApi();

app.Run();

// Exposed so future Api integration tests can boot this app via WebApplicationFactory<Program> —
// top-level statements generate an internal Program class otherwise, invisible to another
// assembly.
public partial class Program;
