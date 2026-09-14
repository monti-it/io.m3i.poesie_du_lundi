namespace PoesieDuLundi.Application;

/// <summary>
/// The materialising job's actual work (docs/ARCHITECTURE.md "Publishing model"): transitions
/// every Scheduled poem whose date has arrived to Published, firing <c>PoemPublished</c> for each
/// so side effects (feed cache-bust, later the newsletter) have a hook. Idempotent — a poem
/// already Published is never returned by <see cref="IPoemRepository.GetDueForPublicationAsync"/>
/// again, so re-running finds nothing left to do. The recurring trigger (a <c>BackgroundService</c>
/// or cron) is an Infrastructure/Host concern that calls this.
/// </summary>
public sealed class MaterialiseDuePoems(IPoemRepository repository, TimeProvider timeProvider)
{
    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var due = await repository.GetDueForPublicationAsync(today, cancellationToken);

        var published = 0;
        foreach (var poem in due)
        {
            if (poem.Publish().IsSuccess)
            {
                published++;
            }
        }

        if (published > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return published;
    }
}
