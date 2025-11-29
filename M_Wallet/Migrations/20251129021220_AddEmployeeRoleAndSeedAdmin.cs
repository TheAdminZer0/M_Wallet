using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M_Wallet.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeRoleAndSeedAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                INSERT INTO ""Employees"" (""Id"", ""IsActive"", ""Name"", ""Passcode"", ""Role"")
                VALUES (1, TRUE, 'Aziz', '630125874', 'Admin')
                ON CONFLICT (""Id"") DO UPDATE 
                SET ""Name"" = 'Aziz', 
                    ""Passcode"" = '630125874', 
                    ""Role"" = 'Admin',
                    ""IsActive"" = TRUE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Employees");
        }
    }
}
