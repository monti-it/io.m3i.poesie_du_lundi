namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>The result of trying to turn one <c>.eml</c> file into a <see cref="ParsedEmail"/> —
/// either it parsed cleanly, or it's reported as a skip with a human-readable reason (issue #52's
/// "explicitly called out, not silently imported" requirement).</summary>
public sealed class EmailParseOutcome
{
    public ParsedEmail? Email { get; }
    public string? SkipReason { get; }
    public bool IsSuccess => Email is not null;

    private EmailParseOutcome(ParsedEmail? email, string? skipReason)
    {
        Email = email;
        SkipReason = skipReason;
    }

    public static EmailParseOutcome Success(ParsedEmail email) => new(email, null);

    public static EmailParseOutcome Skip(string reason) => new(null, reason);
}
