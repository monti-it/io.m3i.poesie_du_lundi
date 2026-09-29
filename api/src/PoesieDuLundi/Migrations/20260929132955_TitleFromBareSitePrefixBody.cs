using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoesieDuLundi.Migrations
{
    /// <summary>
    /// Data-only (issue #108): a poem titled nothing but the site name ("La poésie du lundi", in
    /// the same tolerant variants as <c>EmailTitleResolver.SitePrefixCore</c>, plus a trailing
    /// <c>.</c>) takes its real title from the body's first line — but only when that line stands
    /// alone: non-empty, followed by at least one blank line, and under 80 characters once any
    /// surrounding <c>*asterisks*</c> are stripped. That line and the blank lines after it are
    /// removed from the body so the title isn't shown twice. Every other poem is left alone for
    /// manual review, including titles with extra text after the site name. Slugs stay put, so
    /// published URLs keep working (as in <see cref="StripTitleSitePrefix"/>).
    /// </summary>
    /// <remarks>
    /// This bypasses the domain events that <c>FeedCacheInvalidationHandler</c> listens to; the
    /// feeds' output cache is in-memory with a one-hour expiry and the deploy restarts the pod,
    /// so no explicit invalidation is needed.
    /// </remarks>
    public partial class TitleFromBareSitePrefixBody : Migration
    {
        private const string BareSiteTitle =
            @"^\s*(?:\d+\s*°?\s*)?(?:(?:la|l\s*a|laa|ma)\s+)?po[eé]sie\s*,?\s*(?:du|di)\s+(?:lundi|oundi)\s*\.?\s*$";

        // The first non-blank line (captured), then one or more blank lines.
        private const string TitleLine = @"^\s*([^\r\n]+)\r?\n(?:[ \t]*\r?\n)+";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE "Poems" AS p
                SET "Title" = c.title, "Body" = c.body
                FROM (
                    SELECT "Id",
                           CASE WHEN line ~ '^\*.*\*$' THEN btrim(line, '* ') ELSE line END AS title,
                           regexp_replace("Body", '{TitleLine}', '') AS body
                    FROM (
                        SELECT "Id", "Body", btrim(substring("Body" from '{TitleLine}'), E' \t') AS line
                        FROM "Poems"
                        WHERE "Title" ~* '{BareSiteTitle}'
                    ) AS bare
                    WHERE line IS NOT NULL
                ) AS c
                WHERE p."Id" = c."Id"
                  AND c.title ~ '\S'
                  AND char_length(c.title) < 80
                  AND c.body ~ '\S';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: which site-name variant each poem was titled with isn't recorded, and
            // the removed first line can't be told apart from the body's own opening afterwards.
        }
    }
}
