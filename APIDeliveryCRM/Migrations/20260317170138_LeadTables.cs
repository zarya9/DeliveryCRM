using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class LeadTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeadSources",
                columns: table => new
                {
                    ID_LeadSource = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadSources", x => x.ID_LeadSource);
                });

            migrationBuilder.CreateTable(
                name: "LeadStages",
                columns: table => new
                {
                    ID_LeadStage = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadStages", x => x.ID_LeadStage);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    ID_Lead = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Contact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeadSource_id = table.Column<int>(type: "integer", nullable: false),
                    LeadStage_id = table.Column<int>(type: "integer", nullable: false),
                    ManagerUser_id = table.Column<int>(type: "integer", nullable: true),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.ID_Lead);
                    table.ForeignKey(
                        name: "FK_Leads_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Leads_LeadSources_LeadSource_id",
                        column: x => x.LeadSource_id,
                        principalTable: "LeadSources",
                        principalColumn: "ID_LeadSource",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Leads_LeadStages_LeadStage_id",
                        column: x => x.LeadStage_id,
                        principalTable: "LeadStages",
                        principalColumn: "ID_LeadStage",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Leads_Users_ManagerUser_id",
                        column: x => x.ManagerUser_id,
                        principalTable: "Users",
                        principalColumn: "ID_User");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Company_id",
                table: "Leads",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_LeadSource_id",
                table: "Leads",
                column: "LeadSource_id");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_LeadStage_id",
                table: "Leads",
                column: "LeadStage_id");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ManagerUser_id",
                table: "Leads",
                column: "ManagerUser_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "LeadSources");

            migrationBuilder.DropTable(
                name: "LeadStages");
        }
    }
}
