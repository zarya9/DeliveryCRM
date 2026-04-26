using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class OrderDispatchSlaTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Arrived_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Delay_reason",
                table: "Orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Eta_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "In_transit_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Pickup_started_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Priority",
                table: "Orders",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Sla_breached_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Sla_due_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderTimelineEvents",
                columns: table => new
                {
                    ID_OrderTimelineEvent = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    OldStatus_id = table.Column<int>(type: "integer", nullable: true),
                    NewStatus_id = table.Column<int>(type: "integer", nullable: true),
                    OldCourier_id = table.Column<int>(type: "integer", nullable: true),
                    NewCourier_id = table.Column<int>(type: "integer", nullable: true),
                    ActorUser_id = table.Column<int>(type: "integer", nullable: true),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTimelineEvents", x => x.ID_OrderTimelineEvent);
                    table.ForeignKey(
                        name: "FK_OrderTimelineEvents_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTimelineEvents_Order_id",
                table: "OrderTimelineEvents",
                column: "Order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderTimelineEvents");

            migrationBuilder.DropColumn(
                name: "Arrived_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Delay_reason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Eta_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "In_transit_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Pickup_started_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Sla_breached_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Sla_due_at",
                table: "Orders");
        }
    }
}
