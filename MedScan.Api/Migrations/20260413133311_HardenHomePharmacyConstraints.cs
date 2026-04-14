using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedScan.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardenHomePharmacyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HomePharmacyItems_ProfileId_MedicationId",
                table: "HomePharmacyItems");

            migrationBuilder.CreateIndex(
                name: "IX_HomePharmacyItems_ProfileId_MedicationId",
                table: "HomePharmacyItems",
                columns: new[] { "ProfileId", "MedicationId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_HomePharmacyItems_Quantity_Positive",
                table: "HomePharmacyItems",
                sql: "\"Quantity\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HomePharmacyItems_ProfileId_MedicationId",
                table: "HomePharmacyItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HomePharmacyItems_Quantity_Positive",
                table: "HomePharmacyItems");

            migrationBuilder.CreateIndex(
                name: "IX_HomePharmacyItems_ProfileId_MedicationId",
                table: "HomePharmacyItems",
                columns: new[] { "ProfileId", "MedicationId" });
        }
    }
}
