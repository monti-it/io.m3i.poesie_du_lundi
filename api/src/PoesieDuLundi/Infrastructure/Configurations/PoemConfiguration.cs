using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        // A small set of slugged tags as a jsonb array — not a native text[] column (issue #20's
        // "ledgy gotcha": a native array column passes InMemory and throws against Npgsql; see
        // docs/ENGINEERING_PRACTICES.md "Database strategy" and the Persistence.SmokeTests
        // round-trip). An explicit ValueComparer is required: without one, EF Core can't tell a
        // replaced tag list apart from an unmodified one.
        builder.Property(poem => poem.Tags)
            .HasConversion(
                tags => JsonSerializer.Serialize(tags.Select(tag => tag.Value), (JsonSerializerOptions?)null),
                json => (JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Select(value => new Slug(value))
                    .ToList(),
                new ValueComparer<IReadOnlyList<Slug>>(
                    (left, right) => (left ?? Array.Empty<Slug>()).SequenceEqual(right ?? Array.Empty<Slug>()),
                    tags => tags.Aggregate(0, (hash, tag) => HashCode.Combine(hash, tag.Value)),
                    tags => tags.ToList()))
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
