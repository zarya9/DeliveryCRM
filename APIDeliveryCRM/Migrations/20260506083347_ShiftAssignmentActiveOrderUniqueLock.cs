using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class ShiftAssignmentActiveOrderUniqueLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftAssignments_Order_id",
                table: "ShiftAssignments");

            migrationBuilder.CreateIndex(
                name: "UX_ShiftAssignments_ActiveOrder",
                table: "ShiftAssignments",
                column: "Order_id",
                unique: true,
                filter: "\"ShiftPlan_id\" IS NOT NULL AND \"Status\" IN (1,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ShiftAssignments_ActiveOrder",
                table: "ShiftAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Order_id",
                table: "ShiftAssignments",
                column: "Order_id");
        }
    }
}
