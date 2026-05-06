using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class second : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_Types_VehicleType_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CourierProfiles_Courier_id",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicle_Types",
                table: "Vehicle_Types");

            migrationBuilder.RenameTable(
                name: "Vehicle_Types",
                newName: "Vehicle_types");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Vehicle_types",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Vehicle_types",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Patronumic",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Delivered_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "Courier_id",
                table: "Orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAddress_id",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PickupAddress_id",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Order_Statuses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Passport_data",
                table: "CourierProfiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DriverLicense",
                table: "CourierProfiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicle_types",
                table: "Vehicle_types",
                column: "ID_VehicleTypes");

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    ID_Address = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    House = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Flat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    User_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.ID_Address);
                    table.ForeignKey(
                        name: "FK_Addresses_Users_User_id",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatRoomTypes",
                columns: table => new
                {
                    ID_ChatRoomType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatRoomTypes", x => x.ID_ChatRoomType);
                });

            migrationBuilder.CreateTable(
                name: "ManagerProfiles",
                columns: table => new
                {
                    ID_ManagerProfile = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User_id = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Is_Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerProfiles", x => x.ID_ManagerProfile);
                    table.ForeignKey(
                        name: "FK_ManagerProfiles_Users_User_id",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatRooms",
                columns: table => new
                {
                    ID_ChatRoom = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChatRoomType_id = table.Column<int>(type: "integer", nullable: false),
                    Order_id = table.Column<int>(type: "integer", nullable: true),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessage_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatRooms", x => x.ID_ChatRoom);
                    table.ForeignKey(
                        name: "FK_ChatRooms_ChatRoomTypes_ChatRoomType_id",
                        column: x => x.ChatRoomType_id,
                        principalTable: "ChatRoomTypes",
                        principalColumn: "ID_ChatRoomType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatRooms_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    ID_ChatMessage = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatRoom_id = table.Column<int>(type: "integer", nullable: false),
                    Sender_id = table.Column<int>(type: "integer", nullable: false),
                    MessageText = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.ID_ChatMessage);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatRooms_ChatRoom_id",
                        column: x => x.ChatRoom_id,
                        principalTable: "ChatRooms",
                        principalColumn: "ID_ChatRoom",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_Sender_id",
                        column: x => x.Sender_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatParticipants",
                columns: table => new
                {
                    ID_ChatParticipant = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatRoom_id = table.Column<int>(type: "integer", nullable: false),
                    User_id = table.Column<int>(type: "integer", nullable: false),
                    Joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Is_active = table.Column<bool>(type: "boolean", nullable: false),
                    LastRead_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipants", x => x.ID_ChatParticipant);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_ChatRooms_ChatRoom_id",
                        column: x => x.ChatRoom_id,
                        principalTable: "ChatRooms",
                        principalColumn: "ID_ChatRoom",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_Users_User_id",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryAddress_id",
                table: "Orders",
                column: "DeliveryAddress_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PickupAddress_id",
                table: "Orders",
                column: "PickupAddress_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierProfiles_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles",
                column: "Vehicle_typesID_VehicleTypes");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_User_id",
                table: "Addresses",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatRoom_id",
                table: "ChatMessages",
                column: "ChatRoom_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Sender_id",
                table: "ChatMessages",
                column: "Sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_ChatRoom_id",
                table: "ChatParticipants",
                column: "ChatRoom_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_User_id",
                table: "ChatParticipants",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_ChatRoomType_id",
                table: "ChatRooms",
                column: "ChatRoomType_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_Order_id",
                table: "ChatRooms",
                column: "Order_id");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerProfiles_User_id",
                table: "ManagerProfiles",
                column: "User_id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Addresses_DeliveryAddress_id",
                table: "Orders",
                column: "DeliveryAddress_id",
                principalTable: "Addresses",
                principalColumn: "ID_Address",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Addresses_PickupAddress_id",
                table: "Orders",
                column: "PickupAddress_id",
                principalTable: "Addresses",
                principalColumn: "ID_Address",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CourierProfiles_Courier_id",
                table: "Orders",
                column: "Courier_id",
                principalTable: "CourierProfiles",
                principalColumn: "ID_CourierProfile",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_types_VehicleType_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Vehicle_types_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Addresses_DeliveryAddress_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Addresses_PickupAddress_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CourierProfiles_Courier_id",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatParticipants");

            migrationBuilder.DropTable(
                name: "ManagerProfiles");

            migrationBuilder.DropTable(
                name: "ChatRooms");

            migrationBuilder.DropTable(
                name: "ChatRoomTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicle_types",
                table: "Vehicle_types");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryAddress_id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PickupAddress_id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_CourierProfiles_Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupAddress_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Vehicle_typesID_VehicleTypes",
                table: "CourierProfiles");

            migrationBuilder.RenameTable(
                name: "Vehicle_types",
                newName: "Vehicle_Types");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Vehicle_Types",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Vehicle_Types",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Patronumic",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Delivered_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Courier_id",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Order_Statuses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Passport_data",
                table: "CourierProfiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DriverLicense",
                table: "CourierProfiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicle_Types",
                table: "Vehicle_Types",
                column: "ID_VehicleTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Vehicle_Types_VehicleType_id",
                table: "CourierProfiles",
                column: "VehicleType_id",
                principalTable: "Vehicle_Types",
                principalColumn: "ID_VehicleTypes",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CourierProfiles_Courier_id",
                table: "Orders",
                column: "Courier_id",
                principalTable: "CourierProfiles",
                principalColumn: "ID_CourierProfile",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
