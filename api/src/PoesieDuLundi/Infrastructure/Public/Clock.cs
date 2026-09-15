namespace PoesieDuLundi.Infrastructure.Public;

internal static class Clock
{
    public static DateOnly Today(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
