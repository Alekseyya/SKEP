using Microsoft.EntityFrameworkCore.Migrations;

namespace Data.Migrations
{
    public partial class init_3010_2019 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPayment",
                table: "ProjectScheduleEntry");

            migrationBuilder.AddColumn<string>(
                name: "ContractNum",
                table: "ProjectScheduleEntry",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractStageNum",
                table: "ProjectScheduleEntry",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractNum",
                table: "ProjectScheduleEntry");

            migrationBuilder.DropColumn(
                name: "ContractStageNum",
                table: "ProjectScheduleEntry");

            migrationBuilder.AddColumn<bool>(
                name: "IsPayment",
                table: "ProjectScheduleEntry",
                nullable: false,
                defaultValue: false);
        }
    }
}
