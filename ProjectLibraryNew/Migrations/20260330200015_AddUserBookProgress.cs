using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLibraryNew.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBookProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBookProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    IsTextRead = table.Column<bool>(type: "bit", nullable: false),
                    IsAnalysisRead = table.Column<bool>(type: "bit", nullable: false),
                    HasPassedTest = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBookProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBookProgresses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBookProgresses_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBookProgresses_BookId",
                table: "UserBookProgresses",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookProgresses_UserId",
                table: "UserBookProgresses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBookProgresses");
        }
    }
}
