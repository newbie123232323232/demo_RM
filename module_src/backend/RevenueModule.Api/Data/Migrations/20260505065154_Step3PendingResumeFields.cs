using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevenueModule.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step3PendingResumeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherType",
                table: "Bills",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherValue",
                table: "Bills",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoucherType",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "VoucherValue",
                table: "Bills");
        }
    }
}
