using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Api;

/// <summary>The bare <c>{ "error": "..." }</c> body for a failed <see cref="Result"/> — this app's
/// one small admin surface doesn't need ledgy's RFC 7807 <c>problem+json</c> contract yet.</summary>
public sealed record ErrorDto(string Error);

public static class ResultExtensions
{
    /// <summary>Maps a failed <see cref="Result"/>'s <see cref="ErrorType"/> to 400/404/409. Callers
    /// must check <see cref="Result.IsFailure"/> first — this throws on a successful result, since a
    /// handler always has a more specific success response to build (200/201/204).</summary>
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result has no problem to map.");
        }

        return result.Type switch
        {
            ErrorType.NotFound => Results.NotFound(new ErrorDto(result.Error)),
            ErrorType.Conflict => Results.Conflict(new ErrorDto(result.Error)),
            _ => Results.BadRequest(new ErrorDto(result.Error)),
        };
    }
}
