using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLibraryNew.Migrations
{
    /// <inheritdoc />
    public partial class AddBookPopupInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasPopup",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPopupLink",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PopupContent",
                table: "Books",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PopupLinkUrl",
                table: "Books",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasPopup",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "HasPopupLink",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "PopupContent",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "PopupLinkUrl",
                table: "Books");
        }
    }
}
