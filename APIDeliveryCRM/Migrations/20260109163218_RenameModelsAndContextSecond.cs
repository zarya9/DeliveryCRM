using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class RenameModelsAndContextSecond : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientProfiles_Payment_Methods_Preferred_payment_method_id",
                table: "ClientProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Payment_Methods_PaymentMethod_id",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payment_Methods",
                table: "Payment_Methods");

            migrationBuilder.RenameTable(
                name: "Payment_Methods",
                newName: "PaymentMethods");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentMethods",
                table: "PaymentMethods",
                column: "ID_PaymentMethod");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientProfiles_PaymentMethods_Preferred_payment_method_id",
                table: "ClientProfiles",
                column: "Preferred_payment_method_id",
                principalTable: "PaymentMethods",
                principalColumn: "ID_PaymentMethod",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentMethods_PaymentMethod_id",
                table: "Orders",
                column: "PaymentMethod_id",
                principalTable: "PaymentMethods",
                principalColumn: "ID_PaymentMethod",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientProfiles_PaymentMethods_Preferred_payment_method_id",
                table: "ClientProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentMethods_PaymentMethod_id",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentMethods",
                table: "PaymentMethods");

            migrationBuilder.RenameTable(
                name: "PaymentMethods",
                newName: "Payment_Methods");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payment_Methods",
                table: "Payment_Methods",
                column: "ID_PaymentMethod");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientProfiles_Payment_Methods_Preferred_payment_method_id",
                table: "ClientProfiles",
                column: "Preferred_payment_method_id",
                principalTable: "Payment_Methods",
                principalColumn: "ID_PaymentMethod",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Payment_Methods_PaymentMethod_id",
                table: "Orders",
                column: "PaymentMethod_id",
                principalTable: "Payment_Methods",
                principalColumn: "ID_PaymentMethod",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
