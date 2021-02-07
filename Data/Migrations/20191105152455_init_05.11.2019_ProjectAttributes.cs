using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Data.Migrations
{
    public partial class init_05112019_ProjectAttributes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalcDocTemplateVersion",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalcDocTemplateVersionPMP",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalcDocUploaded",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalcDocUploadedBy",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalcDocUploadedByPMP",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalcDocUploadedPMP",
                table: "Project",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalcDocTemplateVersion",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "CalcDocTemplateVersionPMP",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "CalcDocUploaded",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "CalcDocUploadedBy",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "CalcDocUploadedByPMP",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "CalcDocUploadedPMP",
                table: "Project");
        }
    }
}
