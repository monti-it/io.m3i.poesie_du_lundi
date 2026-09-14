using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Application;

/// <summary>
/// Schedules a draft poem for a Monday. The Monday-only + one-poem-per-Monday rules live here,
/// not on <c>Poem</c> — see the note on <c>Poem</c> (issue #17).
/// </summary>
public sealed class SchedulePoemForMonday(IPoemRepository repository)
{
    public async Task<Result> HandleAsync(
        Guid poemId, DateOnly publicationDate, CancellationToken cancellationToken = default)
    {
        if (publicationDate.DayOfWeek != DayOfWeek.Monday)
        {
            return Result.Failure("Poems can only be scheduled for a Monday.");
        }

        if (await repository.HasScheduledOrPublishedForDateAsync(publicationDate, cancellationToken))
        {
            return Result.Failure($"{publicationDate} already has a scheduled or published poem.");
        }

        var poem = await repository.GetAsync(poemId, cancellationToken);
        if (poem is null)
        {
            return Result.Failure("Poem not found.");
        }

        var result = poem.Schedule(publicationDate);
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
