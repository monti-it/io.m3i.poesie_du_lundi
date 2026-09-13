using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi;

/// <summary>
/// Applies the pending EF Core migrations. Any failure throws, which is what makes a bad
/// migration block a rollout loudly (the <c>migrate</c> init container exits non-zero — see
/// <c>k8s/api-deployment.yaml</c>).
/// </summary>
public static class DatabaseMigrator
{
    public static void Migrate(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PoesieDuLundiDbContext>().Database.Migrate();
    }
}
