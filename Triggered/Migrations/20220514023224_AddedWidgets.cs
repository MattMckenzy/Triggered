using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Triggered.Migrations
{
    public partial class AddedWidgets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Widgets",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Markup = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Widgets", x => x.Key);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Widgets");
        }
    }
}
