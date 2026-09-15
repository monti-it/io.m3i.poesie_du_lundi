using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Application;

/// <summary>Updates a poem's title/body/series and, optionally, its slug and tags.</summary>
public sealed class UpdatePoem(IPoemRepository repository)
{
    public async Task<Result> HandleAsync(
        Guid poemId,
        string title,
        string body,
        string? slug,
        Guid? seriesId,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var poem = await repository.GetAsync(poemId, cancellationToken);
        if (poem is null)
        {
            return Result.NotFound("Poem not found.");
        }

        var result = poem.UpdateContent(title, body, seriesId);
        if (result.IsFailure)
        {
            return result;
        }

        if (slug is not null)
        {
            try
            {
                poem.ChangeSlug(new Slug(slug));
            }
            catch (ArgumentException exception)
            {
                return Result.Failure(exception.Message);
            }
        }

        if (tags is not null)
        {
            try
            {
                poem.ChangeTags(tags.Select(tag => new Slug(tag)).ToList());
            }
            catch (ArgumentException exception)
            {
                return Result.Failure(exception.Message);
            }
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
