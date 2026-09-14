using Microsoft.Extensions.DependencyInjection;

namespace PoesieDuLundi.Api.Admin;

public sealed record MeDto(string Subject);

public static class AdminEndpoints
{
    /// <summary>
    /// Maps the <c>/api/admin</c> route group. Every endpoint in the group sits behind the same
    /// gate: no resolved <see cref="ForwardAuthIdentityProvider"/> subject (missing
    /// <c>Remote-User</c>, outside <c>Development</c>) → 401, before any handler runs.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin");

        admin.AddEndpointFilter(async (context, next) =>
        {
            var identity = context.HttpContext.RequestServices
                .GetRequiredService<ForwardAuthIdentityProvider>();

            return identity.GetCurrentSubject() is null
                ? Results.Unauthorized()
                : await next(context);
        });

        // Echoes the resolved identity — verifies the forwardAuth wiring end to end without
        // touching any real admin feature yet.
        admin.MapGet("/me", (ForwardAuthIdentityProvider identity) =>
            Results.Ok(new MeDto(identity.GetCurrentSubject()!)));

        return app;
    }
}
