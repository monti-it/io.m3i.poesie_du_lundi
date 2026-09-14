using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoesieDuLundi.Domain;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure.Configurations;

public sealed class PoemConfiguration : IEntityTypeConfiguration<Poem>
{
    public void Configure(EntityTypeBuilder<Poem> builder)
    {
        builder.ToTable("Poems");
        builder.HasKey(poem => poem.Id);
        // The in-memory event buffer, not a column — dispatch is deferred (docs/ENGINEERING_PRACTICES.md
        // "Domain events").
        builder.Ignore(poem => poem.DomainEvents);

        builder.Property(poem => poem.Title).IsRequired();
        builder.Property(poem => poem.Body).IsRequired();
        builder.Property(poem => poem.SeriesId);
        builder.Property(poem => poem.AuthorId);

        builder.Property(poem => poem.Slug)
            .HasConversion(slug => slug.Value, value => new Slug(value))
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(poem => poem.Slug).IsUnique();

        builder.Property(poem => poem.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(poem => poem.PublicationDate);
    }
}
