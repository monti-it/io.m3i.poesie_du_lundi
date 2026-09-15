using PoesieDuLundi.Infrastructure.Public;

namespace PoesieDuLundi.Api.Public;

public sealed record ArchiveGroupDto(int Year, int Month, int Count);

public sealed record ArchiveDto(
    IReadOnlyCollection<ArchiveGroupDto> Groups, IReadOnlyCollection<PublicPoemSummaryDto> Entries);

public static class PublicArchiveEndpoints
{
    public static IEndpointRouteBuilder MapPublicArchive(this IEndpointRouteBuilder api)
    {
        api.MapGet("/archive", async (
                int? year, int? month, GetArchiveQuery query, CancellationToken cancellationToken) =>
            {
                if (month is < 1 or > 12)
                {
                    return Results.BadRequest(new ErrorDto("Month must be between 1 and 12."));
                }

                var archive = await query.HandleAsync(year, month, cancellationToken);
                return Results.Ok(ToDto(archive));
            })
            .WithTags("Public Archive")
            .Produces<ArchiveDto>()
            .Produces<ErrorDto>(StatusCodes.Status400BadRequest);

        return api;
    }

    private static ArchiveDto ToDto(Archive archive) => new(
        archive.Groups.Select(group => new ArchiveGroupDto(group.Year, group.Month, group.Count)).ToList(),
        archive.Entries.Select(PublicPoemEndpoints.ToDto).ToList());
}
