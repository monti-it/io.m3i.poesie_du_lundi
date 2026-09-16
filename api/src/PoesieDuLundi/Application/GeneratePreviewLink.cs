using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Application;

public sealed record PreviewLinkDetails(string Slug, string Token, DateTimeOffset ExpiresAt);

/// <summary>Issues a signed, expiring token a reviewer can use to view a not-yet-published poem
/// without publishing it (issue #25). Only a poem that already carries a
/// <see cref="Domain.Poem.PublicationDate"/> (Scheduled or Published) can be previewed — a bare
/// Draft has no timeline position to preview against, so it must be scheduled first.</summary>
public sealed class GeneratePreviewLink(IPoemRepository repository, IPreviewTokenService tokenService)
{
    public async Task<Result<PreviewLinkDetails>> HandleAsync(
        Guid poemId, CancellationToken cancellationToken = default)
    {
        var poem = await repository.GetAsync(poemId, cancellationToken);
        if (poem is null)
        {
            return Result.NotFound<PreviewLinkDetails>("Poem not found.");
        }

        if (poem.PublicationDate is null)
        {
            return Result.Conflict<PreviewLinkDetails>(
                "Poem must be scheduled before a preview link can be issued.");
        }

        var issued = tokenService.Issue(poem.Id);
        return Result.Success(new PreviewLinkDetails(poem.Slug.Value, issued.Token, issued.ExpiresAt));
    }
}
