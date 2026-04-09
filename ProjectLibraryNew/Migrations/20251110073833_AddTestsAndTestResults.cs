using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLibraryNew.Migrations
{
    /// <inheritdoc />
    public partial class AddTestsAndTestResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResult_AspNetUsers_UserId",
                table: "TestResult");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResult_Tests_TestId",
                table: "TestResult");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestResult",
                table: "TestResult");

            migrationBuilder.RenameTable(
                name: "TestResult",
                newName: "TestResults");

            migrationBuilder.RenameIndex(
                name: "IX_TestResult_UserId",
                table: "TestResults",
                newName: "IX_TestResults_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TestResult_TestId",
                table: "TestResults",
                newName: "IX_TestResults_TestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestResults",
                table: "TestResults",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_AspNetUsers_UserId",
                table: "TestResults",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_AspNetUsers_UserId",
                table: "TestResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestResults",
                table: "TestResults");

            migrationBuilder.RenameTable(
                name: "TestResults",
                newName: "TestResult");

            migrationBuilder.RenameIndex(
                name: "IX_TestResults_UserId",
                table: "TestResult",
                newName: "IX_TestResult_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TestResults_TestId",
                table: "TestResult",
                newName: "IX_TestResult_TestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestResult",
                table: "TestResult",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResult_AspNetUsers_UserId",
                table: "TestResult",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResult_Tests_TestId",
                table: "TestResult",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
