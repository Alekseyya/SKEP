using Microsoft.EntityFrameworkCore.Migrations;

namespace Data.Migrations
{
    public partial class init_11022020 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceElementID",
                table: "EmployeeGradAssignment",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceListID",
                table: "EmployeeGradAssignment",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutonomous",
                table: "Department",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalSourceElementID",
                table: "EmployeeGradAssignment");

            migrationBuilder.DropColumn(
                name: "ExternalSourceListID",
                table: "EmployeeGradAssignment");

            migrationBuilder.DropColumn(
                name: "IsAutonomous",
                table: "Department");
        }
    }
}
