using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Application;

public sealed class DeletePoem(IPoemRepository repository)
{
    public async Task<Result> HandleAsync(Guid poemId, CancellationToken cancellationToken = default)
    {
        var poem = await repository.GetAsync(poemId, cancellationToken);
        if (poem is null)
        {
            return Result.NotFound("Poem not found.");
        }

        repository.Remove(poem);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
