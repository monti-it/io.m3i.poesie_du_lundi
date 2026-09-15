using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Application;

/// <summary>Pulls a published poem back to draft.</summary>
public sealed class UnpublishPoem(IPoemRepository repository)
{
    public async Task<Result> HandleAsync(Guid poemId, CancellationToken cancellationToken = default)
    {
        var poem = await repository.GetAsync(poemId, cancellationToken);
        if (poem is null)
        {
            return Result.NotFound("Poem not found.");
        }

        var result = poem.Unpublish();
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
