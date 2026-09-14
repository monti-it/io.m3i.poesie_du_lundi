using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Domain;

/// <summary>
/// A named cycle of poems (a season / theme). <see cref="Poem.SeriesId"/> is the only link between
/// the two aggregates — a series carries no navigation back to its poems, so it cannot itself know
/// whether any exist. <see cref="EnsureCanBeDeleted"/> takes that as a fact the caller already
/// knows (the future delete use case queries <c>Poem</c> before calling it) and enforces the
/// chosen policy: reject rather than silently detaching poems from their series.
/// </summary>
public sealed class Series : AggregateRoot
{
    public string Title { get; }
    public Slug Slug { get; }
    public string? Description { get; }
    public int Order { get; }

    public Series(string title, int order, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Title = title.Trim();
        Slug = Slug.FromText(Title);
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Order = order;
    }

    public Result EnsureCanBeDeleted(bool hasPoems)
    {
        if (hasPoems)
        {
            return Result.Failure("A series with poems attached cannot be deleted.");
        }

        return Result.Success();
    }
}
