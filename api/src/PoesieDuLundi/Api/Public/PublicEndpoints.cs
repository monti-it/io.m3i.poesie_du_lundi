namespace PoesieDuLundi.Api.Public;

/// <summary>
/// Maps every anonymous, published-content route under <c>/api</c> — no group-wide filter, unlike
/// <see cref="Admin.AdminEndpoints.MapAdminApi"/>'s auth gate: these are the routes the world reads
/// with no <c>Remote-User</c> at all (docs/ARCHITECTURE.md "Request flow").
/// </summary>
public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapPublicPoems();
        api.MapPublicArchive();
        api.MapPublicSeries();

        return app;
    }
}
