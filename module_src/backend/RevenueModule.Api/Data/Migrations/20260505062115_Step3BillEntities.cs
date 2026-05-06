using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RevenueModule.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step3BillEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuyerId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PendingAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Subtotal = table.Column<long>(type: "bigint", nullable: false),
                    VoucherThuongAmount = table.Column<long>(type: "bigint", nullable: false),
                    BaseBeforeVip = table.Column<long>(type: "bigint", nullable: false),
                    VipPointUsed = table.Column<int>(type: "integer", nullable: false),
                    VipDiscountVnd = table.Column<long>(type: "bigint", nullable: false),
                    Payable = table.Column<long>(type: "bigint", nullable: false),
                    VipPointEarned = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.Id);
                    table.CheckConstraint("CK_Bills_Payable_NonNegative", "\"Payable\" >= 0");
                    table.ForeignKey(
                        name: "FK_Bills_Buyers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Buyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceVnd = table.Column<long>(type: "bigint", nullable: false),
                    LineTotalVnd = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillLines", x => x.Id);
                    table.CheckConstraint("CK_BillLines_Qty_Positive", "\"Qty\" > 0");
                    table.CheckConstraint("CK_BillLines_UnitPrice_NonNegative", "\"UnitPriceVnd\" >= 0");
                    table.ForeignKey(
                        name: "FK_BillLines_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillLines_BillId",
                table: "BillLines",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BuyerId",
                table: "Bills",
                column: "BuyerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillLines");

            migrationBuilder.DropTable(
                name: "Bills");
        }
    }
}
