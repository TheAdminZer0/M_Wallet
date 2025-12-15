using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M_Wallet.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDelivery",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_DriverId",
                table: "Transactions",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_People_DriverId",
                table: "Transactions",
                column: "DriverId",
                principalTable: "People",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_People_DriverId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_DriverId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsDelivery",
                table: "Transactions");
        }
    }
}
