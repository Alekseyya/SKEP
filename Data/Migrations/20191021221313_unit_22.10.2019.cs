using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Data.Migrations
{
    public partial class unit_22102019 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "VacationRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "VacationRecord",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "VacationRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceElementID",
                table: "VacationRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceListID",
                table: "VacationRecord",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VacationRecord",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RecordSource",
                table: "VacationRecord",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TSHoursRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "TSHoursRecord",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TSHoursRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceElementID",
                table: "TSHoursRecord",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceListID",
                table: "TSHoursRecord",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TSHoursRecord",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TSAutoHoursRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "TSAutoHoursRecord",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TSAutoHoursRecord",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TSAutoHoursRecord",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowSendEmailNotifications",
                table: "RPCSUser",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TSRecordsEditApproveAllowedUntilDate",
                table: "ReportingPeriod",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ActualAverageHourPayrollValue",
                table: "QualifyingRoleRate",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualAverageMonthPayrollValue",
                table: "QualifyingRoleRate",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FRCCorrectionFactorValue",
                table: "QualifyingRoleRate",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FRCInflationRateValue",
                table: "QualifyingRoleRate",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HoursPlan",
                table: "QualifyingRoleRate",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthRateValue",
                table: "QualifyingRoleRate",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessTripCostSubItemID",
                table: "ProjectType",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TSApproveMode",
                table: "ProjectType",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "RiskIndicatorComments",
                table: "ProjectStatusRecord",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorSID",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Editor",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorSID",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalDependenciesInfo",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProjectStatusRecord",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVersion",
                table: "ProjectStatusRecord",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ItemID",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlannedReleaseInfo",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProblemsText",
                table: "ProjectStatusRecord",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProjectStatusBeginDate",
                table: "ProjectStatusRecord",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ProjectStatusEndDate",
                table: "ProjectStatusRecord",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ProjectStatusRecordID",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedSolutionText",
                table: "ProjectStatusRecord",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupervisorComments",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "ProjectStatusRecord",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowTSRecordOnlyWorkingDays",
                table: "Project",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowTSRecordWithoutProjectMembership",
                table: "Project",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ContractEquipmentResaleAmount",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeHoursBudget",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeHoursBudgetPMP",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeePAID",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeePayrollBudgetPMP",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EquipmentCostsForResale",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Project",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCostsBudgetPMP",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionDepartmentID",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectDocsURL",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubcontractorsAmountBudgetPMP",
                table: "Project",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountNoVAT",
                table: "ExpensesRecord",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountReserved",
                table: "ExpensesRecord",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AmountReservedApprovedActualDate",
                table: "ExpensesRecord",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountReservedNoVAT",
                table: "ExpensesRecord",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentCompletedActualDate",
                table: "ExpensesRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceListID",
                table: "ExpensesRecord",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AirlineCardInfo",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ForeignPassportDueDate",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForeignPassportNumber",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InternationalPassportDueDate",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternationalPassportName",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternationalPassportNumber",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employee",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProbationEndDate",
                table: "Employee",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Department",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "Department",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Department",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentPAID",
                table: "Department",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Department",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsePayrollHalfYearValue",
                table: "Department",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsePayrollQuarterValue",
                table: "Department",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsePayrollYearValue",
                table: "Department",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CostSubItem",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "CostSubItem",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "CostSubItem",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CostSubItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProjectBusinessTripCosts",
                table: "CostSubItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProjectEquipmentCostsForResale",
                table: "CostSubItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProjectOtherCosts",
                table: "CostSubItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProjectPerformanceBonusCosts",
                table: "CostSubItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProjectSubcontractorsCosts",
                table: "CostSubItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CostItem",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "CostItem",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "CostItem",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CostItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProjectScheduleEntryID",
                table: "ChangeInfoRecord",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectScheduleEntryTypeID",
                table: "ChangeInfoRecord",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectStatusRecordID",
                table: "ChangeInfoRecord",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "LimitAmountApproved",
                table: "BudgetLimit",
                nullable: true,
                oldClrType: typeof(decimal));

            migrationBuilder.AlterColumn<decimal>(
                name: "FundsExpendedAmount",
                table: "BudgetLimit",
                nullable: true,
                oldClrType: typeof(decimal));

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "BudgetLimit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBySID",
                table: "BudgetLimit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "BudgetLimit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BudgetLimit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProjectScheduleEntryType",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ItemID = table.Column<int>(nullable: true),
                    IsVersion = table.Column<bool>(nullable: false),
                    VersionNumber = table.Column<int>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Modified = table.Column<DateTime>(nullable: true),
                    Author = table.Column<string>(nullable: true),
                    AuthorSID = table.Column<string>(nullable: true),
                    Editor = table.Column<string>(nullable: true),
                    EditorSID = table.Column<string>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    DeletedDate = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<string>(nullable: true),
                    DeletedBySID = table.Column<string>(nullable: true),
                    ShortName = table.Column<string>(nullable: false),
                    WBSCode = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    ProjectTypeID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectScheduleEntryType", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ProjectScheduleEntryType_ProjectType_ProjectTypeID",
                        column: x => x.ProjectTypeID,
                        principalTable: "ProjectType",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectScheduleEntry",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ItemID = table.Column<int>(nullable: true),
                    IsVersion = table.Column<bool>(nullable: false),
                    VersionNumber = table.Column<int>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Modified = table.Column<DateTime>(nullable: true),
                    Author = table.Column<string>(nullable: true),
                    AuthorSID = table.Column<string>(nullable: true),
                    Editor = table.Column<string>(nullable: true),
                    EditorSID = table.Column<string>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    DeletedDate = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<string>(nullable: true),
                    DeletedBySID = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: false),
                    Amount = table.Column<decimal>(nullable: true),
                    ProjectScheduleEntryTypeID = table.Column<int>(nullable: true),
                    ProjectID = table.Column<int>(nullable: false),
                    IsPayment = table.Column<bool>(nullable: false),
                    IncludeInProjectStatusRecord = table.Column<bool>(nullable: false),
                    DueDate = table.Column<DateTime>(nullable: true),
                    ExpectedDueDate = table.Column<DateTime>(nullable: true),
                    DateCompleted = table.Column<DateTime>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    ProjectScheduleEntryID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectScheduleEntry", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ProjectScheduleEntry_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectScheduleEntry_ProjectScheduleEntry_ProjectScheduleEn~",
                        column: x => x.ProjectScheduleEntryID,
                        principalTable: "ProjectScheduleEntry",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectScheduleEntry_ProjectScheduleEntryType_ProjectSchedu~",
                        column: x => x.ProjectScheduleEntryTypeID,
                        principalTable: "ProjectScheduleEntryType",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectType_BusinessTripCostSubItemID",
                table: "ProjectType",
                column: "BusinessTripCostSubItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatusRecord_ProjectStatusRecordID",
                table: "ProjectStatusRecord",
                column: "ProjectStatusRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_Project_EmployeePAID",
                table: "Project",
                column: "EmployeePAID");

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProductionDepartmentID",
                table: "Project",
                column: "ProductionDepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentPAID",
                table: "Department",
                column: "DepartmentPAID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_ProjectScheduleEntryID",
                table: "ChangeInfoRecord",
                column: "ProjectScheduleEntryID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_ProjectScheduleEntryTypeID",
                table: "ChangeInfoRecord",
                column: "ProjectScheduleEntryTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_ProjectStatusRecordID",
                table: "ChangeInfoRecord",
                column: "ProjectStatusRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScheduleEntry_ProjectID",
                table: "ProjectScheduleEntry",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScheduleEntry_ProjectScheduleEntryID",
                table: "ProjectScheduleEntry",
                column: "ProjectScheduleEntryID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScheduleEntry_ProjectScheduleEntryTypeID",
                table: "ProjectScheduleEntry",
                column: "ProjectScheduleEntryTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScheduleEntryType_ProjectTypeID",
                table: "ProjectScheduleEntryType",
                column: "ProjectTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_ProjectScheduleEntry_ProjectScheduleEntryID",
                table: "ChangeInfoRecord",
                column: "ProjectScheduleEntryID",
                principalTable: "ProjectScheduleEntry",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_ProjectScheduleEntryType_ProjectScheduleEn~",
                table: "ChangeInfoRecord",
                column: "ProjectScheduleEntryTypeID",
                principalTable: "ProjectScheduleEntryType",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_ProjectStatusRecord_ProjectStatusRecordID",
                table: "ChangeInfoRecord",
                column: "ProjectStatusRecordID",
                principalTable: "ProjectStatusRecord",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Employee_DepartmentPAID",
                table: "Department",
                column: "DepartmentPAID",
                principalTable: "Employee",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Employee_EmployeePAID",
                table: "Project",
                column: "EmployeePAID",
                principalTable: "Employee",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Department_ProductionDepartmentID",
                table: "Project",
                column: "ProductionDepartmentID",
                principalTable: "Department",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectStatusRecord_ProjectStatusRecord_ProjectStatusRecord~",
                table: "ProjectStatusRecord",
                column: "ProjectStatusRecordID",
                principalTable: "ProjectStatusRecord",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectType_CostSubItem_BusinessTripCostSubItemID",
                table: "ProjectType",
                column: "BusinessTripCostSubItemID",
                principalTable: "CostSubItem",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChangeInfoRecord_ProjectScheduleEntry_ProjectScheduleEntryID",
                table: "ChangeInfoRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_ChangeInfoRecord_ProjectScheduleEntryType_ProjectScheduleEn~",
                table: "ChangeInfoRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_ChangeInfoRecord_ProjectStatusRecord_ProjectStatusRecordID",
                table: "ChangeInfoRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_Department_Employee_DepartmentPAID",
                table: "Department");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_Employee_EmployeePAID",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_Department_ProductionDepartmentID",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectStatusRecord_ProjectStatusRecord_ProjectStatusRecord~",
                table: "ProjectStatusRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectType_CostSubItem_BusinessTripCostSubItemID",
                table: "ProjectType");

            migrationBuilder.DropTable(
                name: "ProjectScheduleEntry");

            migrationBuilder.DropTable(
                name: "ProjectScheduleEntryType");

            migrationBuilder.DropIndex(
                name: "IX_ProjectType_BusinessTripCostSubItemID",
                table: "ProjectType");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStatusRecord_ProjectStatusRecordID",
                table: "ProjectStatusRecord");

            migrationBuilder.DropIndex(
                name: "IX_Project_EmployeePAID",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_ProductionDepartmentID",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Department_DepartmentPAID",
                table: "Department");

            migrationBuilder.DropIndex(
                name: "IX_ChangeInfoRecord_ProjectScheduleEntryID",
                table: "ChangeInfoRecord");

            migrationBuilder.DropIndex(
                name: "IX_ChangeInfoRecord_ProjectScheduleEntryTypeID",
                table: "ChangeInfoRecord");

            migrationBuilder.DropIndex(
                name: "IX_ChangeInfoRecord_ProjectStatusRecordID",
                table: "ChangeInfoRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "VacationRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "VacationRecord");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "VacationRecord");

            migrationBuilder.DropColumn(
                name: "ExternalSourceElementID",
                table: "VacationRecord");

            migrationBuilder.DropColumn(
                name: "ExternalSourceListID",
                table: "VacationRecord");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VacationRecord");

            migrationBuilder.DropColumn(
                name: "RecordSource",
                table: "VacationRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TSHoursRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "TSHoursRecord");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TSHoursRecord");

            migrationBuilder.DropColumn(
                name: "ExternalSourceElementID",
                table: "TSHoursRecord");

            migrationBuilder.DropColumn(
                name: "ExternalSourceListID",
                table: "TSHoursRecord");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TSHoursRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TSAutoHoursRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "TSAutoHoursRecord");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TSAutoHoursRecord");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TSAutoHoursRecord");

            migrationBuilder.DropColumn(
                name: "AllowSendEmailNotifications",
                table: "RPCSUser");

            migrationBuilder.DropColumn(
                name: "TSRecordsEditApproveAllowedUntilDate",
                table: "ReportingPeriod");

            migrationBuilder.DropColumn(
                name: "ActualAverageHourPayrollValue",
                table: "QualifyingRoleRate");

            migrationBuilder.DropColumn(
                name: "ActualAverageMonthPayrollValue",
                table: "QualifyingRoleRate");

            migrationBuilder.DropColumn(
                name: "FRCCorrectionFactorValue",
                table: "QualifyingRoleRate");

            migrationBuilder.DropColumn(
                name: "FRCInflationRateValue",
                table: "QualifyingRoleRate");

            migrationBuilder.DropColumn(
                name: "HoursPlan",
                table: "QualifyingRoleRate");

            migrationBuilder.DropColumn(
                name: "MonthRateValue",
                table: "QualifyingRoleRate");

            migrationBuilder.DropColumn(
                name: "BusinessTripCostSubItemID",
                table: "ProjectType");

            migrationBuilder.DropColumn(
                name: "TSApproveMode",
                table: "ProjectType");

            migrationBuilder.DropColumn(
                name: "Author",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "AuthorSID",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "Editor",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "EditorSID",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "ExternalDependenciesInfo",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "IsVersion",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "ItemID",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "PlannedReleaseInfo",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "ProblemsText",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "ProjectStatusBeginDate",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "ProjectStatusEndDate",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "ProjectStatusRecordID",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "ProposedSolutionText",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "SupervisorComments",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "ProjectStatusRecord");

            migrationBuilder.DropColumn(
                name: "AllowTSRecordOnlyWorkingDays",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "AllowTSRecordWithoutProjectMembership",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "ContractEquipmentResaleAmount",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "EmployeeHoursBudget",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "EmployeeHoursBudgetPMP",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "EmployeePAID",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "EmployeePayrollBudgetPMP",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "EquipmentCostsForResale",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "OtherCostsBudgetPMP",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "ProductionDepartmentID",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "ProjectDocsURL",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "SubcontractorsAmountBudgetPMP",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "AmountNoVAT",
                table: "ExpensesRecord");

            migrationBuilder.DropColumn(
                name: "AmountReserved",
                table: "ExpensesRecord");

            migrationBuilder.DropColumn(
                name: "AmountReservedApprovedActualDate",
                table: "ExpensesRecord");

            migrationBuilder.DropColumn(
                name: "AmountReservedNoVAT",
                table: "ExpensesRecord");

            migrationBuilder.DropColumn(
                name: "PaymentCompletedActualDate",
                table: "ExpensesRecord");

            migrationBuilder.DropColumn(
                name: "SourceListID",
                table: "ExpensesRecord");

            migrationBuilder.DropColumn(
                name: "AirlineCardInfo",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "ForeignPassportDueDate",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "ForeignPassportNumber",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "InternationalPassportDueDate",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "InternationalPassportName",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "InternationalPassportNumber",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "ProbationEndDate",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "DepartmentPAID",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "UsePayrollHalfYearValue",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "UsePayrollQuarterValue",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "UsePayrollYearValue",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "IsProjectBusinessTripCosts",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "IsProjectEquipmentCostsForResale",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "IsProjectOtherCosts",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "IsProjectPerformanceBonusCosts",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "IsProjectSubcontractorsCosts",
                table: "CostSubItem");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CostItem");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "CostItem");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "CostItem");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CostItem");

            migrationBuilder.DropColumn(
                name: "ProjectScheduleEntryID",
                table: "ChangeInfoRecord");

            migrationBuilder.DropColumn(
                name: "ProjectScheduleEntryTypeID",
                table: "ChangeInfoRecord");

            migrationBuilder.DropColumn(
                name: "ProjectStatusRecordID",
                table: "ChangeInfoRecord");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BudgetLimit");

            migrationBuilder.DropColumn(
                name: "DeletedBySID",
                table: "BudgetLimit");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "BudgetLimit");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BudgetLimit");

            migrationBuilder.AlterColumn<string>(
                name: "RiskIndicatorComments",
                table: "ProjectStatusRecord",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "LimitAmountApproved",
                table: "BudgetLimit",
                nullable: false,
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FundsExpendedAmount",
                table: "BudgetLimit",
                nullable: false,
                oldClrType: typeof(decimal),
                oldNullable: true);
        }
    }
}
