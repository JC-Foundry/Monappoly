using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UltimateMonopoly.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomBoards_AspNetUsers_UserId",
                table: "CustomBoards");

            migrationBuilder.DropIndex(
                name: "IX_CustomBoards_UserId",
                table: "CustomBoards");

            migrationBuilder.AddColumn<string>(
                name: "AvatarColour",
                table: "AspNetUsers",
                type: "varchar(7)",
                maxLength: 7,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AvatarImagePath",
                table: "AspNetUsers",
                type: "varchar(10240)",
                maxLength: 10240,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<uint>(
                name: "NumberOfDraws",
                table: "AspNetUsers",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "NumberOfLosses",
                table: "AspNetUsers",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "NumberOfWins",
                table: "AspNetUsers",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarColour",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AvatarImagePath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NumberOfDraws",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NumberOfLosses",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NumberOfWins",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_CustomBoards_UserId",
                table: "CustomBoards",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomBoards_AspNetUsers_UserId",
                table: "CustomBoards",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
