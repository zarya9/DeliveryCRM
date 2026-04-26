using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class OrderLogisticsRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "DeliveryRouteKind",
                table: "Orders",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<int>(
                name: "DestinationHub_id",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginHub_id",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LogisticsHubs",
                columns: table => new
                {
                    ID_LogisticsHub = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogisticsHubs", x => x.ID_LogisticsHub);
                    table.ForeignKey(
                        name: "FK_LogisticsHubs_Addresses_Address_id",
                        column: x => x.Address_id,
                        principalTable: "Addresses",
                        principalColumn: "ID_Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogisticsHubs_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderRouteLegs",
                columns: table => new
                {
                    ID_OrderRouteLeg = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FromAddress_id = table.Column<int>(type: "integer", nullable: true),
                    ToAddress_id = table.Column<int>(type: "integer", nullable: true),
                    LogisticsHub_id = table.Column<int>(type: "integer", nullable: true),
                    AssignedCourier_id = table.Column<int>(type: "integer", nullable: true)
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
                name: "IX_Orders_DestinationHub_id",
                table: "Orders",
                column: "DestinationHub_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OriginHub_id",
                table: "Orders",
                column: "OriginHub_id");

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsHubs_Address_id",
                table: "LogisticsHubs",
                column: "Address_id");

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsHubs_Company_id",
                table: "LogisticsHubs",
                column: "Company_id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_LogisticsHubs_DestinationHub_id",
                table: "Orders",
                column: "DestinationHub_id",
                principalTable: "LogisticsHubs",
                principalColumn: "ID_LogisticsHub",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_LogisticsHubs_OriginHub_id",
                table: "Orders",
                column: "OriginHub_id",
                principalTable: "LogisticsHubs",
                principalColumn: "ID_LogisticsHub",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_LogisticsHubs_DestinationHub_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_LogisticsHubs_OriginHub_id",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderRouteLegs");

            migrationBuilder.DropTable(
                name: "LogisticsHubs");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DestinationHub_id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OriginHub_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryRouteKind",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DestinationHub_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OriginHub_id",
                table: "Orders");
        }
    }
}
