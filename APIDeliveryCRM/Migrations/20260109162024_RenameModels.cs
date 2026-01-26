using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class RenameModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientProfiles_Payment_Methods_Preferred_payment_method_id",
                table: "ClientProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Courier_Statuses_CurrentStatus_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Schedule_Types_WorkSchedule_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Categories_VehicleCategory_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelCards_FuelCard_Statuses_Status_id",
                table: "FuelCards");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelCards_FuelCard_Types_Type_id",
                table: "FuelCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Notification_Types_Type_id",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Order_Statuses_Order_StatusesID_OrderStatus",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Order_Statuses_Status_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Order_Types_OrderType_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Package_Types_PackageType_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Payment_Methods_PaymentMethod_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Fuel_Types_FuelType_id",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Vehicle_BodyTypes_BodyType_id",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Vehicle_Categories_Category_id",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Vehicle_Models_Model_id",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "Courier_FuelCards");

            migrationBuilder.DropTable(
                name: "Courier_Statuses");

            migrationBuilder.DropTable(
                name: "Fuel_Types");

            migrationBuilder.DropTable(
                name: "FuelCard_Statuses");

            migrationBuilder.DropTable(
                name: "FuelCard_Types");

            migrationBuilder.DropTable(
                name: "Notification_Types");

            migrationBuilder.DropTable(
                name: "Order_Statuses");

            migrationBuilder.DropTable(
                name: "Order_Types");

            migrationBuilder.DropTable(
                name: "Package_Types");

            // Сохраняем данные из Payment_Methods перед удалением (если таблица существует)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Payment_Methods') THEN
                        CREATE TABLE IF NOT EXISTS ""PaymentMethods_Temp"" AS 
                        SELECT * FROM ""Payment_Methods"";
                    END IF;
                END $$;
            ");

            migrationBuilder.DropTable(
                name: "Payment_Methods");

            migrationBuilder.DropTable(
                name: "Schedule_Types");

            migrationBuilder.DropTable(
                name: "Shift_Assignments");

            migrationBuilder.DropTable(
                name: "Vehicle_Assignments");

            migrationBuilder.DropTable(
                name: "Vehicle_BodyTypes");

            migrationBuilder.DropTable(
                name: "Vehicle_Categories");

            migrationBuilder.DropTable(
                name: "Vehicle_Models");

            migrationBuilder.DropTable(
                name: "Courier_Shifts");

            migrationBuilder.DropTable(
                name: "Drive_Types");

            migrationBuilder.DropTable(
                name: "Transmission_Types");

            migrationBuilder.DropTable(
                name: "Vehicle_Brands");

            migrationBuilder.DropTable(
                name: "Shift_Statuses");

            migrationBuilder.RenameColumn(
                name: "Order_StatusesID_OrderStatus",
                table: "Orders",
                newName: "OrderStatusID_OrderStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_Order_StatusesID_OrderStatus",
                table: "Orders",
                newName: "IX_Orders_OrderStatusID_OrderStatus");

            migrationBuilder.CreateTable(
                name: "CourierFuelCards",
                columns: table => new
                {
                    ID_CF = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    FuelCard_id = table.Column<int>(type: "integer", nullable: false),
                    Is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    Is_backup = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedByUser_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierFuelCards", x => x.ID_CF);
                    table.ForeignKey(
                        name: "FK_CourierFuelCards_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierFuelCards_CourierProfiles_Courier_id",
                        column: x => x.Courier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierFuelCards_FuelCards_FuelCard_id",
                        column: x => x.FuelCard_id,
                        principalTable: "FuelCards",
                        principalColumn: "ID_Card",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierFuelCards_Users_AssignedByUser_id",
                        column: x => x.AssignedByUser_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierStatuses",
                columns: table => new
                {
                    ID_CourierStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierStatuses", x => x.ID_CourierStatus);
                });

            migrationBuilder.CreateTable(
                name: "DriveTypes",
                columns: table => new
                {
                    ID_DriveType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriveTypes", x => x.ID_DriveType);
                });

            migrationBuilder.CreateTable(
                name: "FuelCardStatuses",
                columns: table => new
                {
                    ID_Status = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsCanBeUsed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCardStatuses", x => x.ID_Status);
                });

            migrationBuilder.CreateTable(
                name: "FuelCardTypes",
                columns: table => new
                {
                    ID_Type = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCardTypes", x => x.ID_Type);
                });

            migrationBuilder.CreateTable(
                name: "FuelTypes",
                columns: table => new
                {
                    ID_FuelType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelTypes", x => x.ID_FuelType);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTypes",
                columns: table => new
                {
                    ID_NotificationType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTypes", x => x.ID_NotificationType);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatuses",
                columns: table => new
                {
                    ID_OrderStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatuses", x => x.ID_OrderStatus);
                });

            migrationBuilder.CreateTable(
                name: "OrderTypes",
                columns: table => new
                {
                    ID_OrderType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Base_price = table.Column<decimal>(type: "numeric", nullable: false),
                    Price_km = table.Column<decimal>(type: "numeric", nullable: false),
                    Estimated_delivery_factor = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTypes", x => x.ID_OrderType);
                });

            migrationBuilder.CreateTable(
                name: "PackageTypes",
                columns: table => new
                {
                    ID_PackageType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Max_weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_height = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_wight = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_length = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageTypes", x => x.ID_PackageType);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    ID_PaymentMethod = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.ID_PaymentMethod);
                });

            // Восстанавливаем данные из временной таблицы (если она существует)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'PaymentMethods_Temp') THEN
                        INSERT INTO ""PaymentMethods"" (""ID_PaymentMethod"", ""Name"")
                        SELECT ""ID_PaymentMethod"", ""Name"" FROM ""PaymentMethods_Temp"";
                        DROP TABLE ""PaymentMethods_Temp"";
                    END IF;
                    
                    -- Если таблица пуста, создаем дефолтную запись
                    IF NOT EXISTS (SELECT 1 FROM ""PaymentMethods"") THEN
                        INSERT INTO ""PaymentMethods"" (""Name"") VALUES ('Наличные');
                    END IF;
                END $$;
            ");

            migrationBuilder.CreateTable(
                name: "ScheduleTypes",
                columns: table => new
                {
                    ID_SheduleType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTypes", x => x.ID_SheduleType);
                });

            migrationBuilder.CreateTable(
                name: "ShiftStatuses",
                columns: table => new
                {
                    ID_ShiftStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftStatuses", x => x.ID_ShiftStatus);
                });

            migrationBuilder.CreateTable(
                name: "TransmissionTypes",
                columns: table => new
                {
                    ID_TransmisType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransmissionTypes", x => x.ID_TransmisType);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Vehicle_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    Start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    End_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Mileage_start = table.Column<int>(type: "integer", nullable: false),
                    Mileage_end = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAssignments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_CourierProfiles_Courier_id",
                        column: x => x.Courier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Vehicles_Vehicle_id",
                        column: x => x.Vehicle_id,
                        principalTable: "Vehicles",
                        principalColumn: "ID_Vehicle",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleBodyTypes",
                columns: table => new
                {
                    ID_BodyType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleBodyTypes", x => x.ID_BodyType);
                });

            migrationBuilder.CreateTable(
                name: "VehicleBrands",
                columns: table => new
                {
                    ID_Brand = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleBrands", x => x.ID_Brand);
                });

            migrationBuilder.CreateTable(
                name: "VehicleCategories",
                columns: table => new
                {
                    ID_Category = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Max_Weight = table.Column<decimal>(type: "numeric", nullable: true),
                    Speed_factor = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCategories", x => x.ID_Category);
                });

            migrationBuilder.CreateTable(
                name: "CourierShifts",
                columns: table => new
                {
                    ID_Shift = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShiftStatus_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierShifts", x => x.ID_Shift);
                    table.ForeignKey(
                        name: "FK_CourierShifts_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierShifts_CourierProfiles_Courier_id",
                        column: x => x.Courier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierShifts_ShiftStatuses_ShiftStatus_id",
                        column: x => x.ShiftStatus_id,
                        principalTable: "ShiftStatuses",
                        principalColumn: "ID_ShiftStatus",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModels",
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
                    table.PrimaryKey("PK_VehicleModels", x => x.ID_Model);
                    table.ForeignKey(
                        name: "FK_VehicleModels_DriveTypes_DriveType_id",
                        column: x => x.DriveType_id,
                        principalTable: "DriveTypes",
                        principalColumn: "ID_DriveType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleModels_TransmissionTypes_TransmissionType_id",
                        column: x => x.TransmissionType_id,
                        principalTable: "TransmissionTypes",
                        principalColumn: "ID_TransmisType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleModels_VehicleBrands_Brand_id",
                        column: x => x.Brand_id,
                        principalTable: "VehicleBrands",
                        principalColumn: "ID_Brand",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftAssignments",
                columns: table => new
                {
                    ID_ShiftAssignment = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Shift_id = table.Column<int>(type: "integer", nullable: false),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    Assignment_sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftAssignments", x => x.ID_ShiftAssignment);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_CourierShifts_Shift_id",
                        column: x => x.Shift_id,
                        principalTable: "CourierShifts",
                        principalColumn: "ID_Shift",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourierFuelCards_AssignedByUser_id",
                table: "CourierFuelCards",
                column: "AssignedByUser_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierFuelCards_Company_id",
                table: "CourierFuelCards",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierFuelCards_Courier_id",
                table: "CourierFuelCards",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierFuelCards_FuelCard_id",
                table: "CourierFuelCards",
                column: "FuelCard_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierShifts_Company_id",
                table: "CourierShifts",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierShifts_Courier_id",
                table: "CourierShifts",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierShifts_ShiftStatus_id",
                table: "CourierShifts",
                column: "ShiftStatus_id");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Company_id",
                table: "ShiftAssignments",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Order_id",
                table: "ShiftAssignments",
                column: "Order_id");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Shift_id",
                table: "ShiftAssignments",
                column: "Shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_Company_id",
                table: "VehicleAssignments",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_Courier_id",
                table: "VehicleAssignments",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_Vehicle_id",
                table: "VehicleAssignments",
                column: "Vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_Brand_id",
                table: "VehicleModels",
                column: "Brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_DriveType_id",
                table: "VehicleModels",
                column: "DriveType_id");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_TransmissionType_id",
                table: "VehicleModels",
                column: "TransmissionType_id");

            // Создаем дефолтные записи во всех новых таблицах, если они пусты
            migrationBuilder.Sql(@"
                -- Создаем дефолтные записи, если таблицы пусты
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM ""PaymentMethods"") THEN
                        INSERT INTO ""PaymentMethods"" (""Name"") VALUES ('Наличные');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""CourierStatuses"") THEN
                        INSERT INTO ""CourierStatuses"" (""Name"", ""Description"") VALUES ('Активен', 'Курьер доступен для работы');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""ScheduleTypes"") THEN
                        INSERT INTO ""ScheduleTypes"" (""Name"", ""Description"") VALUES ('Полный день', 'Работа в течение всего дня');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""VehicleCategories"") THEN
                        INSERT INTO ""VehicleCategories"" (""Name"", ""Description"") VALUES ('Легковой', 'Легковой автомобиль');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""FuelCardStatuses"") THEN
                        INSERT INTO ""FuelCardStatuses"" (""Name"", ""IsCanBeUsed"") VALUES ('Активна', true);
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""FuelCardTypes"") THEN
                        INSERT INTO ""FuelCardTypes"" (""Name"", ""Priority"") VALUES ('Стандартная', 'Обычная');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""NotificationTypes"") THEN
                        INSERT INTO ""NotificationTypes"" (""Name"", ""Description"") VALUES ('Информация', 'Информационное уведомление');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""OrderStatuses"") THEN
                        INSERT INTO ""OrderStatuses"" (""Name"", ""Description"") VALUES ('Новый', 'Новый заказ');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""OrderTypes"") THEN
                        INSERT INTO ""OrderTypes"" (""Name"", ""Description"", ""Base_price"", ""Price_km"", ""Estimated_delivery_factor"") 
                        VALUES ('Стандарт', 'Стандартная доставка', 100, 10, 1.0);
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""PackageTypes"") THEN
                        INSERT INTO ""PackageTypes"" (""Name"", ""Description"", ""Max_weight"", ""Max_height"", ""Max_wight"", ""Max_length"") 
                        VALUES ('Стандарт', 'Стандартная упаковка', 10, 50, 50, 50);
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""FuelTypes"") THEN
                        INSERT INTO ""FuelTypes"" (""Name"") VALUES ('Бензин');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""VehicleBodyTypes"") THEN
                        INSERT INTO ""VehicleBodyTypes"" (""Name"") VALUES ('Седан');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""VehicleBrands"") THEN
                        INSERT INTO ""VehicleBrands"" (""Name"") VALUES ('Неизвестно');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""DriveTypes"") THEN
                        INSERT INTO ""DriveTypes"" (""Name"") VALUES ('Передний');
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM ""TransmissionTypes"") THEN
                        INSERT INTO ""TransmissionTypes"" (""Name"") VALUES ('Механическая');
                    END IF;
                END $$;
            ");

            // Обновляем несуществующие ссылки и NULL значения во всех таблицах перед созданием внешних ключей
            migrationBuilder.Sql(@"
                -- Обновляем ClientProfiles -> PaymentMethods
                UPDATE ""ClientProfiles""
                SET ""Preferred_payment_method_id"" = (
                    SELECT ""ID_PaymentMethod"" FROM ""PaymentMethods"" ORDER BY ""ID_PaymentMethod"" LIMIT 1
                )
                WHERE ""Preferred_payment_method_id"" IS NULL 
                   OR ""Preferred_payment_method_id"" NOT IN (SELECT ""ID_PaymentMethod"" FROM ""PaymentMethods"");
                
                -- Обновляем Orders -> PaymentMethods
                UPDATE ""Orders""
                SET ""PaymentMethod_id"" = (
                    SELECT ""ID_PaymentMethod"" FROM ""PaymentMethods"" ORDER BY ""ID_PaymentMethod"" LIMIT 1
                )
                WHERE ""PaymentMethod_id"" IS NULL 
                   OR ""PaymentMethod_id"" NOT IN (SELECT ""ID_PaymentMethod"" FROM ""PaymentMethods"");
                
                -- Обновляем Orders -> OrderStatuses
                UPDATE ""Orders""
                SET ""Status_id"" = (
                    SELECT ""ID_OrderStatus"" FROM ""OrderStatuses"" ORDER BY ""ID_OrderStatus"" LIMIT 1
                )
                WHERE ""Status_id"" IS NULL 
                   OR ""Status_id"" NOT IN (SELECT ""ID_OrderStatus"" FROM ""OrderStatuses"");
                
                UPDATE ""Orders""
                SET ""OrderStatusID_OrderStatus"" = (
                    SELECT ""ID_OrderStatus"" FROM ""OrderStatuses"" ORDER BY ""ID_OrderStatus"" LIMIT 1
                )
                WHERE ""OrderStatusID_OrderStatus"" IS NULL 
                   OR ""OrderStatusID_OrderStatus"" NOT IN (SELECT ""ID_OrderStatus"" FROM ""OrderStatuses"");
                
                -- Обновляем Orders -> OrderTypes
                UPDATE ""Orders""
                SET ""OrderType_id"" = (
                    SELECT ""ID_OrderType"" FROM ""OrderTypes"" ORDER BY ""ID_OrderType"" LIMIT 1
                )
                WHERE ""OrderType_id"" IS NULL 
                   OR ""OrderType_id"" NOT IN (SELECT ""ID_OrderType"" FROM ""OrderTypes"");
                
                -- Обновляем Orders -> PackageTypes
                UPDATE ""Orders""
                SET ""PackageType_id"" = (
                    SELECT ""ID_PackageType"" FROM ""PackageTypes"" ORDER BY ""ID_PackageType"" LIMIT 1
                )
                WHERE ""PackageType_id"" IS NULL 
                   OR ""PackageType_id"" NOT IN (SELECT ""ID_PackageType"" FROM ""PackageTypes"");
                
                -- Обновляем CourierProfiles -> CourierStatuses (включая NULL)
                UPDATE ""CourierProfiles""
                SET ""CurrentStatus_id"" = (
                    SELECT ""ID_CourierStatus"" FROM ""CourierStatuses"" ORDER BY ""ID_CourierStatus"" LIMIT 1
                )
                WHERE ""CurrentStatus_id"" IS NULL 
                   OR ""CurrentStatus_id"" NOT IN (SELECT ""ID_CourierStatus"" FROM ""CourierStatuses"");
                
                -- Обновляем CourierProfiles -> ScheduleTypes (включая NULL)
                UPDATE ""CourierProfiles""
                SET ""WorkSchedule_id"" = (
                    SELECT ""ID_SheduleType"" FROM ""ScheduleTypes"" ORDER BY ""ID_SheduleType"" LIMIT 1
                )
                WHERE ""WorkSchedule_id"" IS NULL 
                   OR ""WorkSchedule_id"" NOT IN (SELECT ""ID_SheduleType"" FROM ""ScheduleTypes"");
                
                -- Обновляем CourierProfiles -> VehicleCategories (включая NULL)
                UPDATE ""CourierProfiles""
                SET ""VehicleCategory_id"" = (
                    SELECT ""ID_Category"" FROM ""VehicleCategories"" ORDER BY ""ID_Category"" LIMIT 1
                )
                WHERE ""VehicleCategory_id"" IS NULL 
                   OR ""VehicleCategory_id"" NOT IN (SELECT ""ID_Category"" FROM ""VehicleCategories"");
                
                -- Обновляем FuelCards -> FuelCardStatuses (включая NULL)
                UPDATE ""FuelCards""
                SET ""Status_id"" = (
                    SELECT ""ID_Status"" FROM ""FuelCardStatuses"" ORDER BY ""ID_Status"" LIMIT 1
                )
                WHERE ""Status_id"" IS NULL 
                   OR ""Status_id"" NOT IN (SELECT ""ID_Status"" FROM ""FuelCardStatuses"");
                
                -- Обновляем FuelCards -> FuelCardTypes (включая NULL)
                UPDATE ""FuelCards""
                SET ""Type_id"" = (
                    SELECT ""ID_Type"" FROM ""FuelCardTypes"" ORDER BY ""ID_Type"" LIMIT 1
                )
                WHERE ""Type_id"" IS NULL 
                   OR ""Type_id"" NOT IN (SELECT ""ID_Type"" FROM ""FuelCardTypes"");
                
                -- Обновляем Notifications -> NotificationTypes (включая NULL)
                UPDATE ""Notifications""
                SET ""Type_id"" = (
                    SELECT ""ID_NotificationType"" FROM ""NotificationTypes"" ORDER BY ""ID_NotificationType"" LIMIT 1
                )
                WHERE ""Type_id"" IS NULL 
                   OR ""Type_id"" NOT IN (SELECT ""ID_NotificationType"" FROM ""NotificationTypes"");
                
                -- Обновляем Vehicles -> FuelTypes (включая NULL)
                UPDATE ""Vehicles""
                SET ""FuelType_id"" = (
                    SELECT ""ID_FuelType"" FROM ""FuelTypes"" ORDER BY ""ID_FuelType"" LIMIT 1
                )
                WHERE ""FuelType_id"" IS NULL 
                   OR ""FuelType_id"" NOT IN (SELECT ""ID_FuelType"" FROM ""FuelTypes"");
                
                -- Обновляем Vehicles -> VehicleBodyTypes (включая NULL)
                UPDATE ""Vehicles""
                SET ""BodyType_id"" = (
                    SELECT ""ID_BodyType"" FROM ""VehicleBodyTypes"" ORDER BY ""ID_BodyType"" LIMIT 1
                )
                WHERE ""BodyType_id"" IS NULL 
                   OR ""BodyType_id"" NOT IN (SELECT ""ID_BodyType"" FROM ""VehicleBodyTypes"");
                
                -- Обновляем Vehicles -> VehicleCategories (включая NULL)
                UPDATE ""Vehicles""
                SET ""Category_id"" = (
                    SELECT ""ID_Category"" FROM ""VehicleCategories"" ORDER BY ""ID_Category"" LIMIT 1
                )
                WHERE ""Category_id"" IS NULL 
                   OR ""Category_id"" NOT IN (SELECT ""ID_Category"" FROM ""VehicleCategories"");
                
                -- Обновляем Vehicles -> VehicleModels (включая NULL)
                UPDATE ""Vehicles""
                SET ""Model_id"" = (
                    SELECT ""ID_Model"" FROM ""VehicleModels"" ORDER BY ""ID_Model"" LIMIT 1
                )
                WHERE ""Model_id"" IS NULL 
                   OR ""Model_id"" NOT IN (SELECT ""ID_Model"" FROM ""VehicleModels"");
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientProfiles_PaymentMethods_Preferred_payment_method_id",
                table: "ClientProfiles",
                column: "Preferred_payment_method_id",
                principalTable: "PaymentMethods",
                principalColumn: "ID_PaymentMethod",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_CourierStatuses_CurrentStatus_id",
                table: "CourierProfiles",
                column: "CurrentStatus_id",
                principalTable: "CourierStatuses",
                principalColumn: "ID_CourierStatus",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_ScheduleTypes_WorkSchedule_id",
                table: "CourierProfiles",
                column: "WorkSchedule_id",
                principalTable: "ScheduleTypes",
                principalColumn: "ID_SheduleType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_VehicleCategories_VehicleCategory_id",
                table: "CourierProfiles",
                column: "VehicleCategory_id",
                principalTable: "VehicleCategories",
                principalColumn: "ID_Category",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelCards_FuelCardStatuses_Status_id",
                table: "FuelCards",
                column: "Status_id",
                principalTable: "FuelCardStatuses",
                principalColumn: "ID_Status",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelCards_FuelCardTypes_Type_id",
                table: "FuelCards",
                column: "Type_id",
                principalTable: "FuelCardTypes",
                principalColumn: "ID_Type",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_NotificationTypes_Type_id",
                table: "Notifications",
                column: "Type_id",
                principalTable: "NotificationTypes",
                principalColumn: "ID_NotificationType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderStatuses_OrderStatusID_OrderStatus",
                table: "Orders",
                column: "OrderStatusID_OrderStatus",
                principalTable: "OrderStatuses",
                principalColumn: "ID_OrderStatus");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderStatuses_Status_id",
                table: "Orders",
                column: "Status_id",
                principalTable: "OrderStatuses",
                principalColumn: "ID_OrderStatus",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderTypes_OrderType_id",
                table: "Orders",
                column: "OrderType_id",
                principalTable: "OrderTypes",
                principalColumn: "ID_OrderType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PackageTypes_PackageType_id",
                table: "Orders",
                column: "PackageType_id",
                principalTable: "PackageTypes",
                principalColumn: "ID_PackageType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentMethods_PaymentMethod_id",
                table: "Orders",
                column: "PaymentMethod_id",
                principalTable: "PaymentMethods",
                principalColumn: "ID_PaymentMethod",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_FuelTypes_FuelType_id",
                table: "Vehicles",
                column: "FuelType_id",
                principalTable: "FuelTypes",
                principalColumn: "ID_FuelType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleBodyTypes_BodyType_id",
                table: "Vehicles",
                column: "BodyType_id",
                principalTable: "VehicleBodyTypes",
                principalColumn: "ID_BodyType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleCategories_Category_id",
                table: "Vehicles",
                column: "Category_id",
                principalTable: "VehicleCategories",
                principalColumn: "ID_Category",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleModels_Model_id",
                table: "Vehicles",
                column: "Model_id",
                principalTable: "VehicleModels",
                principalColumn: "ID_Model",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientProfiles_PaymentMethods_Preferred_payment_method_id",
                table: "ClientProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_CourierStatuses_CurrentStatus_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_ScheduleTypes_WorkSchedule_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_VehicleCategories_VehicleCategory_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelCards_FuelCardStatuses_Status_id",
                table: "FuelCards");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelCards_FuelCardTypes_Type_id",
                table: "FuelCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_NotificationTypes_Type_id",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderStatuses_OrderStatusID_OrderStatus",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderStatuses_Status_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderTypes_OrderType_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PackageTypes_PackageType_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentMethods_PaymentMethod_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_FuelTypes_FuelType_id",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleBodyTypes_BodyType_id",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleCategories_Category_id",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleModels_Model_id",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "CourierFuelCards");

            migrationBuilder.DropTable(
                name: "CourierStatuses");

            migrationBuilder.DropTable(
                name: "FuelCardStatuses");

            migrationBuilder.DropTable(
                name: "FuelCardTypes");

            migrationBuilder.DropTable(
                name: "FuelTypes");

            migrationBuilder.DropTable(
                name: "NotificationTypes");

            migrationBuilder.DropTable(
                name: "OrderStatuses");

            migrationBuilder.DropTable(
                name: "OrderTypes");

            migrationBuilder.DropTable(
                name: "PackageTypes");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "ScheduleTypes");

            migrationBuilder.DropTable(
                name: "ShiftAssignments");

            migrationBuilder.DropTable(
                name: "VehicleAssignments");

            migrationBuilder.DropTable(
                name: "VehicleBodyTypes");

            migrationBuilder.DropTable(
                name: "VehicleCategories");

            migrationBuilder.DropTable(
                name: "VehicleModels");

            migrationBuilder.DropTable(
                name: "CourierShifts");

            migrationBuilder.DropTable(
                name: "DriveTypes");

            migrationBuilder.DropTable(
                name: "TransmissionTypes");

            migrationBuilder.DropTable(
                name: "VehicleBrands");

            migrationBuilder.DropTable(
                name: "ShiftStatuses");

            migrationBuilder.RenameColumn(
                name: "OrderStatusID_OrderStatus",
                table: "Orders",
                newName: "Order_StatusesID_OrderStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_OrderStatusID_OrderStatus",
                table: "Orders",
                newName: "IX_Orders_Order_StatusesID_OrderStatus");

            migrationBuilder.CreateTable(
                name: "Courier_FuelCards",
                columns: table => new
                {
                    ID_CF = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignedByUser_id = table.Column<int>(type: "integer", nullable: false),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    FuelCard_id = table.Column<int>(type: "integer", nullable: false),
                    Is_backup = table.Column<bool>(type: "boolean", nullable: false),
                    Is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courier_FuelCards", x => x.ID_CF);
                    table.ForeignKey(
                        name: "FK_Courier_FuelCards_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
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
                name: "Courier_Statuses",
                columns: table => new
                {
                    ID_CourierStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courier_Statuses", x => x.ID_CourierStatus);
                });

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
                    IsCanBeUsed = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
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
                name: "Notification_Types",
                columns: table => new
                {
                    ID_NotificationType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification_Types", x => x.ID_NotificationType);
                });

            migrationBuilder.CreateTable(
                name: "Order_Statuses",
                columns: table => new
                {
                    ID_OrderStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_Statuses", x => x.ID_OrderStatus);
                });

            migrationBuilder.CreateTable(
                name: "Order_Types",
                columns: table => new
                {
                    ID_OrderType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Base_price = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Estimated_delivery_factor = table.Column<decimal>(type: "numeric", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price_km = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_Types", x => x.ID_OrderType);
                });

            migrationBuilder.CreateTable(
                name: "Package_Types",
                columns: table => new
                {
                    ID_PackageType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Max_height = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_length = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_wight = table.Column<decimal>(type: "numeric", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Package_Types", x => x.ID_PackageType);
                });

            migrationBuilder.CreateTable(
                name: "Payment_Methods",
                columns: table => new
                {
                    ID_PaymentMethod = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment_Methods", x => x.ID_PaymentMethod);
                });

            migrationBuilder.CreateTable(
                name: "Schedule_Types",
                columns: table => new
                {
                    ID_SheduleType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule_Types", x => x.ID_SheduleType);
                });

            migrationBuilder.CreateTable(
                name: "Shift_Statuses",
                columns: table => new
                {
                    ID_ShiftStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shift_Statuses", x => x.ID_ShiftStatus);
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
                name: "Vehicle_Assignments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    Vehicle_id = table.Column<int>(type: "integer", nullable: false),
                    End_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Mileage_end = table.Column<int>(type: "integer", nullable: false),
                    Mileage_start = table.Column<int>(type: "integer", nullable: false),
                    Start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_Assignments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Vehicle_Assignments_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
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
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Max_Weight = table.Column<decimal>(type: "numeric", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Speed_factor = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_Categories", x => x.ID_Category);
                });

            migrationBuilder.CreateTable(
                name: "Courier_Shifts",
                columns: table => new
                {
                    ID_Shift = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    ShiftStatus_id = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courier_Shifts", x => x.ID_Shift);
                    table.ForeignKey(
                        name: "FK_Courier_Shifts_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courier_Shifts_CourierProfiles_Courier_id",
                        column: x => x.Courier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courier_Shifts_Shift_Statuses_ShiftStatus_id",
                        column: x => x.ShiftStatus_id,
                        principalTable: "Shift_Statuses",
                        principalColumn: "ID_ShiftStatus",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle_Models",
                columns: table => new
                {
                    ID_Model = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Brand_id = table.Column<int>(type: "integer", nullable: false),
                    DriveType_id = table.Column<int>(type: "integer", nullable: false),
                    TransmissionType_id = table.Column<int>(type: "integer", nullable: false),
                    AvgFuelCity = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgFuelHighWay = table.Column<decimal>(type: "numeric", nullable: false),
                    EngineCapacity = table.Column<decimal>(type: "numeric", nullable: false),
                    HorsePower = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<DateOnly>(type: "date", nullable: false)
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
                name: "Shift_Assignments",
                columns: table => new
                {
                    ID_ShiftAssignment = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    Shift_id = table.Column<int>(type: "integer", nullable: false),
                    Assignment_sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shift_Assignments", x => x.ID_ShiftAssignment);
                    table.ForeignKey(
                        name: "FK_Shift_Assignments_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shift_Assignments_Courier_Shifts_Shift_id",
                        column: x => x.Shift_id,
                        principalTable: "Courier_Shifts",
                        principalColumn: "ID_Shift",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shift_Assignments_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courier_FuelCards_AssignedByUser_id",
                table: "Courier_FuelCards",
                column: "AssignedByUser_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_FuelCards_Company_id",
                table: "Courier_FuelCards",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_FuelCards_Courier_id",
                table: "Courier_FuelCards",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_FuelCards_FuelCard_id",
                table: "Courier_FuelCards",
                column: "FuelCard_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_Shifts_Company_id",
                table: "Courier_Shifts",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_Shifts_Courier_id",
                table: "Courier_Shifts",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_Shifts_ShiftStatus_id",
                table: "Courier_Shifts",
                column: "ShiftStatus_id");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_Assignments_Company_id",
                table: "Shift_Assignments",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_Assignments_Order_id",
                table: "Shift_Assignments",
                column: "Order_id");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_Assignments_Shift_id",
                table: "Shift_Assignments",
                column: "Shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_Assignments_Company_id",
                table: "Vehicle_Assignments",
                column: "Company_id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ClientProfiles_Payment_Methods_Preferred_payment_method_id",
                table: "ClientProfiles",
                column: "Preferred_payment_method_id",
                principalTable: "Payment_Methods",
                principalColumn: "ID_PaymentMethod",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Courier_Statuses_CurrentStatus_id",
                table: "CourierProfiles",
                column: "CurrentStatus_id",
                principalTable: "Courier_Statuses",
                principalColumn: "ID_CourierStatus",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Schedule_Types_WorkSchedule_id",
                table: "CourierProfiles",
                column: "WorkSchedule_id",
                principalTable: "Schedule_Types",
                principalColumn: "ID_SheduleType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_Categories_VehicleCategory_id",
                table: "CourierProfiles",
                column: "VehicleCategory_id",
                principalTable: "Vehicle_Categories",
                principalColumn: "ID_Category",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelCards_FuelCard_Statuses_Status_id",
                table: "FuelCards",
                column: "Status_id",
                principalTable: "FuelCard_Statuses",
                principalColumn: "ID_Status",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelCards_FuelCard_Types_Type_id",
                table: "FuelCards",
                column: "Type_id",
                principalTable: "FuelCard_Types",
                principalColumn: "ID_Type",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Notification_Types_Type_id",
                table: "Notifications",
                column: "Type_id",
                principalTable: "Notification_Types",
                principalColumn: "ID_NotificationType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Order_Statuses_Order_StatusesID_OrderStatus",
                table: "Orders",
                column: "Order_StatusesID_OrderStatus",
                principalTable: "Order_Statuses",
                principalColumn: "ID_OrderStatus");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Order_Statuses_Status_id",
                table: "Orders",
                column: "Status_id",
                principalTable: "Order_Statuses",
                principalColumn: "ID_OrderStatus",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Order_Types_OrderType_id",
                table: "Orders",
                column: "OrderType_id",
                principalTable: "Order_Types",
                principalColumn: "ID_OrderType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Package_Types_PackageType_id",
                table: "Orders",
                column: "PackageType_id",
                principalTable: "Package_Types",
                principalColumn: "ID_PackageType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Payment_Methods_PaymentMethod_id",
                table: "Orders",
                column: "PaymentMethod_id",
                principalTable: "Payment_Methods",
                principalColumn: "ID_PaymentMethod",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Fuel_Types_FuelType_id",
                table: "Vehicles",
                column: "FuelType_id",
                principalTable: "Fuel_Types",
                principalColumn: "ID_FuelType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Vehicle_BodyTypes_BodyType_id",
                table: "Vehicles",
                column: "BodyType_id",
                principalTable: "Vehicle_BodyTypes",
                principalColumn: "ID_BodyType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Vehicle_Categories_Category_id",
                table: "Vehicles",
                column: "Category_id",
                principalTable: "Vehicle_Categories",
                principalColumn: "ID_Category",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Vehicle_Models_Model_id",
                table: "Vehicles",
                column: "Model_id",
                principalTable: "Vehicle_Models",
                principalColumn: "ID_Model",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
