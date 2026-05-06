using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class first : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courier_Statuses",
                columns: table => new
                {
                    ID_CourierStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courier_Statuses", x => x.ID_CourierStatus);
                });

            migrationBuilder.CreateTable(
                name: "Notification_Types",
                columns: table => new
                {
                    ID_NotificationType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
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
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
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
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Base_price = table.Column<decimal>(type: "numeric", nullable: false),
                    Price_km = table.Column<decimal>(type: "numeric", nullable: false),
                    Estimated_delivery_factor = table.Column<decimal>(type: "numeric", nullable: false)
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
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Max_weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_height = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_wight = table.Column<decimal>(type: "numeric", nullable: false),
                    Max_length = table.Column<decimal>(type: "numeric", nullable: false)
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
                name: "Roles",
                columns: table => new
                {
                    ID_Role = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.ID_Role);
                });

            migrationBuilder.CreateTable(
                name: "Schedule_Types",
                columns: table => new
                {
                    ID_SheduleType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
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
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shift_Statuses", x => x.ID_ShiftStatus);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle_Types",
                columns: table => new
                {
                    ID_VehicleTypes = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Max_Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Speed_factor = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle_Types", x => x.ID_VehicleTypes);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ID_User = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FName = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Patronumic = table.Column<string>(type: "text", nullable: false),
                    Created_at = table.Column<DateOnly>(type: "date", nullable: false),
                    Is_Active = table.Column<bool>(type: "boolean", nullable: false),
                    Role_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ID_User);
                    table.ForeignKey(
                        name: "FK_Users_Roles_Role_id",
                        column: x => x.Role_id,
                        principalTable: "Roles",
                        principalColumn: "ID_Role",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientProfiles",
                columns: table => new
                {
                    ID_ClientProfile = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Default_address = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    User_id = table.Column<int>(type: "integer", nullable: false),
                    Preferred_payment_method_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientProfiles", x => x.ID_ClientProfile);
                    table.ForeignKey(
                        name: "FK_ClientProfiles_Payment_Methods_Preferred_payment_method_id",
                        column: x => x.Preferred_payment_method_id,
                        principalTable: "Payment_Methods",
                        principalColumn: "ID_PaymentMethod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientProfiles_Users_User_id",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierProfiles",
                columns: table => new
                {
                    ID_CourierProfile = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User_id = table.Column<int>(type: "integer", nullable: false),
                    VehicleType_id = table.Column<int>(type: "integer", nullable: false),
                    DriverLicense = table.Column<string>(type: "text", nullable: false),
                    Passport_data = table.Column<string>(type: "text", nullable: false),
                    WorkSchedule_id = table.Column<int>(type: "integer", nullable: false),
                    CurrentStatus_id = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    Total_deliveries = table.Column<int>(type: "integer", nullable: false),
                    Is_online = table.Column<bool>(type: "boolean", nullable: false),
                    Current_lat = table.Column<decimal>(type: "numeric", nullable: false),
                    Current_lon = table.Column<decimal>(type: "numeric", nullable: false),
                    LastActivity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierProfiles", x => x.ID_CourierProfile);
                    table.ForeignKey(
                        name: "FK_CourierProfiles_Courier_Statuses_CurrentStatus_id",
                        column: x => x.CurrentStatus_id,
                        principalTable: "Courier_Statuses",
                        principalColumn: "ID_CourierStatus",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierProfiles_Schedule_Types_WorkSchedule_id",
                        column: x => x.WorkSchedule_id,
                        principalTable: "Schedule_Types",
                        principalColumn: "ID_SheduleType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierProfiles_Users_User_id",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierProfiles_Vehicle_Types_VehicleType_id",
                        column: x => x.VehicleType_id,
                        principalTable: "Vehicle_Types",
                        principalColumn: "ID_VehicleTypes",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Logins",
                columns: table => new
                {
                    ID_Login = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Password = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ID_User = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logins", x => x.ID_Login);
                    table.ForeignKey(
                        name: "FK_Logins_Users_ID_User",
                        column: x => x.ID_User,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Courier_Shifts",
                columns: table => new
                {
                    ID_Shift = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShiftStatus_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courier_Shifts", x => x.ID_Shift);
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
                name: "Orders",
                columns: table => new
                {
                    ID_Order = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Order_Number = table.Column<int>(type: "integer", nullable: false),
                    Client_id = table.Column<int>(type: "integer", nullable: false),
                    OrderType_id = table.Column<int>(type: "integer", nullable: false),
                    Status_id = table.Column<int>(type: "integer", nullable: false),
                    Courier_id = table.Column<int>(type: "integer", nullable: false),
                    PackageType_id = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    Length = table.Column<decimal>(type: "numeric", nullable: false),
                    Width = table.Column<decimal>(type: "numeric", nullable: false),
                    Estimated_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    Final_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentMethod_id = table.Column<int>(type: "integer", nullable: false),
                    Is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    Order_StatusesID_OrderStatus = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.ID_Order);
                    table.ForeignKey(
                        name: "FK_Orders_ClientProfiles_Client_id",
                        column: x => x.Client_id,
                        principalTable: "ClientProfiles",
                        principalColumn: "ID_ClientProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_CourierProfiles_Courier_id",
                        column: x => x.Courier_id,
                        principalTable: "CourierProfiles",
                        principalColumn: "ID_CourierProfile",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Order_Statuses_Order_StatusesID_OrderStatus",
                        column: x => x.Order_StatusesID_OrderStatus,
                        principalTable: "Order_Statuses",
                        principalColumn: "ID_OrderStatus");
                    table.ForeignKey(
                        name: "FK_Orders_Order_Statuses_Status_id",
                        column: x => x.Status_id,
                        principalTable: "Order_Statuses",
                        principalColumn: "ID_OrderStatus",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Order_Types_OrderType_id",
                        column: x => x.OrderType_id,
                        principalTable: "Order_Types",
                        principalColumn: "ID_OrderType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Package_Types_PackageType_id",
                        column: x => x.PackageType_id,
                        principalTable: "Package_Types",
                        principalColumn: "ID_PackageType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Payment_Methods_PaymentMethod_id",
                        column: x => x.PaymentMethod_id,
                        principalTable: "Payment_Methods",
                        principalColumn: "ID_PaymentMethod",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    ID_Notification = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User_id = table.Column<int>(type: "integer", nullable: false),
                    Type_id = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    Is_read = table.Column<bool>(type: "boolean", nullable: false),
                    Sent_at = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.ID_Notification);
                    table.ForeignKey(
                        name: "FK_Notifications_Notification_Types_Type_id",
                        column: x => x.Type_id,
                        principalTable: "Notification_Types",
                        principalColumn: "ID_NotificationType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_User_id",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    ID_Review = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    Author_id = table.Column<int>(type: "integer", nullable: false),
                    TargetUser_id = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.ID_Review);
                    table.ForeignKey(
                        name: "FK_Reviews_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_Author_id",
                        column: x => x.Author_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_TargetUser_id",
                        column: x => x.TargetUser_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shift_Assignments",
                columns: table => new
                {
                    ID_ShiftAssignment = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Shift_id = table.Column<int>(type: "integer", nullable: false),
                    Order_id = table.Column<int>(type: "integer", nullable: false),
                    Assignment_sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shift_Assignments", x => x.ID_ShiftAssignment);
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
                name: "IX_ClientProfiles_Preferred_payment_method_id",
                table: "ClientProfiles",
                column: "Preferred_payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_User_id",
                table: "ClientProfiles",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_Shifts_Courier_id",
                table: "Courier_Shifts",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_Shifts_ShiftStatus_id",
                table: "Courier_Shifts",
                column: "ShiftStatus_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierProfiles_CurrentStatus_id",
                table: "CourierProfiles",
                column: "CurrentStatus_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierProfiles_User_id",
                table: "CourierProfiles",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierProfiles_VehicleType_id",
                table: "CourierProfiles",
                column: "VehicleType_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierProfiles_WorkSchedule_id",
                table: "CourierProfiles",
                column: "WorkSchedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_Logins_ID_User",
                table: "Logins",
                column: "ID_User");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Order_id",
                table: "Notifications",
                column: "Order_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type_id",
                table: "Notifications",
                column: "Type_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_id",
                table: "Notifications",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Client_id",
                table: "Orders",
                column: "Client_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Courier_id",
                table: "Orders",
                column: "Courier_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Order_StatusesID_OrderStatus",
                table: "Orders",
                column: "Order_StatusesID_OrderStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderType_id",
                table: "Orders",
                column: "OrderType_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PackageType_id",
                table: "Orders",
                column: "PackageType_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentMethod_id",
                table: "Orders",
                column: "PaymentMethod_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_id",
                table: "Orders",
                column: "Status_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Author_id",
                table: "Reviews",
                column: "Author_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Order_id",
                table: "Reviews",
                column: "Order_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TargetUser_id",
                table: "Reviews",
                column: "TargetUser_id");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_Assignments_Order_id",
                table: "Shift_Assignments",
                column: "Order_id");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_Assignments_Shift_id",
                table: "Shift_Assignments",
                column: "Shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_id",
                table: "Users",
                column: "Role_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Logins");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Shift_Assignments");

            migrationBuilder.DropTable(
                name: "Notification_Types");

            migrationBuilder.DropTable(
                name: "Courier_Shifts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Shift_Statuses");

            migrationBuilder.DropTable(
                name: "ClientProfiles");

            migrationBuilder.DropTable(
                name: "CourierProfiles");

            migrationBuilder.DropTable(
                name: "Order_Statuses");

            migrationBuilder.DropTable(
                name: "Order_Types");

            migrationBuilder.DropTable(
                name: "Package_Types");

            migrationBuilder.DropTable(
                name: "Payment_Methods");

            migrationBuilder.DropTable(
                name: "Courier_Statuses");

            migrationBuilder.DropTable(
                name: "Schedule_Types");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vehicle_Types");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
