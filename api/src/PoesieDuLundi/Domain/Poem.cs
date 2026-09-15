using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Domain;

/// <summary>
/// A poem in the Monday publishing cycle. The "must be a Monday" scheduling rule belongs to the
/// publishing workflow (issue #17), not here — Poem only enforces its own
/// Draft → Scheduled → Published lifecycle.
/// </summary>
public sealed class Poem : AggregateRoot
{
    public string Title { get; private set; }
    public string Body { get; private set; }
    public Slug Slug { get; private set; }
    public Guid? SeriesId { get; private set; }
    public Guid? AuthorId { get; }
    public PoemStatus Status { get; private set; }
    public DateOnly? PublicationDate { get; private set; }
    public IReadOnlyList<Slug> Tags { get; private set; } = [];

    public Poem(string title, string body, Guid? seriesId = null, Guid? authorId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Body is required.", nameof(body));
        }

        Title = title.Trim();
        Body = body;
        Slug = Slug.FromText(Title);
        SeriesId = seriesId;
        AuthorId = authorId;
        Status = PoemStatus.Draft;
    }

    public void ChangeSlug(Slug slug) => Slug = slug;

    /// <summary>Replaces the whole tag set — each tag is itself a <see cref="Slug"/>, so it's
    /// already validated by the time it reaches here; only deduplication/ordering happens.</summary>
    public void ChangeTags(IReadOnlyCollection<Slug> tags) =>
        Tags = tags.Distinct().OrderBy(tag => tag.Value, StringComparer.Ordinal).ToList();

    public Result UpdateContent(string title, string body, Guid? seriesId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Result.Failure("Body is required.");
        }

        Title = title.Trim();
        Body = body;
        SeriesId = seriesId;
        return Result.Success();
    }

    /// <summary>Resolved-on-read publication check: a scheduled poem counts as published once its
    /// date has arrived, independently of whether the materialising job has run yet
    /// (docs/ARCHITECTURE.md "Publishing model").</summary>
    public bool IsEffectivelyPublished(DateOnly asOf) =>
        Status == PoemStatus.Published || (Status == PoemStatus.Scheduled && PublicationDate <= asOf);

    public Result Schedule(DateOnly publicationDate)
    {
        if (Status != PoemStatus.Draft)
        {
            return Result.Conflict("Only a draft poem can be scheduled.");
        }

        Status = PoemStatus.Scheduled;
        PublicationDate = publicationDate;
        return Result.Success();
    }

    public Result Publish()
    {
        if (Status != PoemStatus.Scheduled)
        {
            return Result.Conflict("Only a scheduled poem can be published.");
        }

        Status = PoemStatus.Published;
        AddDomainEvent(new PoemPublished(Id, PublicationDate!.Value));
        return Result.Success();
    }

    public Result Unpublish()
    {
        if (Status != PoemStatus.Published)
        {
            return Result.Conflict("Only a published poem can be unpublished.");
        }

        Status = PoemStatus.Draft;
        PublicationDate = null;
        AddDomainEvent(new PoemUnpublished(Id));
        return Result.Success();
    }
}
