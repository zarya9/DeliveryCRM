using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class FixedProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ManagerProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Passport_issued_by",
                table: "ManagerProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Passport_issued_date",
                table: "ManagerProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Passport_number",
                table: "ManagerProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Passport_series",
                table: "ManagerProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ManagerProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "ManagerProfiles");

            migrationBuilder.DropColumn(
                name: "Passport_issued_by",
                table: "ManagerProfiles");

            migrationBuilder.DropColumn(
                name: "Passport_issued_date",
                table: "ManagerProfiles");

            migrationBuilder.DropColumn(
                name: "Passport_number",
                table: "ManagerProfiles");

            migrationBuilder.DropColumn(
                name: "Passport_series",
                table: "ManagerProfiles");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ManagerProfiles");
        }
    }
}
