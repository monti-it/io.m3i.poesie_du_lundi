using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Infrastructure;

/// <summary>The one <c>DbContext</c> for this bounded context.</summary>
public sealed class PoesieDuLundiDbContext(DbContextOptions<PoesieDuLundiDbContext> options)
    : DbContext(options)
{
    public DbSet<Poem> Poems => Set<Poem>();
    public DbSet<Series> Series => Set<Series>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PoesieDuLundiDbContext).Assembly);
    }
}
