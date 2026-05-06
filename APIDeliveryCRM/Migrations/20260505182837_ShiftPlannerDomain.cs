using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class ShiftPlannerDomain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ShiftAssignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderRouteStop_id",
                table: "ShiftAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Planned_distance_km",
                table: "ShiftAssignments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "Planned_end_utc",
                table: "ShiftAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Planned_start_utc",
                table: "ShiftAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShiftPlan_id",
                table: "ShiftAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Stage",
                table: "ShiftAssignments",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "ShiftAssignments",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "HandoffStage",
                table: "Orders",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Plan_locked_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Plan_locked_shiftPlan_id",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShiftPlans",
                columns: table => new
                {
                    ID_ShiftPlan = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Shift_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    Vehicle_id = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Planned_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Planned_end_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Total_distance_km = table.Column<decimal>(type: "numeric", nullable: false),
                    Estimated_duration_minutes = table.Column<decimal>(type: "numeric", nullable: false),
                    Peak_weight_kg = table.Column<decimal>(type: "numeric", nullable: false),
                    Peak_volume_m3 = table.Column<decimal>(type: "numeric", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Last_recompute_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftPlans", x => x.ID_ShiftPlan);
                    table.ForeignKey(
                        name: "FK_ShiftPlans_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftPlans_CourierProfiles_Courier_id",
                        column: x => x.Courier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftPlans_CourierShifts_Shift_id",
                        column: x => x.Shift_id,
                        principalTable: "CourierShifts",
                        principalColumn: "ID_Shift",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftPlans_Vehicles_Vehicle_id",
                        column: x => x.Vehicle_id,
                        principalTable: "Vehicles",
                        principalColumn: "ID_Vehicle",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_RouteStop_Status",
                table: "ShiftAssignments",
                columns: new[] { "OrderRouteStop_id", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_ShiftPlan_id",
                table: "ShiftAssignments",
                column: "ShiftPlan_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Plan_locked_shiftPlan_id",
                table: "Orders",
                column: "Plan_locked_shiftPlan_id");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftPlans_Company_id_Status",
                table: "ShiftPlans",
                columns: new[] { "Company_id", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftPlans_Courier_id",
                table: "ShiftPlans",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftPlans_Shift_id",
                table: "ShiftPlans",
                column: "Shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftPlans_Vehicle_id",
                table: "ShiftPlans",
                column: "Vehicle_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShiftPlans_Plan_locked_shiftPlan_id",
                table: "Orders",
                column: "Plan_locked_shiftPlan_id",
                principalTable: "ShiftPlans",
                principalColumn: "ID_ShiftPlan",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftAssignments_OrderRouteStops_OrderRouteStop_id",
                table: "ShiftAssignments",
                column: "OrderRouteStop_id",
                principalTable: "OrderRouteStops",
                principalColumn: "ID_OrderRouteStop",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftAssignments_ShiftPlans_ShiftPlan_id",
                table: "ShiftAssignments",
                column: "ShiftPlan_id",
                principalTable: "ShiftPlans",
                principalColumn: "ID_ShiftPlan",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShiftPlans_Plan_locked_shiftPlan_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftAssignments_OrderRouteStops_OrderRouteStop_id",
                table: "ShiftAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftAssignments_ShiftPlans_ShiftPlan_id",
                table: "ShiftAssignments");

            migrationBuilder.DropTable(
                name: "ShiftPlans");

            migrationBuilder.DropIndex(
                name: "IX_ShiftAssignments_RouteStop_Status",
                table: "ShiftAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ShiftAssignments_ShiftPlan_id",
                table: "ShiftAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Plan_locked_shiftPlan_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "OrderRouteStop_id",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "Planned_distance_km",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "Planned_end_utc",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "Planned_start_utc",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "ShiftPlan_id",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "HandoffStage",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Plan_locked_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Plan_locked_shiftPlan_id",
                table: "Orders");
        }
    }
}
