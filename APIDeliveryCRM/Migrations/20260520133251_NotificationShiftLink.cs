using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class NotificationShiftLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Shift_id",
                schema: "коммуникации",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Shift_id",
                schema: "коммуникации",
                table: "Notifications",
                column: "Shift_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_CourierShifts_Shift_id",
                schema: "коммуникации",
                table: "Notifications",
                column: "Shift_id",
                principalSchema: "логистика_и_смены",
                principalTable: "CourierShifts",
                principalColumn: "ID_Shift",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_CourierShifts_Shift_id",
                schema: "коммуникации",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Shift_id",
                schema: "коммуникации",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Shift_id",
                schema: "коммуникации",
                table: "Notifications");
        }
    }
}
