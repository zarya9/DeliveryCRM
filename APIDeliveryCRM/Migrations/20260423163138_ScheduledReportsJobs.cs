using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledReportsJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledReportJobs",
                columns: table => new
                {
                    ID_ScheduledReportJob = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    ReportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TimeUtc = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    Is_active = table.Column<bool>(type: "boolean", nullable: false),
                    LastRun_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRun_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReportJobs", x => x.ID_ScheduledReportJob);
                    table.ForeignKey(
                        name: "FK_ScheduledReportJobs_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReportJobs_Company_id",
                table: "ScheduledReportJobs",
                column: "Company_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledReportJobs");
        }
    }
}
