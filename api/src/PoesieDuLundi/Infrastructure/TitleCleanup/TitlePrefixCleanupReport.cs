namespace PoesieDuLundi.Infrastructure.TitleCleanup;

/// <summary>One title a <c>strip-title-prefix</c> run changed (or would change, in a dry run).</summary>
public sealed record RetitledPoem(Guid PoemId, string OldTitle, string NewTitle);

/// <summary>What a <c>strip-title-prefix</c> run did (or, for a dry run, would do) — printed to the
/// console by <c>StripTitlePrefixRunner</c> (issue #62).</summary>
public sealed record TitlePrefixCleanupReport(bool DryRun, IReadOnlyList<RetitledPoem> Retitled);
