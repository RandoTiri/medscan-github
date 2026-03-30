using System;
using MedScan.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedScan.Api.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260324143000_LinkProfilesToIdentityUsers")]
    public partial class LinkProfilesToIdentityUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_AppUser_UserId",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Profiles",
                newName: "LegacyUserId");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Profiles" p
                SET "UserId" = u."Id"
                FROM "AppUser" au
                JOIN "AspNetUsers" u ON LOWER(au."Email") = LOWER(u."Email")
                WHERE p."LegacyUserId" = au."Id";
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Profiles" WHERE "UserId" IS NULL) THEN
                        RAISE EXCEPTION 'Profile ownership migration failed: one or more Profiles could not be mapped to AspNetUsers by email.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Profiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_AspNetUsers_UserId",
                table: "Profiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "LegacyUserId",
                table: "Profiles");

            migrationBuilder.DropTable(
                name: "AppUser");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Down migration is not supported because this migration consolidates users into AspNetUsers and removes AppUser.");
        }
    }
}
