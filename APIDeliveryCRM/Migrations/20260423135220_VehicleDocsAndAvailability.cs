using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class VehicleDocsAndAvailability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Insurance_expires_at",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Is_available",
                table: "Vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "Maintenance_due_at",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Registration_expires_at",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Insurance_expires_at",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Is_available",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Maintenance_due_at",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Registration_expires_at",
                table: "Vehicles");
        }
    }
}
