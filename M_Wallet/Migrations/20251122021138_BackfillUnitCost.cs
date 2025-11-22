using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M_Wallet.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUnitCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE ""TransactionItems""
                  SET ""UnitCost"" = ""Products"".""CostPrice""
                  FROM ""Products""
                  WHERE ""TransactionItems"".""ProductId"" = ""Products"".""Id""
                  AND ""TransactionItems"".""UnitCost"" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
