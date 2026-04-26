using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class BillingModuleMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    ID_SubscriptionPlan = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    MaxOrdersPerMonth = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.ID_SubscriptionPlan);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoices",
                columns: table => new
                {
                    ID_BillingInvoice = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionPlan_id = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeriodMonths = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoices", x => x.ID_BillingInvoice);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_SubscriptionPlans_SubscriptionPlan_id",
                        column: x => x.SubscriptionPlan_id,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "ID_SubscriptionPlan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanySubscriptions",
                columns: table => new
                {
                    ID_CompanySubscription = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company_id = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionPlan_id = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodStart_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySubscriptions", x => x.ID_CompanySubscription);
                    table.ForeignKey(
                        name: "FK_CompanySubscriptions_Companies_Company_id",
                        column: x => x.Company_id,
                        principalTable: "Companies",
                        principalColumn: "ID_Company",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanySubscriptions_SubscriptionPlans_SubscriptionPlan_id",
                        column: x => x.SubscriptionPlan_id,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "ID_SubscriptionPlan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    ID_PaymentTransaction = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingInvoice_id = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Succeeded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.ID_PaymentTransaction);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_BillingInvoices_BillingInvoice_id",
                        column: x => x.BillingInvoice_id,
                        principalTable: "BillingInvoices",
                        principalColumn: "ID_BillingInvoice",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_Company_id",
                table: "BillingInvoices",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_SubscriptionPlan_id",
                table: "BillingInvoices",
                column: "SubscriptionPlan_id");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscriptions_Company_id",
                table: "CompanySubscriptions",
                column: "Company_id");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscriptions_SubscriptionPlan_id",
                table: "CompanySubscriptions",
                column: "SubscriptionPlan_id");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_BillingInvoice_id",
                table: "PaymentTransactions",
                column: "BillingInvoice_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySubscriptions");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "BillingInvoices");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
