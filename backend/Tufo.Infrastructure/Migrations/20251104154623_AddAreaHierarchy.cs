using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tufo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentAreaId",
                table: "Areas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_ParentAreaId",
                table: "Areas",
                column: "ParentAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Areas_ParentAreaId",
                table: "Areas",
                column: "ParentAreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Areas_ParentAreaId",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_ParentAreaId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "ParentAreaId",
                table: "Areas");
        }
    }
}
