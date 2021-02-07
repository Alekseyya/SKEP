using Microsoft.EntityFrameworkCore.Migrations;

namespace Data.Migrations
{
    public partial class init_02122019 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApiAccess",
                table: "RPCSUser",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ActivityType",
                table: "ProjectType",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Utilization",
                table: "ProjectType",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoImportTSRecordFromJIRA",
                table: "Project",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DisallowUserCreateTSRecord",
                table: "Project",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DismissalReason",
                table: "Employee",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApiAccess",
                table: "RPCSUser");

            migrationBuilder.DropColumn(
                name: "ActivityType",
                table: "ProjectType");

            migrationBuilder.DropColumn(
                name: "Utilization",
                table: "ProjectType");

            migrationBuilder.DropColumn(
                name: "AutoImportTSRecordFromJIRA",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "DisallowUserCreateTSRecord",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "DismissalReason",
                table: "Employee");
        }
    }
}
