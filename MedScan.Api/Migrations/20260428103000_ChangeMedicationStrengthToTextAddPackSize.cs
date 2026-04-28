using MedScan.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedScan.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428103000_ChangeMedicationStrengthToTextAddPackSize")]
    public partial class ChangeMedicationStrengthToTextAddPackSize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StrengthMg",
                table: "Medications",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackSize",
                table: "Medications",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "Medications");

            migrationBuilder.AlterColumn<int>(
                name: "StrengthMg",
                table: "Medications",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
