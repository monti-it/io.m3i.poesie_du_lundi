using PoesieDuLundi.Domain;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Application;

/// <summary>Creates a new draft poem. Title/body validation lives on the <see cref="Poem"/>
/// constructor (a value the aggregate can never hold invalid) — this use case only translates
/// that into the expected-failure <see cref="Result"/> contract for the admin API.</summary>
public sealed class CreateDraftPoem(IPoemRepository repository)
{
    public async Task<Result<Guid>> HandleAsync(
        string title, string body, Guid? seriesId, CancellationToken cancellationToken = default)
    {
        Poem poem;
        try
        {
            poem = new Poem(title, body, seriesId);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<Guid>(exception.Message);
        }

        await repository.AddAsync(poem, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success(poem.Id);
    }
}
