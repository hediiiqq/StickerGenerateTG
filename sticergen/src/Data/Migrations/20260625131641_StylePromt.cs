using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sticergen.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class StylePromt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StylePrompt",
                table: "Drafts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StylePrompt",
                table: "Drafts");
        }
    }
}
