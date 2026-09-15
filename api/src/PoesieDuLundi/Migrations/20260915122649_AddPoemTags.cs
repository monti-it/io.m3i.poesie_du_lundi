using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoesieDuLundi.Migrations
{
    /// <inheritdoc />
    public partial class AddPoemTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Poems",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Poems");
        }
    }
}
