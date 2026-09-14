using Microsoft.Extensions.DependencyInjection;

namespace PoesieDuLundi.Api.Admin;

/// <summary>
/// Mounts the generated OpenAPI document and Swagger UI at <c>/api/admin/swagger</c> — inside the
/// path prefix Traefik's forwardAuth gate covers (docs/ARCHITECTURE.md), so they are never
/// reachable anonymously on the deployed site. <see cref="ApiDocumentationPolicy"/> decides
/// whether this is called at all.
/// </summary>
public static class AdminApiDocs
{
    public static WebApplication MapAdminApiDocs(this WebApplication app)
    {
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/api/admin/swagger"),
            branch =>
            {
                // Swagger UI is classic middleware, not a routed endpoint, so it can't share
                // AdminEndpoints' route-group filter — the same identity check is applied by hand.
                branch.Use(async (context, next) =>
                {
                    var identity = context.RequestServices
                        .GetRequiredService<ForwardAuthIdentityProvider>();

                    if (identity.GetCurrentSubject() is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    await next(context);
                });

                branch.UseSwagger(options =>
                    options.RouteTemplate = "api/admin/swagger/{documentName}/swagger.json");
                branch.UseSwaggerUI(options =>
                {
                    options.RoutePrefix = "api/admin/swagger";
                    options.SwaggerEndpoint("/api/admin/swagger/v1/swagger.json", "PoesieDuLundi API v1");
                });
            });

        return app;
    }
}
