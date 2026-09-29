using PoesieDuLundi.Application;

namespace PoesieDuLundi.Infrastructure.TitleCleanup;

/// <summary>
/// The one-time <c>strip-title-prefix</c> tool (issue #62): removes the redundant "La poésie du
/// lundi :" prefix (see <see cref="PoemTitlePrefixCleanup"/>) from every existing <see
/// cref="Domain.Poem"/> title that has it. Only the title changes — body, series and slug (so
/// published URLs keep working) are left untouched.
/// </summary>
public sealed class StripTitlePrefix(IPoemRepository repository)
{
    public async Task<TitlePrefixCleanupReport> ExecuteAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        var poems = await repository.GetAllAsync(cancellationToken);
        var retitled = new List<RetitledPoem>();

        foreach (var poem in poems)
        {
            var newTitle = PoemTitlePrefixCleanup.StripSitePrefix(poem.Title);
            if (newTitle is null)
            {
                continue;
            }

            retitled.Add(new RetitledPoem(poem.Id, poem.Title, newTitle));

            if (!dryRun)
            {
                var result = poem.UpdateContent(newTitle, poem.Body, poem.SeriesId);
                if (result.IsFailure)
                {
                    throw new InvalidOperationException(
                        $"Retitling poem {poem.Id} from '{poem.Title}' to '{newTitle}' failed: {result.Error}");
                }
            }
        }

        if (!dryRun && retitled.Count > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new TitlePrefixCleanupReport(dryRun, retitled);
    }
}
