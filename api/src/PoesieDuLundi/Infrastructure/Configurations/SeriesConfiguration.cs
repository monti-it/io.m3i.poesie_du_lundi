using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoesieDuLundi.Domain;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure.Configurations;

public sealed class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("Series");
        builder.HasKey(series => series.Id);
        // The in-memory event buffer, not a column — dispatch is deferred (docs/ENGINEERING_PRACTICES.md
        // "Domain events").
        builder.Ignore(series => series.DomainEvents);

        builder.Property(series => series.Title).IsRequired();
        builder.Property(series => series.Description);
        builder.Property(series => series.Order);

        builder.Property(series => series.Slug)
            .HasConversion(slug => slug.Value, value => new Slug(value))
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(series => series.Slug).IsUnique();
    }
}
