using PoesieDuLundi;
using PoesieDuLundi.Infrastructure;

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

// Placeholder — the real diagnostics endpoints (/healthz, /api/hello, /api/status) land in #11.
app.MapGet("/api/hello", () => Results.Ok(new { message = "Hello from PoesieDuLundi" }));

app.Run();

// Exposed so future Api integration tests can boot this app via WebApplicationFactory<Program> —
// top-level statements generate an internal Program class otherwise, invisible to another
// assembly.
public partial class Program;
