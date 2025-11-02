using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tufo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RouteNotes_UserId",
                table: "RouteNotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_CreatedAt",
                table: "Media",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Media_UserId",
                table: "Media",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_AspNetUsers_UserId",
                table: "Media",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RouteNotes_AspNetUsers_UserId",
                table: "RouteNotes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_AspNetUsers_UserId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteNotes_AspNetUsers_UserId",
                table: "RouteNotes");

            migrationBuilder.DropIndex(
                name: "IX_RouteNotes_UserId",
                table: "RouteNotes");

            migrationBuilder.DropIndex(
                name: "IX_Media_CreatedAt",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_UserId",
                table: "Media");
        }
    }
}
