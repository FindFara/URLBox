using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URLBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlVisibilityAndOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Urls",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Urls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Urls_CreatedByUserId",
                table: "Urls",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Urls_AspNetUsers_CreatedByUserId",
                table: "Urls",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Urls_AspNetUsers_CreatedByUserId",
                table: "Urls");

            migrationBuilder.DropIndex(
                name: "IX_Urls_CreatedByUserId",
                table: "Urls");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Urls");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Urls");
        }
    }
}
