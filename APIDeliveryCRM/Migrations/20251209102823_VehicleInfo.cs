using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class VehicleInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_types_VehicleType_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_types_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicle_types",
                table: "Vehicle_types");

            migrationBuilder.RenameTable(
                name: "Vehicle_types",
                newName: "Vehicle_Types");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicle_Types",
                table: "Vehicle_Types",
                column: "ID_VehicleTypes");

            migrationBuilder.CreateTable(
                name: "Drive_Types",
                columns: table => new
                {
                    ID_DriveType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drive_Types", x => x.ID_DriveType);
                });

            migrationBuilder.CreateTable(
                name: "Fuel_Types",
                columns: table => new
                {
                    ID_FuelType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuel_Types", x => x.ID_FuelType);
                });

            migrationBuilder.CreateTable(
                name: "FuelCard_Statuses",
                columns: table => new
                {
                    ID_Status = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsCanBeUsed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCard_Statuses", x => x.ID_Status);
                });

            migrationBuilder.CreateTable(
                name: "FuelCard_Types",
                columns: table => new
                {
                    ID_Type = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCard_Types", x => x.ID_Type);
                });

            migrationBuilder.CreateTable(
                name: "FuelCompanies",
                columns: table => new
                {
                    ID_Company = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PhoneManager = table.Column<string>(type: "text", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCompanies", x => x.ID_Company);
                });

            migrationBuilder.CreateTable(
                name: "Transmission_Types",
                columns: table => new
                {
                    ID_TransmisType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transmission_Types", x => x.ID_TransmisType);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle_BodyTypes",
                columns: table => new
                {
                    ID_BodyType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_BodyTypes", x => x.ID_BodyType);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle_Brands",
                columns: table => new
                {
                    ID_Brand = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_Brands", x => x.ID_Brand);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle_Categories",
                columns: table => new
                {
                    ID_Category = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_Categories", x => x.ID_Category);
                });

            migrationBuilder.CreateTable(
                name: "FuelCards",
                columns: table => new
                {
                    ID_Card = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumberCard = table.Column<string>(type: "text", nullable: false),
                    Type_id = table.Column<int>(type: "integer", nullable: false),
                    Status_id = table.Column<int>(type: "integer", nullable: false),
                    FuelCompany_id = table.Column<int>(type: "integer", nullable: false),
                    PIN = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    MonthlyLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    Odometer = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCards", x => x.ID_Card);
                    table.ForeignKey(
                        name: "FK_FuelCards_FuelCard_Statuses_Status_id",
                        column: x => x.Status_id,
                        principalTable: "FuelCard_Statuses",
                        principalColumn: "ID_Status",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelCards_FuelCard_Types_Type_id",
                        column: x => x.Type_id,
                        principalTable: "FuelCard_Types",
                        principalColumn: "ID_Type",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelCards_FuelCompanies_FuelCompany_id",
                        column: x => x.FuelCompany_id,
                        principalTable: "FuelCompanies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle_Models",
                columns: table => new
                {
                    ID_Model = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Brand_id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<DateOnly>(type: "date", nullable: false),
                    AvgFuelCity = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgFuelHighWay = table.Column<decimal>(type: "numeric", nullable: false),
                    EngineCapacity = table.Column<decimal>(type: "numeric", nullable: false),
                    HorsePower = table.Column<int>(type: "integer", nullable: false),
                    TransmissionType_id = table.Column<int>(type: "integer", nullable: false),
                    DriveType_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_Models", x => x.ID_Model);
                    table.ForeignKey(
                        name: "FK_Vehicle_Models_Drive_Types_DriveType_id",
                        column: x => x.DriveType_id,
                        principalTable: "Drive_Types",
                        principalColumn: "ID_DriveType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicle_Models_Transmission_Types_TransmissionType_id",
                        column: x => x.TransmissionType_id,
                        principalTable: "Transmission_Types",
                        principalColumn: "ID_TransmisType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicle_Models_Vehicle_Brands_Brand_id",
                        column: x => x.Brand_id,
                        principalTable: "Vehicle_Brands",
                        principalColumn: "ID_Brand",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Courier_FuelCards",
                columns: table => new
                {
                    ID_CF = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    FuelCard_id = table.Column<int>(type: "integer", nullable: false),
                    Is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    Is_backup = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedByUser_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courier_FuelCards", x => x.ID_CF);
                    table.ForeignKey(
                        name: "FK_Courier_FuelCards_CourierProfiles_Courier_id",
                        column: x => x.Courier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courier_FuelCards_FuelCards_FuelCard_id",
                        column: x => x.FuelCard_id,
                        principalTable: "FuelCards",
                        principalColumn: "ID_Card",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courier_FuelCards_Users_AssignedByUser_id",
                        column: x => x.AssignedByUser_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    ID_Vehicle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    License_plate = table.Column<string>(type: "text", nullable: false),
                    VIN = table.Column<string>(type: "text", nullable: false),
                    Category_id = table.Column<int>(type: "integer", nullable: false),
                    Model_id = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<DateOnly>(type: "date", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    BodyType_id = table.Column<int>(type: "integer", nullable: false),
                    Cargo_volume = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_cargo_weight = table.Column<decimal>(type: "numeric", nullable: false),
                    FuelType_id = table.Column<int>(type: "integer", nullable: false),
                    FuelTank_Capacity = table.Column<decimal>(type: "numeric", nullable: false),
                    Current_mileage = table.Column<decimal>(type: "numeric", nullable: false),
                    Insurance_policy = table.Column<string>(type: "text", nullable: false),
                    CurrentCourier_id = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.ID_Vehicle);
                    table.ForeignKey(
                        name: "FK_Vehicles_CourierProfiles_CurrentCourier_id",
                        column: x => x.CurrentCourier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_Fuel_Types_FuelType_id",
                        column: x => x.FuelType_id,
                        principalTable: "Fuel_Types",
                        principalColumn: "ID_FuelType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_Vehicle_BodyTypes_BodyType_id",
                        column: x => x.BodyType_id,
                        principalTable: "Vehicle_BodyTypes",
                        principalColumn: "ID_BodyType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_Vehicle_Categories_Category_id",
                        column: x => x.Category_id,
                        principalTable: "Vehicle_Categories",
                        principalColumn: "ID_Category",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_Vehicle_Models_Model_id",
                        column: x => x.Model_id,
                        principalTable: "Vehicle_Models",
                        principalColumn: "ID_Model",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle_Assignments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Vehicle_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    Start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    End_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Mileage_start = table.Column<int>(type: "integer", nullable: false),
                    Mileage_end = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_Assignments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Vehicle_Assignments_CourierProfiles_Courier_id",
                        column: x => x.Courier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicle_Assignments_Vehicles_Vehicle_id",
                        column: x => x.Vehicle_id,
                        principalTable: "Vehicles",
                        principalColumn: "ID_Vehicle",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courier_FuelCards_AssignedByUser_id",
                table: "Courier_FuelCards",
                column: "AssignedByUser_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_FuelCards_Courier_id",
                table: "Courier_FuelCards",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_FuelCards_FuelCard_id",
                table: "Courier_FuelCards",
                column: "FuelCard_id");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_FuelCompany_id",
                table: "FuelCards",
                column: "FuelCompany_id");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_Status_id",
                table: "FuelCards",
                column: "Status_id");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_Type_id",
                table: "FuelCards",
                column: "Type_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_Assignments_Courier_id",
                table: "Vehicle_Assignments",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_Assignments_Vehicle_id",
                table: "Vehicle_Assignments",
                column: "Vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_Models_Brand_id",
                table: "Vehicle_Models",
                column: "Brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_Models_DriveType_id",
                table: "Vehicle_Models",
                column: "DriveType_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_Models_TransmissionType_id",
                table: "Vehicle_Models",
                column: "TransmissionType_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_BodyType_id",
                table: "Vehicles",
                column: "BodyType_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Category_id",
                table: "Vehicles",
                column: "Category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CurrentCourier_id",
                table: "Vehicles",
                column: "CurrentCourier_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_FuelType_id",
                table: "Vehicles",
                column: "FuelType_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Model_id",
                table: "Vehicles",
                column: "Model_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Types_VehicleType_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Types_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles");

            migrationBuilder.DropTable(
                name: "Courier_FuelCards");

            migrationBuilder.DropTable(
                name: "Vehicle_Assignments");

            migrationBuilder.DropTable(
                name: "FuelCards");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "FuelCard_Statuses");

            migrationBuilder.DropTable(
                name: "FuelCard_Types");

            migrationBuilder.DropTable(
                name: "FuelCompanies");

            migrationBuilder.DropTable(
                name: "Fuel_Types");

            migrationBuilder.DropTable(
                name: "Vehicle_BodyTypes");

            migrationBuilder.DropTable(
                name: "Vehicle_Categories");

            migrationBuilder.DropTable(
                name: "Vehicle_Models");

            migrationBuilder.DropTable(
                name: "Drive_Types");

            migrationBuilder.DropTable(
                name: "Transmission_Types");

            migrationBuilder.DropTable(
                name: "Vehicle_Brands");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicle_Types",
                table: "Vehicle_Types");

            migrationBuilder.RenameTable(
                name: "Vehicle_Types",
                newName: "Vehicle_types");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicle_types",
                table: "Vehicle_types",
                column: "ID_VehicleTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_types_VehicleType_id",
                table: "CourierProfiles",
                column: "VehicleType_id",
                principalTable: "Vehicle_types",
                principalColumn: "ID_VehicleTypes",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_types_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles",
                column: "Vehicle_typesID_VehicleTypes",
                principalTable: "Vehicle_types",
                principalColumn: "ID_VehicleTypes");
        }
    }
}
