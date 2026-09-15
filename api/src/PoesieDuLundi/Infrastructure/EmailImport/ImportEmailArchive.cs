using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure.EmailImport;

/// <summary>
/// The one-time <c>import-emails</c> tool (issue #52): turns the <c>datasource/Gmail*</c> archive
/// into published, back-dated <see cref="Poem"/>s. Bypasses <c>SchedulePoemForMonday</c>'s
/// Monday-only / one-per-Monday rules on purpose — those protect *future* scheduling, but the
/// archive genuinely contains non-Monday sends and same-day double-posts, and back-dating a real
/// historical email isn't a scheduling decision to second-guess.
/// </summary>
public sealed class ImportEmailArchive(IPoemRepository repository)
{
    public async Task<EmailImportReport> ExecuteAsync(
        string rootPath, string manifestPath, bool dryRun, CancellationToken cancellationToken = default)
    {
        var manifest = ImportManifest.Load(manifestPath);
        var plan = EmailImportPlan.Create(EmailArchiveScan.Scan(rootPath), manifest);

        if (dryRun)
        {
            return new EmailImportReport(true, plan, []);
        }

        var usedSlugs = new HashSet<string>(manifest.UsedSlugs);
        var imported = new List<ImportedPoem>();

        foreach (var email in plan.Candidates)
        {
            var poem = new Poem(email.Title, email.Body);
            var slug = AssignUniqueSlug(poem.Title, email.PublicationDate, usedSlugs);
            poem.ChangeSlug(slug);

            // Draft -> Scheduled -> Published: Poem has no "create already published" constructor
            // (issue #52's domain constraint) — an import poem drives the same two transitions a
            // live poem does, just both at once instead of days/weeks apart.
            Require(poem.Schedule(email.PublicationDate), email);
            Require(poem.Publish(), email);

            await repository.AddAsync(poem, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            manifest.Add(new ImportManifestEntry(
                email.MessageId, email.SourceFile, poem.Id, poem.Title, slug.Value,
                email.PublicationDate, DateTimeOffset.UtcNow));
            // Rewritten after every poem, not just at the end: a run that dies on email #200 of
            // 261 must not lose the manifest rows for the 199 poems it already committed.
            manifest.Save(manifestPath);

            imported.Add(new ImportedPoem(poem.Id, poem.Title, slug.Value, email.PublicationDate, email.SourceFile));
        }

        return new EmailImportReport(false, plan, imported);
    }

    private static void Require(Result result, ParsedEmail email)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Importing '{email.SourceFile}' failed: {result.Error}");
        }
    }

    /// <summary>Most of the archive shares a handful of recurring titles ("la poésie du lundi"),
    /// which would all collide on the same <see cref="Slug"/> — disambiguate with the publication
    /// date, then (for the rare genuine same-day double-post) a numeric suffix.</summary>
    private static Slug AssignUniqueSlug(string title, DateOnly publicationDate, HashSet<string> usedSlugs)
    {
        var baseSlug = Slug.FromText(title).Value;
        if (usedSlugs.Add(baseSlug))
        {
            return new Slug(baseSlug);
        }

        var withDate = $"{baseSlug}-{publicationDate:yyyy-MM-dd}";
        if (usedSlugs.Add(withDate))
        {
            return new Slug(withDate);
        }

        var suffix = 2;
        string candidate;
        do
        {
            candidate = $"{withDate}-{suffix}";
            suffix++;
        }
        while (!usedSlugs.Add(candidate));

        return new Slug(candidate);
    }
}
