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

            migrationBuilder.InsertData(
                table: "People",
                columns: new[] { "Id", "CompletedDeliveries", "CreatedAt", "IsActive", "Name", "Passcode", "Password", "PhoneNumber", "Preferences", "Role", "Username" },
                values: new object[,]
                {
                    { 1, 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Aziz", "630125874", "123", null, null, "Admin", "aziz" },
                    { 2, 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "POS Terminal", null, "pos", null, null, "System", "pos" }
                });
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
