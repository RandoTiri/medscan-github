using MedScan.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedScan.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260423123000_AddHomePharmacyBatchNumber")]
    public partial class AddHomePharmacyBatchNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "HomePharmacyItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "HomePharmacyItems");
        }
    }
}
