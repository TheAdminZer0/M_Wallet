using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M_Wallet.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Password", "Username" },
                values: new object[] { "123", "aziz" });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "IsActive", "Name", "Passcode", "Password", "Role", "Username" },
                values: new object[] { 2, true, "POS Terminal", null, "pos", "System", "pos" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DropColumn(
                name: "Password",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Employees");
        }
    }
}
