using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sticergen.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class FilePaths1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FilnalFilePath",
                table: "DraftStickers",
                newName: "FinalFilePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FinalFilePath",
                table: "DraftStickers",
                newName: "FilnalFilePath");
        }
    }
}
