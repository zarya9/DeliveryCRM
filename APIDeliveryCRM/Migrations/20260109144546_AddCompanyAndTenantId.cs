using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class AddCompanyAndTenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сначала создаем таблицу Companies
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    ID_Company = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subdomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SecondaryColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Is_Active = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    MaxOrdersPerMonth = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AzureStorageConnectionString = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AzureStorageContainerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    KafkaBootstrapServers = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    KafkaGroupId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.ID_Company);
                });

            // Создаем дефолтную компанию
            migrationBuilder.Sql(@"
                INSERT INTO ""Companies"" (
                    ""Name"", ""Subdomain"", ""Created_at"", ""Is_Active"", 
                    ""SubscriptionPlan"", ""MaxUsers"", ""MaxOrdersPerMonth"", ""SubscriptionExpiresAt""
                )
                VALUES (
                    'Default Company', 'default', NOW(), true, 
                    'Pro', 100, 10000, NOW() + INTERVAL '1 year'
                );
            ");

            // Теперь добавляем колонки Company_id
            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Reviews",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Reports",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "ManagerProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "FuelCards",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "CourierProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Courier_Shifts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "ClientProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "ChatRooms",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "AuditLogs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Company_id",
                table: "Addresses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Company_id",
                table: "Vehicles",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Company_id",
                table: "Users",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Company_id",
                table: "Reviews",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Company_id",
                table: "Reports",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Company_id",
                table: "Orders",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Company_id",
                table: "Notifications",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerProfiles_Company_id",
                table: "ManagerProfiles",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_Company_id",
                table: "FuelCards",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_CourierProfiles_Company_id",
                table: "CourierProfiles",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Courier_Shifts_Company_id",
                table: "Courier_Shifts",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_Company_id",
                table: "ClientProfiles",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_Company_id",
                table: "ChatRooms",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Company_id",
                table: "AuditLogs",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_Company_id",
                table: "Addresses",
                column: "Company_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Companies_Company_id",
                table: "Addresses",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Companies_Company_id",
                table: "AuditLogs",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_Companies_Company_id",
                table: "ChatRooms",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientProfiles_Companies_Company_id",
                table: "ClientProfiles",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courier_Shifts_Companies_Company_id",
                table: "Courier_Shifts",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Companies_Company_id",
                table: "CourierProfiles",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelCards_Companies_Company_id",
                table: "FuelCards",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManagerProfiles_Companies_Company_id",
                table: "ManagerProfiles",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Companies_Company_id",
                table: "Notifications",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Companies_Company_id",
                table: "Orders",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Companies_Company_id",
                table: "Reports",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Companies_Company_id",
                table: "Reviews",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Companies_Company_id",
                table: "Users",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Companies_Company_id",
                table: "Vehicles",
                column: "Company_id",
                principalTable: "Companies",
                principalColumn: "ID_Company",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Companies_Company_id",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Companies_Company_id",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_Companies_Company_id",
                table: "ChatRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientProfiles_Companies_Company_id",
                table: "ClientProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Courier_Shifts_Companies_Company_id",
                table: "Courier_Shifts");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Companies_Company_id",
                table: "CourierProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelCards_Companies_Company_id",
                table: "FuelCards");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagerProfiles_Companies_Company_id",
                table: "ManagerProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Companies_Company_id",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Companies_Company_id",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Companies_Company_id",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Companies_Company_id",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Companies_Company_id",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Companies_Company_id",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_Company_id",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Users_Company_id",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_Company_id",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Company_id",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Company_id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Company_id",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_ManagerProfiles_Company_id",
                table: "ManagerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_FuelCards_Company_id",
                table: "FuelCards");

            migrationBuilder.DropIndex(
                name: "IX_CourierProfiles_Company_id",
                table: "CourierProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Courier_Shifts_Company_id",
                table: "Courier_Shifts");

            migrationBuilder.DropIndex(
                name: "IX_ClientProfiles_Company_id",
                table: "ClientProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_Company_id",
                table: "ChatRooms");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Company_id",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_Company_id",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "ManagerProfiles");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "FuelCards");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "CourierProfiles");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Courier_Shifts");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Company_id",
                table: "Addresses");
        }
    }
}
