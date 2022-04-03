using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Triggered.Migrations
{
    public partial class AddedDataServiceObjects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataObjects",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Depth = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpandoObjectJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataObjects", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataObjects_Depth",
                table: "DataObjects",
                column: "Depth");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataObjects");
        }
    }
}
