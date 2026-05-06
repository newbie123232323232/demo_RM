using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevenueModule.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DemoProductFkOnBillLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_BillLines_Products_ProductId",
                table: "BillLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillLines_Products_ProductId",
                table: "BillLines");
        }
    }
}
