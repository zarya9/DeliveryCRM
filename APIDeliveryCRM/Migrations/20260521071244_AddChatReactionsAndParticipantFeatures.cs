using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddChatReactionsAndParticipantFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "DeliveryStatus",
                schema: "коммуникации",
                table: "ChatMessages",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "MentionedUserIds",
                schema: "коммуникации",
                table: "ChatMessages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplyToMessage_id",
                schema: "коммуникации",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                schema: "коммуникации",
                columns: table => new
                {
                    ID_MessageReaction = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatMessage_id = table.Column<int>(type: "integer", nullable: false),
                    User_id = table.Column<int>(type: "integer", nullable: false),
                    Emoji = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => x.ID_MessageReaction);
                    table.ForeignKey(
                        name: "FK_MessageReactions_ChatMessages_ChatMessage_id",
                        column: x => x.ChatMessage_id,
                        principalSchema: "коммуникации",
                        principalTable: "ChatMessages",
                        principalColumn: "ID_ChatMessage",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageReactions_Users_User_id",
                        column: x => x.User_id,
                        principalSchema: "пользователи_и_доступ",
                        principalTable: "Users",
                        principalColumn: "ID_User",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReplyToMessage_id",
                schema: "коммуникации",
                table: "ChatMessages",
                column: "ReplyToMessage_id");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_User_id",
                schema: "коммуникации",
                table: "MessageReactions",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "UX_MessageReactions_Message_User_Emoji",
                schema: "коммуникации",
                table: "MessageReactions",
                columns: new[] { "ChatMessage_id", "User_id", "Emoji" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatMessages_ReplyToMessage_id",
                schema: "коммуникации",
                table: "ChatMessages",
                column: "ReplyToMessage_id",
                principalSchema: "коммуникации",
                principalTable: "ChatMessages",
                principalColumn: "ID_ChatMessage",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatMessages_ReplyToMessage_id",
                schema: "коммуникации",
                table: "ChatMessages");

            migrationBuilder.DropTable(
                name: "MessageReactions",
                schema: "коммуникации");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ReplyToMessage_id",
                schema: "коммуникации",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                schema: "коммуникации",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "MentionedUserIds",
                schema: "коммуникации",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessage_id",
                schema: "коммуникации",
                table: "ChatMessages");
        }
    }
}
