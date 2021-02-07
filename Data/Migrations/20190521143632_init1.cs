using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Data.Migrations
{
    public partial class init1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppProperty",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProperty", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CostItem",
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
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    CostItemID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CostItem_CostItem_CostItemID",
                        column: x => x.CostItemID,
                        principalTable: "CostItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGrad",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeGrad", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLocation",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLocation", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePosition",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePosition", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Organisation",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisation", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCalendarRecord",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Year = table.Column<int>(nullable: false),
                    Month = table.Column<int>(nullable: false),
                    Day = table.Column<int>(nullable: false),
                    WorkingHours = table.Column<int>(nullable: false),
                    CalendarDate = table.Column<DateTime>(nullable: false),
                    IsCelebratory = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCalendarRecord", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRole",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShortName = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    RoleType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRole", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ProjectType",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectType", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "QualifyingRole",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShortName = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    RoleType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualifyingRole", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "RPCSUser",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    UserLogin = table.Column<string>(nullable: false),
                    IsAdmin = table.Column<bool>(nullable: false),
                    IsAdAdmin = table.Column<bool>(nullable: false),
                    IsHR = table.Column<bool>(nullable: false),
                    IsPM = table.Column<bool>(nullable: false),
                    IsPMOAdmin = table.Column<bool>(nullable: false),
                    IsPMOChief = table.Column<bool>(nullable: false),
                    IsDepartmentManager = table.Column<bool>(nullable: false),
                    IsDirector = table.Column<bool>(nullable: false),
                    IsFin = table.Column<bool>(nullable: false),
                    IsPayrollAdmin = table.Column<bool>(nullable: false),
                    IsDepartmentPayrollRead = table.Column<bool>(nullable: false),
                    IsPayrollFullRead = table.Column<bool>(nullable: false),
                    IsDataAdmin = table.Column<bool>(nullable: false),
                    IsTSAdmin = table.Column<bool>(nullable: false),
                    IsEmployee = table.Column<bool>(nullable: false),
                    OOLogin = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPCSUser", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CostSubItem",
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
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    CostItemID = table.Column<int>(nullable: false),
                    CostSubItemID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostSubItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CostSubItem_CostItem_CostItemID",
                        column: x => x.CostItemID,
                        principalTable: "CostItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostSubItem_CostSubItem_CostSubItemID",
                        column: x => x.CostSubItemID,
                        principalTable: "CostSubItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePositionOfficial",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    OrganisationID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePositionOfficial", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmployeePositionOfficial_Organisation_OrganisationID",
                        column: x => x.OrganisationID,
                        principalTable: "Organisation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChangeInfoRecord",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    BudgetLimitID = table.Column<int>(nullable: true),
                    CostItemID = table.Column<int>(nullable: true),
                    CostSubItemID = table.Column<int>(nullable: true),
                    DepartmentID = table.Column<int>(nullable: true),
                    EmployeeID = table.Column<int>(nullable: true),
                    ProjectID = table.Column<int>(nullable: true),
                    TSAutoHoursRecordID = table.Column<int>(nullable: true),
                    TSHoursRecordID = table.Column<int>(nullable: true),
                    VacationRecordID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeInfoRecord", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChangeInfoRecord_CostItem_CostItemID",
                        column: x => x.CostItemID,
                        principalTable: "CostItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChangeInfoRecord_CostSubItem_CostSubItemID",
                        column: x => x.CostSubItemID,
                        principalTable: "CostSubItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLimit",
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
                    CostSubItemID = table.Column<int>(nullable: true),
                    ProjectID = table.Column<int>(nullable: true),
                    DepartmentID = table.Column<int>(nullable: true),
                    Year = table.Column<int>(nullable: false),
                    Month = table.Column<int>(nullable: false),
                    LimitAmount = table.Column<decimal>(nullable: false),
                    LimitAmountApproved = table.Column<decimal>(nullable: false),
                    FundsExpendedAmount = table.Column<decimal>(nullable: false),
                    BudgetLimitID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLimit", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BudgetLimit_BudgetLimit_BudgetLimitID",
                        column: x => x.BudgetLimitID,
                        principalTable: "BudgetLimit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLimit_CostSubItem_CostSubItemID",
                        column: x => x.CostSubItemID,
                        principalTable: "CostSubItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpensesRecord",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ExpensesDate = table.Column<DateTime>(nullable: false),
                    CostSubItemID = table.Column<int>(nullable: false),
                    DepartmentID = table.Column<int>(nullable: false),
                    ProjectID = table.Column<int>(nullable: false),
                    Amount = table.Column<decimal>(nullable: false),
                    BitrixURegNum = table.Column<string>(nullable: false),
                    RecordStatus = table.Column<int>(nullable: false),
                    ExpensesRecordName = table.Column<string>(nullable: true),
                    SourceElementID = table.Column<string>(nullable: true),
                    SourceDB = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpensesRecord", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExpensesRecord_CostSubItem_CostSubItemID",
                        column: x => x.CostSubItemID,
                        principalTable: "CostSubItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
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
                    LastName = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    MidName = table.Column<string>(nullable: true),
                    BirthdayDate = table.Column<DateTime>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    ADLogin = table.Column<string>(nullable: true),
                    EnrollmentDate = table.Column<DateTime>(nullable: true),
                    DismissalDate = table.Column<DateTime>(nullable: true),
                    DepartmentID = table.Column<int>(nullable: true),
                    EmployeePositionID = table.Column<int>(nullable: true),
                    EmployeePositionOfficialID = table.Column<int>(nullable: true),
                    EmployeePositionTitle = table.Column<string>(nullable: true),
                    OrganisationID = table.Column<int>(nullable: true),
                    EmployeeLocationID = table.Column<int>(nullable: true),
                    OfficeName = table.Column<string>(nullable: true),
                    WorkPhoneNumber = table.Column<string>(nullable: true),
                    PersonalMobilePhoneNumber = table.Column<string>(nullable: true),
                    PublicMobilePhoneNumber = table.Column<string>(nullable: true),
                    SkypeLogin = table.Column<string>(nullable: true),
                    Specialization = table.Column<string>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    EmployeeGradID = table.Column<int>(nullable: true),
                    ADEmployeeID = table.Column<string>(nullable: true),
                    IsVacancy = table.Column<bool>(nullable: false),
                    VacancyID = table.Column<string>(nullable: true),
                    MedicalInsuranceInfo = table.Column<string>(nullable: true),
                    HomeAddress = table.Column<string>(nullable: true),
                    EmergencyContactName = table.Column<string>(nullable: true),
                    EmergencyContactMobilePhoneNumber = table.Column<string>(nullable: true),
                    EmployeeID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Employee_EmployeeGrad_EmployeeGradID",
                        column: x => x.EmployeeGradID,
                        principalTable: "EmployeeGrad",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_EmployeeLocation_EmployeeLocationID",
                        column: x => x.EmployeeLocationID,
                        principalTable: "EmployeeLocation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_EmployeePosition_EmployeePositionID",
                        column: x => x.EmployeePositionID,
                        principalTable: "EmployeePosition",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_EmployeePositionOfficial_EmployeePositionOfficialID",
                        column: x => x.EmployeePositionOfficialID,
                        principalTable: "EmployeePositionOfficial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Organisation_OrganisationID",
                        column: x => x.OrganisationID,
                        principalTable: "Organisation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Department",
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
                    ShortName = table.Column<string>(nullable: false),
                    ShortTitle = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: false),
                    ParentDepartmentID = table.Column<int>(nullable: true),
                    OrganisationID = table.Column<int>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    DepartmentManagerID = table.Column<int>(nullable: true),
                    DepartmentManagerAssistantID = table.Column<int>(nullable: true),
                    IsFinancialCentre = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Department_Employee_DepartmentManagerAssistantID",
                        column: x => x.DepartmentManagerAssistantID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Department_Employee_DepartmentManagerID",
                        column: x => x.DepartmentManagerID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Department_Organisation_OrganisationID",
                        column: x => x.OrganisationID,
                        principalTable: "Organisation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Department_Department_ParentDepartmentID",
                        column: x => x.ParentDepartmentID,
                        principalTable: "Department",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCategory",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    EmployeeID = table.Column<int>(nullable: false),
                    CategoryDateBegin = table.Column<DateTime>(nullable: true),
                    CategoryDateEnd = table.Column<DateTime>(nullable: true),
                    CategoryType = table.Column<int>(nullable: false),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCategory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmployeeCategory_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGradAssignment",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    EmployeeID = table.Column<int>(nullable: false),
                    EmployeeGradID = table.Column<int>(nullable: false),
                    BeginDate = table.Column<DateTime>(nullable: false),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeGradAssignment", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmployeeGradAssignment_EmployeeGrad_EmployeeGradID",
                        column: x => x.EmployeeGradID,
                        principalTable: "EmployeeGrad",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeGradAssignment_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePositionAssignment",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    EmployeeID = table.Column<int>(nullable: false),
                    EmployeePositionID = table.Column<int>(nullable: false),
                    EmployeePositionTitle = table.Column<string>(nullable: true),
                    BeginDate = table.Column<DateTime>(nullable: false),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePositionAssignment", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmployeePositionAssignment_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePositionAssignment_EmployeePosition_EmployeePositio~",
                        column: x => x.EmployeePositionID,
                        principalTable: "EmployeePosition",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePositionOfficialAssignment",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    EmployeeID = table.Column<int>(nullable: false),
                    EmployeePositionOfficialID = table.Column<int>(nullable: false),
                    BeginDate = table.Column<DateTime>(nullable: false),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePositionOfficialAssignment", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmployeePositionOfficialAssignment_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePositionOfficialAssignment_EmployeePositionOfficial~",
                        column: x => x.EmployeePositionOfficialID,
                        principalTable: "EmployeePositionOfficial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeQualifyingRole",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    EmployeeID = table.Column<int>(nullable: false),
                    QualifyingRoleID = table.Column<int>(nullable: false),
                    QualifyingRoleDateBegin = table.Column<DateTime>(nullable: true),
                    QualifyingRoleDateEnd = table.Column<DateTime>(nullable: true),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeQualifyingRole", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmployeeQualifyingRole_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeQualifyingRole_QualifyingRole_QualifyingRoleID",
                        column: x => x.QualifyingRoleID,
                        principalTable: "QualifyingRole",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacationRecord",
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
                    EmployeeID = table.Column<int>(nullable: false),
                    VacationBeginDate = table.Column<DateTime>(nullable: false),
                    VacationType = table.Column<int>(nullable: false),
                    VacationEndDate = table.Column<DateTime>(nullable: false),
                    VacationDays = table.Column<int>(nullable: false),
                    VacationRecordID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationRecord", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VacationRecord_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VacationRecord_VacationRecord_VacationRecordID",
                        column: x => x.VacationRecordID,
                        principalTable: "VacationRecord",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDepartmentAssignment",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    EmployeeID = table.Column<int>(nullable: false),
                    DepartmentID = table.Column<int>(nullable: false),
                    BeginDate = table.Column<DateTime>(nullable: false),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDepartmentAssignment", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmployeeDepartmentAssignment_Department_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "Department",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDepartmentAssignment_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project",
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
                    ShortName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    ProjectTypeID = table.Column<int>(nullable: true),
                    CustomerTitle = table.Column<string>(nullable: true),
                    BeginDate = table.Column<DateTime>(nullable: true),
                    EndDate = table.Column<DateTime>(nullable: true),
                    EmployeeCAMID = table.Column<int>(nullable: true),
                    EmployeePMID = table.Column<int>(nullable: true),
                    OrganisationID = table.Column<int>(nullable: true),
                    DepartmentID = table.Column<int>(nullable: true),
                    ContractAmount = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    SubcontractorsAmountBudget = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    OrganisationAmountBudget = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    EmployeePayrollBudget = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    OtherCostsBudget = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    ParentProjectID = table.Column<int>(nullable: true),
                    TotalHoursActual = table.Column<double>(nullable: true),
                    EmployeePayrollTotalAmountActual = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    IsArchived = table.Column<bool>(nullable: false),
                    IsCancelled = table.Column<bool>(nullable: false),
                    IsPaused = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Project_Department_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "Department",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Project_Employee_EmployeeCAMID",
                        column: x => x.EmployeeCAMID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Project_Employee_EmployeePMID",
                        column: x => x.EmployeePMID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Project_Organisation_OrganisationID",
                        column: x => x.OrganisationID,
                        principalTable: "Organisation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Project_ProjectType_ProjectTypeID",
                        column: x => x.ProjectTypeID,
                        principalTable: "ProjectType",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualifyingRoleRate",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DepartmentID = table.Column<int>(nullable: false),
                    QualifyingRoleID = table.Column<int>(nullable: false),
                    RateDateBegin = table.Column<DateTime>(nullable: false),
                    RateValue = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualifyingRoleRate", x => x.ID);
                    table.ForeignKey(
                        name: "FK_QualifyingRoleRate_Department_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "Department",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualifyingRoleRate_QualifyingRole_QualifyingRoleID",
                        column: x => x.QualifyingRoleID,
                        principalTable: "QualifyingRole",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMember",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ProjectID = table.Column<int>(nullable: true),
                    EmployeeID = table.Column<int>(nullable: true),
                    ProjectRoleID = table.Column<int>(nullable: true),
                    MembershipDateBegin = table.Column<DateTime>(nullable: true),
                    MembershipDateEnd = table.Column<DateTime>(nullable: true),
                    AssignmentPercentage = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMember", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ProjectMember_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMember_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMember_ProjectRole_ProjectRoleID",
                        column: x => x.ProjectRoleID,
                        principalTable: "ProjectRole",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectReportRecord",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ReportPeriodName = table.Column<string>(nullable: false),
                    ProjectID = table.Column<int>(nullable: true),
                    EmployeeID = table.Column<int>(nullable: true),
                    CalcDate = table.Column<DateTime>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    Hours = table.Column<double>(nullable: true),
                    EmployeeCount = table.Column<int>(nullable: true),
                    EmployeePayroll = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    EmployeeOvertimePayroll = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    EmployeePerformanceBonus = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    OtherCosts = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    TotalCosts = table.Column<decimal>(type: "decimal(32,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectReportRecord", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ProjectReportRecord_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectReportRecord_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStatusRecord",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    StatusPeriodName = table.Column<string>(nullable: false),
                    ProjectID = table.Column<int>(nullable: false),
                    ContractReceivedMoneyAmountActual = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    PaidToSubcontractorsAmountActual = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    EmployeePayrollAmountActual = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    OtherCostsAmountActual = table.Column<decimal>(type: "decimal(32,2)", nullable: true),
                    StatusText = table.Column<string>(nullable: false),
                    Created = table.Column<DateTime>(nullable: true),
                    RiskIndicatorFlag = table.Column<int>(nullable: false),
                    RiskIndicatorComments = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStatusRecord", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ProjectStatusRecord_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportingPeriod",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Year = table.Column<int>(nullable: false),
                    Month = table.Column<int>(nullable: false),
                    NewTSRecordsAllowedUntilDate = table.Column<DateTime>(nullable: false),
                    VacationProjectID = table.Column<int>(nullable: false),
                    VacationNoPaidProjectID = table.Column<int>(nullable: false),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingPeriod", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ReportingPeriod_Project_VacationNoPaidProjectID",
                        column: x => x.VacationNoPaidProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportingPeriod_Project_VacationProjectID",
                        column: x => x.VacationProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TSAutoHoursRecord",
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
                    EmployeeID = table.Column<int>(nullable: false),
                    BeginDate = table.Column<DateTime>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: false),
                    DayHours = table.Column<double>(nullable: false),
                    ProjectID = table.Column<int>(nullable: false),
                    TSAutoHoursRecordID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TSAutoHoursRecord", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TSAutoHoursRecord_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TSAutoHoursRecord_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TSAutoHoursRecord_TSAutoHoursRecord_TSAutoHoursRecordID",
                        column: x => x.TSAutoHoursRecordID,
                        principalTable: "TSAutoHoursRecord",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TSHoursRecord",
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
                    ProjectID = table.Column<int>(nullable: false),
                    EmployeeID = table.Column<int>(nullable: false),
                    RecordDate = table.Column<DateTime>(nullable: false),
                    Hours = table.Column<double>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    ParentTSAutoHoursRecordID = table.Column<int>(nullable: true),
                    ParentVacationRecordID = table.Column<int>(nullable: true),
                    RecordStatus = table.Column<int>(nullable: false),
                    RecordSource = table.Column<int>(nullable: false),
                    PMComment = table.Column<string>(nullable: true),
                    TSHoursRecordID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TSHoursRecord", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TSHoursRecord_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TSHoursRecord_TSAutoHoursRecord_ParentTSAutoHoursRecordID",
                        column: x => x.ParentTSAutoHoursRecordID,
                        principalTable: "TSAutoHoursRecord",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TSHoursRecord_VacationRecord_ParentVacationRecordID",
                        column: x => x.ParentVacationRecordID,
                        principalTable: "VacationRecord",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TSHoursRecord_Project_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Project",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TSHoursRecord_TSHoursRecord_TSHoursRecordID",
                        column: x => x.TSHoursRecordID,
                        principalTable: "TSHoursRecord",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLimit_BudgetLimitID",
                table: "BudgetLimit",
                column: "BudgetLimitID");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLimit_CostSubItemID",
                table: "BudgetLimit",
                column: "CostSubItemID");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLimit_DepartmentID",
                table: "BudgetLimit",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLimit_ProjectID",
                table: "BudgetLimit",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_BudgetLimitID",
                table: "ChangeInfoRecord",
                column: "BudgetLimitID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_CostItemID",
                table: "ChangeInfoRecord",
                column: "CostItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_CostSubItemID",
                table: "ChangeInfoRecord",
                column: "CostSubItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_DepartmentID",
                table: "ChangeInfoRecord",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_EmployeeID",
                table: "ChangeInfoRecord",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_ProjectID",
                table: "ChangeInfoRecord",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_TSAutoHoursRecordID",
                table: "ChangeInfoRecord",
                column: "TSAutoHoursRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_TSHoursRecordID",
                table: "ChangeInfoRecord",
                column: "TSHoursRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfoRecord_VacationRecordID",
                table: "ChangeInfoRecord",
                column: "VacationRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_CostItem_CostItemID",
                table: "CostItem",
                column: "CostItemID");

            migrationBuilder.CreateIndex(
                name: "IX_CostSubItem_CostItemID",
                table: "CostSubItem",
                column: "CostItemID");

            migrationBuilder.CreateIndex(
                name: "IX_CostSubItem_CostSubItemID",
                table: "CostSubItem",
                column: "CostSubItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentManagerAssistantID",
                table: "Department",
                column: "DepartmentManagerAssistantID");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentManagerID",
                table: "Department",
                column: "DepartmentManagerID");

            migrationBuilder.CreateIndex(
                name: "IX_Department_OrganisationID",
                table: "Department",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_Department_ParentDepartmentID",
                table: "Department",
                column: "ParentDepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_DepartmentID",
                table: "Employee",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_EmployeeGradID",
                table: "Employee",
                column: "EmployeeGradID");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_EmployeeID",
                table: "Employee",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_EmployeeLocationID",
                table: "Employee",
                column: "EmployeeLocationID");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_EmployeePositionID",
                table: "Employee",
                column: "EmployeePositionID");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_EmployeePositionOfficialID",
                table: "Employee",
                column: "EmployeePositionOfficialID");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_OrganisationID",
                table: "Employee",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategory_EmployeeID",
                table: "EmployeeCategory",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDepartmentAssignment_DepartmentID",
                table: "EmployeeDepartmentAssignment",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDepartmentAssignment_EmployeeID",
                table: "EmployeeDepartmentAssignment",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGradAssignment_EmployeeGradID",
                table: "EmployeeGradAssignment",
                column: "EmployeeGradID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGradAssignment_EmployeeID",
                table: "EmployeeGradAssignment",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionAssignment_EmployeeID",
                table: "EmployeePositionAssignment",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionAssignment_EmployeePositionID",
                table: "EmployeePositionAssignment",
                column: "EmployeePositionID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionOfficial_OrganisationID",
                table: "EmployeePositionOfficial",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionOfficialAssignment_EmployeeID",
                table: "EmployeePositionOfficialAssignment",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionOfficialAssignment_EmployeePositionOfficial~",
                table: "EmployeePositionOfficialAssignment",
                column: "EmployeePositionOfficialID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeQualifyingRole_EmployeeID",
                table: "EmployeeQualifyingRole",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeQualifyingRole_QualifyingRoleID",
                table: "EmployeeQualifyingRole",
                column: "QualifyingRoleID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensesRecord_CostSubItemID",
                table: "ExpensesRecord",
                column: "CostSubItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensesRecord_DepartmentID",
                table: "ExpensesRecord",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensesRecord_ProjectID",
                table: "ExpensesRecord",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Project_DepartmentID",
                table: "Project",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Project_EmployeeCAMID",
                table: "Project",
                column: "EmployeeCAMID");

            migrationBuilder.CreateIndex(
                name: "IX_Project_EmployeePMID",
                table: "Project",
                column: "EmployeePMID");

            migrationBuilder.CreateIndex(
                name: "IX_Project_OrganisationID",
                table: "Project",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProjectTypeID",
                table: "Project",
                column: "ProjectTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_EmployeeID",
                table: "ProjectMember",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_ProjectID",
                table: "ProjectMember",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_ProjectRoleID",
                table: "ProjectMember",
                column: "ProjectRoleID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReportRecord_EmployeeID",
                table: "ProjectReportRecord",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReportRecord_ProjectID",
                table: "ProjectReportRecord",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatusRecord_ProjectID",
                table: "ProjectStatusRecord",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_QualifyingRoleRate_DepartmentID",
                table: "QualifyingRoleRate",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_QualifyingRoleRate_QualifyingRoleID",
                table: "QualifyingRoleRate",
                column: "QualifyingRoleID");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingPeriod_VacationNoPaidProjectID",
                table: "ReportingPeriod",
                column: "VacationNoPaidProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingPeriod_VacationProjectID",
                table: "ReportingPeriod",
                column: "VacationProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_TSAutoHoursRecord_EmployeeID",
                table: "TSAutoHoursRecord",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_TSAutoHoursRecord_ProjectID",
                table: "TSAutoHoursRecord",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_TSAutoHoursRecord_TSAutoHoursRecordID",
                table: "TSAutoHoursRecord",
                column: "TSAutoHoursRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_TSHoursRecord_EmployeeID",
                table: "TSHoursRecord",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_TSHoursRecord_ParentTSAutoHoursRecordID",
                table: "TSHoursRecord",
                column: "ParentTSAutoHoursRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_TSHoursRecord_ParentVacationRecordID",
                table: "TSHoursRecord",
                column: "ParentVacationRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_TSHoursRecord_ProjectID",
                table: "TSHoursRecord",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_TSHoursRecord_TSHoursRecordID",
                table: "TSHoursRecord",
                column: "TSHoursRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRecord_EmployeeID",
                table: "VacationRecord",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRecord_VacationRecordID",
                table: "VacationRecord",
                column: "VacationRecordID");

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_Department_DepartmentID",
                table: "ChangeInfoRecord",
                column: "DepartmentID",
                principalTable: "Department",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_Project_ProjectID",
                table: "ChangeInfoRecord",
                column: "ProjectID",
                principalTable: "Project",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_BudgetLimit_BudgetLimitID",
                table: "ChangeInfoRecord",
                column: "BudgetLimitID",
                principalTable: "BudgetLimit",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_Employee_EmployeeID",
                table: "ChangeInfoRecord",
                column: "EmployeeID",
                principalTable: "Employee",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_TSAutoHoursRecord_TSAutoHoursRecordID",
                table: "ChangeInfoRecord",
                column: "TSAutoHoursRecordID",
                principalTable: "TSAutoHoursRecord",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_TSHoursRecord_TSHoursRecordID",
                table: "ChangeInfoRecord",
                column: "TSHoursRecordID",
                principalTable: "TSHoursRecord",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeInfoRecord_VacationRecord_VacationRecordID",
                table: "ChangeInfoRecord",
                column: "VacationRecordID",
                principalTable: "VacationRecord",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLimit_Department_DepartmentID",
                table: "BudgetLimit",
                column: "DepartmentID",
                principalTable: "Department",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLimit_Project_ProjectID",
                table: "BudgetLimit",
                column: "ProjectID",
                principalTable: "Project",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpensesRecord_Department_DepartmentID",
                table: "ExpensesRecord",
                column: "DepartmentID",
                principalTable: "Department",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpensesRecord_Project_ProjectID",
                table: "ExpensesRecord",
                column: "ProjectID",
                principalTable: "Project",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employee_Department_DepartmentID",
                table: "Employee",
                column: "DepartmentID",
                principalTable: "Department",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employee_Department_DepartmentID",
                table: "Employee");

            migrationBuilder.DropTable(
                name: "AppProperty");

            migrationBuilder.DropTable(
                name: "ChangeInfoRecord");

            migrationBuilder.DropTable(
                name: "EmployeeCategory");

            migrationBuilder.DropTable(
                name: "EmployeeDepartmentAssignment");

            migrationBuilder.DropTable(
                name: "EmployeeGradAssignment");

            migrationBuilder.DropTable(
                name: "EmployeePositionAssignment");

            migrationBuilder.DropTable(
                name: "EmployeePositionOfficialAssignment");

            migrationBuilder.DropTable(
                name: "EmployeeQualifyingRole");

            migrationBuilder.DropTable(
                name: "ExpensesRecord");

            migrationBuilder.DropTable(
                name: "ProductionCalendarRecord");

            migrationBuilder.DropTable(
                name: "ProjectMember");

            migrationBuilder.DropTable(
                name: "ProjectReportRecord");

            migrationBuilder.DropTable(
                name: "ProjectStatusRecord");

            migrationBuilder.DropTable(
                name: "QualifyingRoleRate");

            migrationBuilder.DropTable(
                name: "ReportingPeriod");

            migrationBuilder.DropTable(
                name: "RPCSUser");

            migrationBuilder.DropTable(
                name: "BudgetLimit");

            migrationBuilder.DropTable(
                name: "TSHoursRecord");

            migrationBuilder.DropTable(
                name: "ProjectRole");

            migrationBuilder.DropTable(
                name: "QualifyingRole");

            migrationBuilder.DropTable(
                name: "CostSubItem");

            migrationBuilder.DropTable(
                name: "TSAutoHoursRecord");

            migrationBuilder.DropTable(
                name: "VacationRecord");

            migrationBuilder.DropTable(
                name: "CostItem");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "ProjectType");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "EmployeeGrad");

            migrationBuilder.DropTable(
                name: "EmployeeLocation");

            migrationBuilder.DropTable(
                name: "EmployeePosition");

            migrationBuilder.DropTable(
                name: "EmployeePositionOfficial");

            migrationBuilder.DropTable(
                name: "Organisation");
        }
    }
}
