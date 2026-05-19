using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UltimateMonopoly.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAppUserPropnames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarImagePath",
                table: "AspNetUsers",
                newName: "AvatarImageName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarImageName",
                table: "AspNetUsers",
                newName: "AvatarImagePath");
        }
    }
}
