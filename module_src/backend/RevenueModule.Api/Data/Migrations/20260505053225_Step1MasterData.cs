using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RevenueModule.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step1MasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buyers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    VipPoint = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buyers", x => x.Id);
                    table.CheckConstraint("CK_Buyers_VipPoint_NonNegative", "\"VipPoint\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    UnitPriceVnd = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_UnitPriceVnd_NonNegative", "\"UnitPriceVnd\" >= 0");
                });

            migrationBuilder.InsertData(
                table: "Buyers",
                columns: new[] { "Id", "Name", "VipPoint" },
                values: new object[,]
                {
                    { 1, "Buyer A", 3 },
                    { 2, "Buyer B", 7 },
                    { 3, "Buyer C", 12 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Name", "UnitPriceVnd" },
                values: new object[,]
                {
                    { 1, "Sổ tay mini", 45000L },
                    { 2, "Bình giữ nhiệt", 320000L },
                    { 3, "Tai nghe có dây", 290000L },
                    { 4, "Ghế công thái học", 2150000L },
                    { 5, "Màn hình 27 inch", 4790000L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Buyers");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
