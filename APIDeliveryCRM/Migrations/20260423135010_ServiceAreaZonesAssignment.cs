using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class ServiceAreaZonesAssignment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceAreaZones",
                columns: table => new
                {
                    ID_ServiceAreaZone = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Center_lat = table.Column<decimal>(type: "numeric", nullable: false),
                    Center_lon = table.Column<decimal>(type: "numeric", nullable: false),
                    Radius_km = table.Column<decimal>(type: "numeric", nullable: false),
                    Is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAreaZones", x => x.ID_ServiceAreaZone);
                    table.ForeignKey(
                        name: "FK_ServiceAreaZones_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceAreaZoneCouriers",
                columns: table => new
                {
                    ID_ServiceAreaZoneCourier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceAreaZone_id = table.Column<int>(type: "integer", nullable: false),
                    CourierProfile_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAreaZoneCouriers", x => x.ID_ServiceAreaZoneCourier);
                    table.ForeignKey(
                        name: "FK_ServiceAreaZoneCouriers_CourierProfiles_CourierProfile_id",
                        column: x => x.CourierProfile_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceAreaZoneCouriers_ServiceAreaZones_ServiceAreaZone_id",
                        column: x => x.ServiceAreaZone_id,
                        principalTable: "ServiceAreaZones",
                        principalColumn: "ID_ServiceAreaZone",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreaZoneCouriers_CourierProfile_id",
                table: "ServiceAreaZoneCouriers",
                column: "CourierProfile_id");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreaZoneCouriers_ServiceAreaZone_id",
                table: "ServiceAreaZoneCouriers",
                column: "ServiceAreaZone_id");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreaZones_Company_id",
                table: "ServiceAreaZones",
                column: "Company_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceAreaZoneCouriers");

            migrationBuilder.DropTable(
                name: "ServiceAreaZones");
        }
    }
}
