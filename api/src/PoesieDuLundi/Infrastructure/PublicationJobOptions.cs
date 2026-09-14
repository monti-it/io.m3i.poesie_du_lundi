namespace PoesieDuLundi.Infrastructure;

/// <summary>How often <see cref="PoemPublicationBackgroundService"/> sweeps for due poems.</summary>
public sealed record PublicationJobOptions(TimeSpan Interval)
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);
}
