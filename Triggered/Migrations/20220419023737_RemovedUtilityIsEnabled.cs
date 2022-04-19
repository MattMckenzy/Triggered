using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Triggered.Migrations
{
    public partial class RemovedUtilityIsEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Utilities");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Utilities",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
