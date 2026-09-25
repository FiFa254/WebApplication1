using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevFolio.Migrations
{
    /// <inheritdoc />
    public partial class MoveProjectImageToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "ProfileProjects");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImagePath",
                table: "Profiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImagePath",
                table: "Profiles");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "ProfileProjects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
