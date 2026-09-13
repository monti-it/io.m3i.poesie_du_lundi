using Microsoft.EntityFrameworkCore;

namespace PoesieDuLundi.Infrastructure;

/// <summary>
/// The one <c>DbContext</c> for this bounded context. Empty until the first aggregate (`Poem`,
/// issue #14) lands — the initial migration exists now only to prove the migrate command and the
/// real-Postgres round-trip work end to end (issue #7).
/// </summary>
public sealed class PoesieDuLundiDbContext(DbContextOptions<PoesieDuLundiDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PoesieDuLundiDbContext).Assembly);
    }
}
