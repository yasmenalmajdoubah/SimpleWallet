using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleWallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitPockets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_Balance_NonNegative",
                table: "Wallets");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "Wallets",
                newName: "SpendingBalance");

            migrationBuilder.AddColumn<decimal>(
                name: "SavingsBalance",
                table: "Wallets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSalaryAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryAmount",
                table: "Users",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Pocket",
                table: "LedgerEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE [LedgerEntries] SET [Pocket] = 1 WHERE [Pocket] = 0;");

            migrationBuilder.CreateTable(
                name: "BankCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HolderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    ExpiryMonth = table.Column<int>(type: "int", nullable: false),
                    ExpiryYear = table.Column<int>(type: "int", nullable: false),
                    IsFrozen = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankCards_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoneyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoneyRequests", x => x.Id);
                    table.CheckConstraint("CK_MoneyRequests_Amount_Positive", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_MoneyRequests_Users_PayerUserId",
                        column: x => x.PayerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MoneyRequests_Users_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_Savings_NonNegative",
                table: "Wallets",
                sql: "[SavingsBalance] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_Spending_NonNegative",
                table: "Wallets",
                sql: "[SpendingBalance] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Salary_NonNegative",
                table: "Users",
                sql: "[SalaryAmount] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_BankCards_WalletId",
                table: "BankCards",
                column: "WalletId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequests_PayerUserId_Status",
                table: "MoneyRequests",
                columns: new[] { "PayerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequests_RequesterUserId_Status",
                table: "MoneyRequests",
                columns: new[] { "RequesterUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankCards");

            migrationBuilder.DropTable(
                name: "MoneyRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_Savings_NonNegative",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_Spending_NonNegative",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Salary_NonNegative",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SavingsBalance",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "LastSalaryAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SalaryAmount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Pocket",
                table: "LedgerEntries");

            migrationBuilder.RenameColumn(
                name: "SpendingBalance",
                table: "Wallets",
                newName: "Balance");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_Balance_NonNegative",
                table: "Wallets",
                sql: "[Balance] >= 0");
        }
    }
}
