using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Api.Public;

public sealed record TagCountDto(string Tag, int Count);

public static class PublicTagEndpoints
{
    public static IEndpointRouteBuilder MapPublicTags(this IEndpointRouteBuilder api)
    {
        api.MapGet("/tags", async (ListTagsQuery query, CancellationToken cancellationToken) =>
            {
                var tags = await query.HandleAsync(cancellationToken);
                return Results.Ok(tags.Select(tag => new TagCountDto(tag.Tag, tag.Count)).ToList());
            })
            .WithTags("Public Tags")
            .Produces<IReadOnlyCollection<TagCountDto>>();

        return api;
    }
}
