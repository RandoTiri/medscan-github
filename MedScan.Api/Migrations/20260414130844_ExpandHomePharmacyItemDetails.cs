using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedScan.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpandHomePharmacyItemDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HomePharmacyItems_ProfileId_MedicationId",
                table: "HomePharmacyItems");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiresOn",
                table: "HomePharmacyItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageNumber",
                table: "HomePharmacyItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomePharmacyItems_ExpiresOn",
                table: "HomePharmacyItems",
                column: "ExpiresOn");

            migrationBuilder.CreateIndex(
                name: "IX_HomePharmacyItems_ProfileId",
                table: "HomePharmacyItems",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HomePharmacyItems_ExpiresOn",
                table: "HomePharmacyItems");

            migrationBuilder.DropIndex(
                name: "IX_HomePharmacyItems_ProfileId",
                table: "HomePharmacyItems");

            migrationBuilder.DropColumn(
                name: "ExpiresOn",
                table: "HomePharmacyItems");

            migrationBuilder.DropColumn(
                name: "PackageNumber",
                table: "HomePharmacyItems");

            migrationBuilder.CreateIndex(
                name: "IX_HomePharmacyItems_ProfileId_MedicationId",
                table: "HomePharmacyItems",
                columns: new[] { "ProfileId", "MedicationId" },
                unique: true);
        }
    }
}
