using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class SomeFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Categories_Vehicle_categoriesID_Cat~",
                table: "CourierProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CourierProfiles_Vehicle_categoriesID_Category",
                table: "CourierProfiles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Vehicle_categoriesID_Category",
                table: "CourierProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Vehicles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Vehicles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Vehicle_categoriesID_Category",
                table: "CourierProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourierProfiles_Vehicle_categoriesID_Category",
                table: "CourierProfiles",
                column: "Vehicle_categoriesID_Category");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_Categories_Vehicle_categoriesID_Cat~",
                table: "CourierProfiles",
                column: "Vehicle_categoriesID_Category",
                principalTable: "Vehicle_Categories",
                principalColumn: "ID_Category");
        }
    }
}
