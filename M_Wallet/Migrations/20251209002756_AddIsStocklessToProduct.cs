using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M_Wallet.Migrations
{
    /// <inheritdoc />
    public partial class AddIsStocklessToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStockless",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStockless",
                table: "Products");
        }
    }
}
