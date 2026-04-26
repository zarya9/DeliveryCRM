using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class ChatQuickReplyTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatQuickReplyTemplates",
                columns: table => new
                {
                    ID_ChatQuickReplyTemplate = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    User_id = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Is_active = table.Column<bool>(type: "boolean", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatQuickReplyTemplates", x => x.ID_ChatQuickReplyTemplate);
                    table.ForeignKey(
                        name: "FK_ChatQuickReplyTemplates_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatQuickReplyTemplates_Users_User_id",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatQuickReplyTemplates_Company_id",
                table: "ChatQuickReplyTemplates",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatQuickReplyTemplates_User_id",
                table: "ChatQuickReplyTemplates",
                column: "User_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatQuickReplyTemplates");
        }
    }
}
