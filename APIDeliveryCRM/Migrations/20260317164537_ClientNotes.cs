using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class ClientNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientNoteTypes",
                columns: table => new
                {
                    ID_ClientNoteType = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientNoteTypes", x => x.ID_ClientNoteType);
                });

            migrationBuilder.CreateTable(
                name: "ClientNotes",
                columns: table => new
                {
                    ID_ClientNote = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientProfile_id = table.Column<int>(type: "integer", nullable: false),
                    Author_id = table.Column<int>(type: "integer", nullable: false),
                    ClientNoteType_id = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientNotes", x => x.ID_ClientNote);
                    table.ForeignKey(
                        name: "FK_ClientNotes_ClientNoteTypes_ClientNoteType_id",
                        column: x => x.ClientNoteType_id,
                        principalTable: "ClientNoteTypes",
                        principalColumn: "ID_ClientNoteType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientNotes_ClientProfiles_ClientProfile_id",
                        column: x => x.ClientProfile_id,
                        principalTable: "ClientProfiles",
                        principalColumn: "ID_ClientProfile",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientNotes_Users_Author_id",
                        column: x => x.Author_id,
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_Author_id",
                table: "ClientNotes",
                column: "Author_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_ClientNoteType_id",
                table: "ClientNotes",
                column: "ClientNoteType_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_ClientProfile_id",
                table: "ClientNotes",
                column: "ClientProfile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientNotes");

            migrationBuilder.DropTable(
                name: "ClientNoteTypes");
        }
    }
}
