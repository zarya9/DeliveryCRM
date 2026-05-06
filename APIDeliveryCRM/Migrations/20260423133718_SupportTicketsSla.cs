using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class SupportTicketsSla : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    ID_SupportTicket = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Order_id = table.Column<int>(type: "integer", nullable: true),
                    ClientProfile_id = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<byte>(type: "smallint", nullable: false),
                    Priority = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    ResponsibleUser_id = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUser_id = table.Column<int>(type: "integer", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstResponse_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Sla_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delay_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.ID_SupportTicket);
                    table.ForeignKey(
                        name: "FK_SupportTickets_ClientProfiles_ClientProfile_id",
                        column: x => x.ClientProfile_id,
                        principalTable: "ClientProfiles",
                        principalColumn: "ID_ClientProfile",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Orders_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Orders",
                        principalColumn: "ID_Order",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Users_CreatedByUser_id",
                        column: x => x.CreatedByUser_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Users_ResponsibleUser_id",
                        column: x => x.ResponsibleUser_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_ClientProfile_id",
                table: "SupportTickets",
                column: "ClientProfile_id");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Company_id",
                table: "SupportTickets",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedByUser_id",
                table: "SupportTickets",
                column: "CreatedByUser_id");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Order_id",
                table: "SupportTickets",
                column: "Order_id");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_ResponsibleUser_id",
                table: "SupportTickets",
                column: "ResponsibleUser_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportTickets");
        }
    }
}
