namespace PoesieDuLundi.SharedKernel;

/// <summary>
/// The HTTP-status family a failed <see cref="Result"/> maps to. Three buckets, not ledgy's
/// namespaced-code/RFC-7807 record — this app grows that machinery the day it actually needs it.
/// </summary>
public enum ErrorType
{
    /// <summary>400 — the request itself is invalid (bad input, a rule the request broke).</summary>
    Failure,

    /// <summary>404 — the referenced resource doesn't exist.</summary>
    NotFound,

    /// <summary>409 — the request is valid but conflicts with the resource's current state.</summary>
    Conflict,
}

/// <summary>
/// The outcome of Domain/Application code for an <em>expected</em> failure — a business rule
/// violated ("can't publish a poem with an empty body"), not a bug
/// (docs/ENGINEERING_PRACTICES.md "Result pattern for expected failures"). One bounded context,
/// one small admin surface: <see cref="Error"/> plus <see cref="Type"/> is a plain message and a
/// three-way HTTP-status bucket an <c>Api</c> handler maps to a response, not yet the
/// namespaced-code/HTTP-type record ledgy grew once it had several modules and an RFC 7807
/// contract to keep consistent across them — add that machinery here the day this app actually
/// needs it.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public ErrorType Type { get; }

    protected Result(bool isSuccess, string error, ErrorType type)
    {
        if (isSuccess && error.Length > 0)
        {
            throw new InvalidOperationException("A successful result cannot have an error.");
        }

        if (!isSuccess && error.Length == 0)
        {
            throw new InvalidOperationException("A failed result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
        Type = type;
    }

    public static Result Success() => new(true, string.Empty, ErrorType.Failure);

    public static Result Failure(string error) => new(false, error, ErrorType.Failure);

    public static Result NotFound(string error) => new(false, error, ErrorType.NotFound);

    public static Result Conflict(string error) => new(false, error, ErrorType.Conflict);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty, ErrorType.Failure);

    public static Result<T> Failure<T>(string error) => new(default, false, error, ErrorType.Failure);

    public static Result<T> NotFound<T>(string error) => new(default, false, error, ErrorType.NotFound);

    public static Result<T> Conflict<T>(string error) => new(default, false, error, ErrorType.Conflict);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    internal Result(T? value, bool isSuccess, string error, ErrorType type)
        : base(isSuccess, error, type)
    {
        _value = value;
    }
}
