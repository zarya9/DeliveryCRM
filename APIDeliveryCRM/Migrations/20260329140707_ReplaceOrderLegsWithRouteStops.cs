using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class ReplaceOrderLegsWithRouteStops : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderRouteLegs");

            migrationBuilder.CreateTable(
                name: "OrderRouteStops",
                columns: table => new
                {
                    ID_OrderRouteStop = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address_id = table.Column<int>(type: "integer", nullable: false),
                    LogisticsHub_id = table.Column<int>(type: "integer", nullable: true),
                    AssignedCourier_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRouteStops", x => x.ID_OrderRouteStop);
                    table.ForeignKey(
                        name: "FK_OrderRouteStops_Addresses_Address_id",
                        column: x => x.Address_id,
                        principalTable: "Addresses",
                        principalColumn: "ID_Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderRouteStops_CourierProfiles_AssignedCourier_id",
                        column: x => x.AssignedCourier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderRouteStops_LogisticsHubs_LogisticsHub_id",
                        column: x => x.LogisticsHub_id,
                        principalTable: "LogisticsHubs",
                        principalColumn: "ID_LogisticsHub",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderRouteStops_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteStops_Address_id",
                table: "OrderRouteStops",
                column: "Address_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteStops_AssignedCourier_id",
                table: "OrderRouteStops",
                column: "AssignedCourier_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteStops_LogisticsHub_id",
                table: "OrderRouteStops",
                column: "LogisticsHub_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteStops_Order_id",
                table: "OrderRouteStops",
                column: "Order_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderRouteStops");

            migrationBuilder.CreateTable(
                name: "OrderRouteLegs",
                columns: table => new
                {
                    ID_OrderRouteLeg = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignedCourier_id = table.Column<int>(type: "integer", nullable: true),
                    FromAddress_id = table.Column<int>(type: "integer", nullable: true),
                    LogisticsHub_id = table.Column<int>(type: "integer", nullable: true),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    ToAddress_id = table.Column<int>(type: "integer", nullable: true),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRouteLegs", x => x.ID_OrderRouteLeg);
                    table.ForeignKey(
                        name: "FK_OrderRouteLegs_Addresses_FromAddress_id",
                        column: x => x.FromAddress_id,
                        principalTable: "Addresses",
                        principalColumn: "ID_Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderRouteLegs_Addresses_ToAddress_id",
                        column: x => x.ToAddress_id,
                        principalTable: "Addresses",
                        principalColumn: "ID_Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderRouteLegs_CourierProfiles_AssignedCourier_id",
                        column: x => x.AssignedCourier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderRouteLegs_LogisticsHubs_LogisticsHub_id",
                        column: x => x.LogisticsHub_id,
                        principalTable: "LogisticsHubs",
                        principalColumn: "ID_LogisticsHub",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderRouteLegs_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteLegs_AssignedCourier_id",
                table: "OrderRouteLegs",
                column: "AssignedCourier_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteLegs_FromAddress_id",
                table: "OrderRouteLegs",
                column: "FromAddress_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteLegs_LogisticsHub_id",
                table: "OrderRouteLegs",
                column: "LogisticsHub_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteLegs_Order_id",
                table: "OrderRouteLegs",
                column: "Order_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteLegs_ToAddress_id",
                table: "OrderRouteLegs",
                column: "ToAddress_id");
        }
    }
}
