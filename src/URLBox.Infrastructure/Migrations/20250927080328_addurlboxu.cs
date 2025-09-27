using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URLBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addurlboxu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetRoles_ApplicationRoleId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "ApplicationRoleId",
                table: "Projects",
                newName: "RolesId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ApplicationRoleId",
                table: "Projects",
                newName: "IX_Projects_RolesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetRoles_RolesId",
                table: "Projects",
                column: "RolesId",
                principalTable: "AspNetRoles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetRoles_RolesId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "RolesId",
                table: "Projects",
                newName: "ApplicationRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_RolesId",
                table: "Projects",
                newName: "IX_Projects_ApplicationRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetRoles_ApplicationRoleId",
                table: "Projects",
                column: "ApplicationRoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id");
        }
    }
}
