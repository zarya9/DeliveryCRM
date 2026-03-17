using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class ClientTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientSegment_id",
                table: "ClientProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientStatus_id",
                table: "ClientProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClientSegments",
                columns: table => new
                {
                    ID_ClientSegment = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSegments", x => x.ID_ClientSegment);
                });

            migrationBuilder.CreateTable(
                name: "ClientStatuses",
                columns: table => new
                {
                    ID_ClientStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientStatuses", x => x.ID_ClientStatus);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_ClientSegment_id",
                table: "ClientProfiles",
                column: "ClientSegment_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_ClientStatus_id",
                table: "ClientProfiles",
                column: "ClientStatus_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientProfiles_ClientSegments_ClientSegment_id",
                table: "ClientProfiles",
                column: "ClientSegment_id",
                principalTable: "ClientSegments",
                principalColumn: "ID_ClientSegment",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientProfiles_ClientStatuses_ClientStatus_id",
                table: "ClientProfiles",
                column: "ClientStatus_id",
                principalTable: "ClientStatuses",
                principalColumn: "ID_ClientStatus",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientProfiles_ClientSegments_ClientSegment_id",
                table: "ClientProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientProfiles_ClientStatuses_ClientStatus_id",
                table: "ClientProfiles");

            migrationBuilder.DropTable(
                name: "ClientSegments");

            migrationBuilder.DropTable(
                name: "ClientStatuses");

            migrationBuilder.DropIndex(
                name: "IX_ClientProfiles_ClientSegment_id",
                table: "ClientProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ClientProfiles_ClientStatus_id",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "ClientSegment_id",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "ClientStatus_id",
                table: "ClientProfiles");
        }
    }
}
