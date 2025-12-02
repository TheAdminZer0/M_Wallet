using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace M_Wallet.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedingAndDropEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employee");

            // Use raw SQL to insert seeded users to avoid PK conflicts with existing Customers
            migrationBuilder.Sql(@"
                INSERT INTO ""People"" (""Name"", ""Role"", ""Username"", ""Password"", ""Passcode"", ""IsActive"", ""CreatedAt"", ""CompletedDeliveries"")
                VALUES 
                ('Aziz', 'Admin', 'aziz', '123', '630125874', true, '2024-01-01 00:00:00+00', 0),
                ('POS Terminal', 'System', 'pos', 'pos', NULL, true, '2024-01-01 00:00:00+00', 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "People",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "People",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Passcode = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    Preferences = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Employee",
                columns: new[] { "Id", "IsActive", "Name", "Passcode", "Password", "Preferences", "Role", "Username" },
                values: new object[,]
                {
                    { 1, true, "Aziz", "630125874", "123", null, "Admin", "aziz" },
                    { 2, true, "POS Terminal", null, "pos", null, "System", "pos" }
                });
        }
    }
}
