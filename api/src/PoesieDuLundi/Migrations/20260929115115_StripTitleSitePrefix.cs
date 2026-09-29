using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoesieDuLundi.Migrations
{
    /// <summary>
    /// Data-only (issue #62): strips the redundant "La poésie du lundi :" site-name prefix from
    /// existing poem titles, once, through the deploy's <c>migrate</c> init container. The pattern
    /// mirrors <c>EmailTitleResolver</c>'s tolerant prefix match (La/Ma/"l a"/a leading number,
    /// "poesie"/"poésie", di/oundi typos, a <c>:</c>/<c>：</c>/<c>/</c> separator). A title that is
    /// nothing but the prefix is left alone rather than emptied. Only the title changes — slugs stay
    /// put, so published URLs keep working.
    /// </summary>
    public partial class StripTitleSitePrefix : Migration
    {
        private const string SitePrefix =
            @"^\s*(?:\d+\s*°?\s*)?(?:(?:la|l\s*a|laa|ma)\s+)?po[eé]sie\s*,?\s*(?:du|di)\s+(?:lundi|oundi)\s*[:：/]\s*";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE "Poems"
                SET "Title" = btrim(regexp_replace("Title", '{SitePrefix}', '', 'i'))
                WHERE "Title" ~* '{SitePrefix}.*\S';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: the stripped prefix variants aren't recorded anywhere to restore them.
        }
    }
}
