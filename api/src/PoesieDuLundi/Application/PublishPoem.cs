using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Application;

/// <summary>Publishes a scheduled poem immediately, ahead of its <c>PublicationDate</c>.</summary>
public sealed class PublishPoem(IPoemRepository repository)
{
    public async Task<Result> HandleAsync(Guid poemId, CancellationToken cancellationToken = default)
    {
        var poem = await repository.GetAsync(poemId, cancellationToken);
        if (poem is null)
        {
            return Result.NotFound("Poem not found.");
        }

        var result = poem.Publish();
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
