using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class Leads2AnalyticsTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Lost_at",
                table: "Leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lost_reason",
                table: "Leads",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextTask_due_at",
                table: "Leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextTask_title",
                table: "Leads",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Won_at",
                table: "Leads",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lost_at",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Lost_reason",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "NextTask_due_at",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "NextTask_title",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Won_at",
                table: "Leads");
        }
    }
}
