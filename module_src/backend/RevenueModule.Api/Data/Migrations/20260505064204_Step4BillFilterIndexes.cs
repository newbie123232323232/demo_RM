using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevenueModule.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step4BillFilterIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bills_BuyerId",
                table: "Bills");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BuyerId_PendingAtUtc",
                table: "Bills",
                columns: new[] { "BuyerId", "PendingAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_CompletedAtUtc",
                table: "Bills",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Payable",
                table: "Bills",
                column: "Payable");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_PendingAtUtc",
                table: "Bills",
                column: "PendingAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Status",
                table: "Bills",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BillLines_ProductId",
                table: "BillLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BillLines_ProductId_BillId",
                table: "BillLines",
                columns: new[] { "ProductId", "BillId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bills_BuyerId_PendingAtUtc",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_CompletedAtUtc",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_Payable",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_PendingAtUtc",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_Status",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_BillLines_ProductId",
                table: "BillLines");

            migrationBuilder.DropIndex(
                name: "IX_BillLines_ProductId_BillId",
                table: "BillLines");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BuyerId",
                table: "Bills",
                column: "BuyerId");
        }
    }
}
