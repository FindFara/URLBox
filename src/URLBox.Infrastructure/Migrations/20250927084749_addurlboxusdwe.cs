using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URLBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addurlboxusdwe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tag",
                table: "Urls");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "Urls",
                newName: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Urls_ProjectId",
                table: "Urls",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Urls_Projects_ProjectId",
                table: "Urls",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Urls_Projects_ProjectId",
                table: "Urls");

            migrationBuilder.DropIndex(
                name: "IX_Urls_ProjectId",
                table: "Urls");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "Urls",
                newName: "Order");

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "Urls",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
