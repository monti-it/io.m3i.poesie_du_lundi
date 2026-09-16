using PoesieDuLundi.Api.Public;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Api.Admin;

public sealed record CreatePoemRequest(string Title, string Body, Guid? SeriesId);

public sealed record UpdatePoemRequest(
    string Title, string Body, string? Slug, Guid? SeriesId, IReadOnlyCollection<string>? Tags = null);

public sealed record SchedulePoemRequest(DateOnly Date);

public sealed record CreatedPoemDto(Guid Id);

public sealed record PoemDto(
    Guid Id, string Title, string Body, string Slug, string Status, DateOnly? PublicationDate, Guid? SeriesId,
    IReadOnlyCollection<string> Tags);

public sealed record PoemSummaryDto(
    Guid Id, string Title, string Slug, string Status, DateOnly? PublicationDate, Guid? SeriesId,
    IReadOnlyCollection<string> Tags);

public sealed record PreviewLinkDto(string Url, DateTimeOffset ExpiresAt);

/// <summary>One endpoint per poem-management use case, thin handlers that translate a
/// <see cref="Application"/> result into an HTTP response (issue #18) — every route here sits
/// inside the already auth-gated <c>/api/admin</c> group <see cref="AdminEndpoints.MapAdminApi"/>
/// builds.</summary>
public static class AdminPoemEndpoints
{
    public static IEndpointRouteBuilder MapAdminPoems(this IEndpointRouteBuilder admin)
    {
        var poems = admin.MapGroup("/poems").WithTags("Admin Poems");

        poems.MapPost("/", async (
                CreatePoemRequest request, CreateDraftPoem useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.HandleAsync(
                    request.Title, request.Body, request.SeriesId, cancellationToken);
                return result.IsSuccess
                    ? Results.Created($"/api/admin/poems/{result.Value}", new CreatedPoemDto(result.Value))
                    : result.ToProblem();
            })
            .Produces<CreatedPoemDto>(StatusCodes.Status201Created)
            .Produces<ErrorDto>(StatusCodes.Status400BadRequest);

        poems.MapGet("/", async (PoemStatus? status, ListPoemsQuery query, CancellationToken cancellationToken) =>
            {
                var summaries = await query.HandleAsync(status, cancellationToken);
                return Results.Ok(summaries.Select(ToDto));
            })
            .Produces<IReadOnlyCollection<PoemSummaryDto>>();

        poems.MapGet("/{id:guid}", async (Guid id, IPoemRepository repository, CancellationToken cancellationToken) =>
            {
                var poem = await repository.GetAsync(id, cancellationToken);
                return poem is null
                    ? Results.NotFound(new ErrorDto("Poem not found."))
                    : Results.Ok(ToDto(poem));
            })
            .Produces<PoemDto>()
            .Produces<ErrorDto>(StatusCodes.Status404NotFound);

        poems.MapPatch("/{id:guid}", async (
                Guid id, UpdatePoemRequest request, UpdatePoem useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.HandleAsync(
                    id, request.Title, request.Body, request.Slug, request.SeriesId, request.Tags, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorDto>(StatusCodes.Status400BadRequest)
            .Produces<ErrorDto>(StatusCodes.Status404NotFound);

        poems.MapDelete("/{id:guid}", async (Guid id, DeletePoem useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.HandleAsync(id, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorDto>(StatusCodes.Status404NotFound);

        poems.MapPost("/{id:guid}/schedule", async (
                Guid id, SchedulePoemRequest request, SchedulePoemForMonday useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.HandleAsync(id, request.Date, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorDto>(StatusCodes.Status400BadRequest)
            .Produces<ErrorDto>(StatusCodes.Status404NotFound)
            .Produces<ErrorDto>(StatusCodes.Status409Conflict);

        poems.MapPost("/{id:guid}/publish", async (Guid id, PublishPoem useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.HandleAsync(id, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorDto>(StatusCodes.Status404NotFound)
            .Produces<ErrorDto>(StatusCodes.Status409Conflict);

        poems.MapPost("/{id:guid}/unpublish", async (
                Guid id, UnpublishPoem useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.HandleAsync(id, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorDto>(StatusCodes.Status404NotFound)
            .Produces<ErrorDto>(StatusCodes.Status409Conflict);

        poems.MapPost("/{id:guid}/preview-link", async (
                Guid id, HttpRequest request, GeneratePreviewLink useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.HandleAsync(id, cancellationToken);
                if (result.IsFailure)
                {
                    return result.ToProblem();
                }

                var url = $"{AbsoluteUrl.BaseUrl(request)}/poems/{Uri.EscapeDataString(result.Value.Slug)}" +
                    $"?preview={Uri.EscapeDataString(result.Value.Token)}";
                return Results.Ok(new PreviewLinkDto(url, result.Value.ExpiresAt));
            })
            .Produces<PreviewLinkDto>()
            .Produces<ErrorDto>(StatusCodes.Status404NotFound)
            .Produces<ErrorDto>(StatusCodes.Status409Conflict);

        return admin;
    }

    private static PoemDto ToDto(Poem poem) =>
        new(
            poem.Id, poem.Title, poem.Body, poem.Slug.Value, poem.Status.ToString(), poem.PublicationDate,
            poem.SeriesId, poem.Tags.Select(tag => tag.Value).ToList());

    private static PoemSummaryDto ToDto(PoemSummary summary) =>
        new(
            summary.Id, summary.Title, summary.Slug, summary.Status.ToString(), summary.PublicationDate,
            summary.SeriesId, summary.Tags);
}
