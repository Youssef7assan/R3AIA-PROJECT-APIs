using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class SplitNIDImageToFrontBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NIDImage",
                table: "Patients",
                newName: "NIDFrontImage");

            migrationBuilder.AddColumn<string>(
                name: "NIDBackImage",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NIDBackImage",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "NIDFrontImage",
                table: "Patients",
                newName: "NIDImage");
        }
    }
}
