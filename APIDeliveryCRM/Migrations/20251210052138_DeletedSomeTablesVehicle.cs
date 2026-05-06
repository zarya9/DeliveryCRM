using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class DeletedSomeTablesVehicle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Types_VehicleType_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Types_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles");

            migrationBuilder.DropTable(
                name: "Vehicle_Types");

            migrationBuilder.RenameColumn(
                name: "Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles",
                newName: "Vehicle_categoriesID_Category");

            migrationBuilder.RenameColumn(
                name: "VehicleType_id",
                table: "CourierProfiles",
                newName: "VehicleCategory_id");

            migrationBuilder.RenameIndex(
                name: "IX_CourierProfiles_VehicleType_id",
                table: "CourierProfiles",
                newName: "IX_CourierProfiles_VehicleCategory_id");

            migrationBuilder.RenameIndex(
                name: "IX_CourierProfiles_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles",
                newName: "IX_CourierProfiles_Vehicle_categoriesID_Category");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Vehicle_Categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Vehicle_Categories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Max_Weight",
                table: "Vehicle_Categories",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Speed_factor",
                table: "Vehicle_Categories",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_Categories_VehicleCategory_id",
                table: "CourierProfiles",
                column: "VehicleCategory_id",
                principalTable: "Vehicle_Categories",
                principalColumn: "ID_Category",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_Categories_Vehicle_categoriesID_Cat~",
                table: "CourierProfiles",
                column: "Vehicle_categoriesID_Category",
                principalTable: "Vehicle_Categories",
                principalColumn: "ID_Category");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Categories_VehicleCategory_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Categories_Vehicle_categoriesID_Cat~",
                table: "CourierProfiles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Vehicle_Categories");

            migrationBuilder.DropColumn(
                name: "Max_Weight",
                table: "Vehicle_Categories");

            migrationBuilder.DropColumn(
                name: "Speed_factor",
                table: "Vehicle_Categories");

            migrationBuilder.RenameColumn(
                name: "Vehicle_categoriesID_Category",
                table: "CourierProfiles",
                newName: "Vehicle_typesID_VehicleTypes");

            migrationBuilder.RenameColumn(
                name: "VehicleCategory_id",
                table: "CourierProfiles",
                newName: "VehicleType_id");

            migrationBuilder.RenameIndex(
                name: "IX_CourierProfiles_VehicleCategory_id",
                table: "CourierProfiles",
                newName: "IX_CourierProfiles_VehicleType_id");

            migrationBuilder.RenameIndex(
                name: "IX_CourierProfiles_Vehicle_categoriesID_Category",
                table: "CourierProfiles",
                newName: "IX_CourierProfiles_Vehicle_typesID_VehicleTypes");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Vehicle_Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "Vehicle_Types",
                columns: table => new
                {
                    ID_VehicleTypes = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Max_Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Speed_factor = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_Types", x => x.ID_VehicleTypes);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_Types_VehicleType_id",
                table: "CourierProfiles",
                column: "VehicleType_id",
                principalTable: "Vehicle_Types",
                principalColumn: "ID_VehicleTypes",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_Types_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles",
                column: "Vehicle_typesID_VehicleTypes",
                principalTable: "Vehicle_Types",
                principalColumn: "ID_VehicleTypes");
        }
    }
}
