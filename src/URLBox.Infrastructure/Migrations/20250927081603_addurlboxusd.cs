using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URLBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addurlboxusd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetRoles_RolesId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_RolesId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RolesId",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "TeamsId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TeamsId",
                table: "Projects",
                column: "TeamsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Teams_TeamsId",
                table: "Projects",
                column: "TeamsId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Teams_TeamsId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TeamsId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TeamsId",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "RolesId",
                table: "Projects",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_RolesId",
                table: "Projects",
                column: "RolesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetRoles_RolesId",
                table: "Projects",
                column: "RolesId",
                principalTable: "AspNetRoles",
                principalColumn: "Id");
        }
    }
}
