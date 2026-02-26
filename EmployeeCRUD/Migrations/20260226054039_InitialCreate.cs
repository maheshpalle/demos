using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EmployeeCRUD.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Salary = table.Column<decimal>(type: "TEXT", nullable: false),
                    DateOfJoining = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "DateOfJoining", "Department", "Email", "FirstName", "JobTitle", "LastName", "Salary" },
                values: new object[,]
                {
                    { 1, new DateTime(2022, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Engineering", "john.doe@example.com", "John", "Software Engineer", "Doe", 75000m },
                    { 2, new DateTime(2021, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Human Resources", "jane.smith@example.com", "Jane", "HR Manager", "Smith", 65000m },
                    { 3, new DateTime(2023, 3, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Finance", "bob.johnson@example.com", "Bob", "Financial Analyst", "Johnson", 70000m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
