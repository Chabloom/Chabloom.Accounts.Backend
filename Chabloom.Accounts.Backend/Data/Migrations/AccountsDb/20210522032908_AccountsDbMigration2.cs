using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Chabloom.Accounts.Backend.Data.Migrations.AccountsDb
{
    public partial class AccountsDbMigration2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUserClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "UserId" },
                values: new object[] { 2, "role", "Chabloom.Global.Admin", new Guid("421bde72-5f81-451b-83b6-08d8d3b98c06") });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserClaims",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
