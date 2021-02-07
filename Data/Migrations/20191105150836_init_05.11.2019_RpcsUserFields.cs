using Microsoft.EntityFrameworkCore.Migrations;

namespace Data.Migrations
{
    public partial class init_05112019_RpcsUserFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIDDocsAdmin",
                table: "RPCSUser",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIDDocsAdmin",
                table: "RPCSUser");
        }
    }
}
