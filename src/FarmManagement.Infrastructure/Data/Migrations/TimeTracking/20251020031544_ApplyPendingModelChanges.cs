using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Data.Migrations.TimeTracking
{
    /// <inheritdoc />
    public partial class ApplyPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayCalendars",
                columns: table => new
                {
                    PayCalendarId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartPeriodDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndPeriodDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Fortnightly"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    IsPayrollGenerated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayCalendars", x => x.PayCalendarId);
                });

            migrationBuilder.CreateTable(
                name: "PayRates",
                columns: table => new
                {
                    PayRateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Regular"),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayRates", x => x.PayRateId);
                });

            migrationBuilder.CreateTable(
                name: "PayrollOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayFrequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FortnightlyDays = table.Column<int>(type: "int", nullable: true),
                    CasualOvertimeThresholdHours = table.Column<int>(type: "int", nullable: true),
                    PaidBreakMinutes = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    PayrollRunId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayCalendarId = table.Column<int>(type: "int", nullable: false),
                    TotalLabourCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWorkHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StaffCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    RunNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.PayrollRunId);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_PayCalendars_PayCalendarId",
                        column: x => x.PayCalendarId,
                        principalTable: "PayCalendars",
                        principalColumn: "PayCalendarId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollLineItems",
                columns: table => new
                {
                    PayrollLineItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    StaffNumber = table.Column<string>(type: "nchar(7)", maxLength: 7, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContractType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegularHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegularHourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OvertimeHourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossWages = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetWages = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollLineItems", x => x.PayrollLineItemId);
                    table.ForeignKey(
                        name: "FK_PayrollLineItems_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "PayrollRunId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollLineItems_Staff_StaffNumber",
                        column: x => x.StaffNumber,
                        principalTable: "Staff",
                        principalColumn: "StaffNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayCalendars_StartPeriodDate_EndPeriodDate",
                table: "PayCalendars",
                columns: new[] { "StartPeriodDate", "EndPeriodDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PayRates_ContractType_RateType_IsActive",
                table: "PayRates",
                columns: new[] { "ContractType", "RateType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PayRates_EffectiveFrom",
                table: "PayRates",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLineItems_PayrollRunId",
                table: "PayrollLineItems",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLineItems_StaffNumber",
                table: "PayrollLineItems",
                column: "StaffNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_CreatedAt",
                table: "PayrollRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PayCalendarId",
                table: "PayrollRuns",
                column: "PayCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_Status",
                table: "PayrollRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayRates");

            migrationBuilder.DropTable(
                name: "PayrollLineItems");

            migrationBuilder.DropTable(
                name: "PayrollOptions");

            migrationBuilder.DropTable(
                name: "PayrollRuns");

            migrationBuilder.DropTable(
                name: "PayCalendars");
        }
    }
}
