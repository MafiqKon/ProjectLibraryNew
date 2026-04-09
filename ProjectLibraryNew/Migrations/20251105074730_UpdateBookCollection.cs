using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLibraryNew.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "BookCollections",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "BookCollections",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BookCollections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "BookCollections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_BookCollections_UserId",
                table: "BookCollections",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookCollections_AspNetUsers_UserId",
                table: "BookCollections",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookCollections_AspNetUsers_UserId",
                table: "BookCollections");

            migrationBuilder.DropIndex(
                name: "IX_BookCollections_UserId",
                table: "BookCollections");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "BookCollections");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BookCollections");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "BookCollections");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "BookCollections",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
