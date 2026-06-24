using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sticergen.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class FilePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilnalFilePath",
                table: "DraftStickers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFilePath",
                table: "DraftStickers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilnalFilePath",
                table: "DraftStickers");

            migrationBuilder.DropColumn(
                name: "OriginalFilePath",
                table: "DraftStickers");
        }
    }
}
