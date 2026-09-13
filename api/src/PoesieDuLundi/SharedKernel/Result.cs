namespace PoesieDuLundi.SharedKernel;

/// <summary>
/// The outcome of Domain/Application code for an <em>expected</em> failure — a business rule
/// violated ("can't publish a poem with an empty body"), not a bug
/// (docs/ENGINEERING_PRACTICES.md "Result pattern for expected failures"). One bounded context,
/// one small admin surface: <see cref="Error"/> is a plain message an <c>Api</c> handler maps to
/// the right HTTP status, not yet the namespaced-code/HTTP-type record ledgy grew once it had
/// several modules and an RFC 7807 contract to keep consistent across them — add that machinery
/// here the day this app actually needs it.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
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
    }

    public static Result Success() => new(true, string.Empty);

    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);

    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    internal Result(T? value, bool isSuccess, string error)
        : base(isSuccess, error)
    {
        _value = value;
    }
}
