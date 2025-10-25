using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URLBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addpublicurls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Urls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Urls",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Urls");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Urls");
        }
    }
}
