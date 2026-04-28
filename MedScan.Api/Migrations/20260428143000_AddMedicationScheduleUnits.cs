using MedScan.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MedScan.Shared.Models.Enums;

#nullable disable

namespace MedScan.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428143000_AddMedicationScheduleUnits")]
    public partial class AddMedicationScheduleUnits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScheduleUnit",
                table: "UserMedications",
                type: "integer",
                nullable: false,
                defaultValue: (int)MedicationScheduleUnit.Day);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "UserMedications",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");

            migrationBuilder.AddColumn<string>(
                name: "WeeklyDaysJson",
                table: "UserMedications",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                UPDATE "UserMedications"
                SET "StartDate" = CAST("AddedAt" AT TIME ZONE 'UTC' AS date)
                WHERE "StartDate" IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduleUnit",
                table: "UserMedications");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "UserMedications");

            migrationBuilder.DropColumn(
                name: "WeeklyDaysJson",
                table: "UserMedications");
        }
    }
}
