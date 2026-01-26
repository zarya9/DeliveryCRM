using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToRemainingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Vehicle_Assignments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Shift_Assignments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Courier_FuelCards",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_Assignments_Company_id",
                table: "Vehicle_Assignments",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_Assignments_Company_id",
                table: "Shift_Assignments",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_FuelCards_Company_id",
                table: "Courier_FuelCards",
                column: "Company_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courier_FuelCards_Companies_Company_id",
                table: "Courier_FuelCards",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shift_Assignments_Companies_Company_id",
                table: "Shift_Assignments",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicle_Assignments_Companies_Company_id",
                table: "Vehicle_Assignments",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courier_FuelCards_Companies_Company_id",
                table: "Courier_FuelCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Shift_Assignments_Companies_Company_id",
                table: "Shift_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicle_Assignments_Companies_Company_id",
                table: "Vehicle_Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Vehicle_Assignments_Company_id",
                table: "Vehicle_Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Shift_Assignments_Company_id",
                table: "Shift_Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Courier_FuelCards_Company_id",
                table: "Courier_FuelCards");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Vehicle_Assignments");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Shift_Assignments");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Courier_FuelCards");
        }
    }
}
