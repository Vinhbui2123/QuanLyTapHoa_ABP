using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternProject.Migrations
{
    /// <inheritdoc />
    public partial class Update_Invoices_And_Logs_Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "InvoiceItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "InvoiceItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RemainingQuantity",
                table: "InventoryLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "InvoiceItems");

            migrationBuilder.AlterColumn<int>(
                name: "RemainingQuantity",
                table: "InventoryLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
