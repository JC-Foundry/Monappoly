using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UltimateMonopoly.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedToBoardShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "SharedBoardSkins",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "SharedBoardSkins",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DeletedById",
                table: "SharedBoardSkins",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedUtc",
                table: "SharedBoardSkins",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SharedBoardSkins",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedById",
                table: "SharedBoardSkins",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedUtc",
                table: "SharedBoardSkins",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestoredById",
                table: "SharedBoardSkins",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "RestoredUtc",
                table: "SharedBoardSkins",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SharedBoardSkins");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "SharedBoardSkins");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "SharedBoardSkins");

            migrationBuilder.DropColumn(
                name: "DeletedUtc",
                table: "SharedBoardSkins");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SharedBoardSkins");

            migrationBuilder.DropColumn(
                name: "LastModifiedById",
                table: "SharedBoardSkins");

            migrationBuilder.DropColumn(
                name: "LastModifiedUtc",
                table: "SharedBoardSkins");

            migrationBuilder.DropColumn(
                name: "RestoredById",
                table: "SharedBoardSkins");

            migrationBuilder.DropColumn(
                name: "RestoredUtc",
                table: "SharedBoardSkins");
        }
    }
}
