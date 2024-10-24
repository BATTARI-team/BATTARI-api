using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BATTARI_api.Migrations
{
    /// <inheritdoc />
    public partial class fixfrienddatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frineds",
                table: "Friends");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Friends",
                newName: "User2Id");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Friends",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "User1Id",
                table: "Friends",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Friends");

            migrationBuilder.DropColumn(
                name: "User1Id",
                table: "Friends");

            migrationBuilder.RenameColumn(
                name: "User2Id",
                table: "Friends",
                newName: "UserId");

            migrationBuilder.AddColumn<string>(
                name: "Frineds",
                table: "Friends",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
