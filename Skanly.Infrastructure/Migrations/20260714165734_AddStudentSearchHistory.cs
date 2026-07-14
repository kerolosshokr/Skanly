using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skanly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentSearchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentSearchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UniversityId = table.Column<int>(type: "int", nullable: true),
                    AreaId = table.Column<int>(type: "int", nullable: true),
                    MinPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    MaxPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    PropertyType = table.Column<byte>(type: "tinyint", nullable: true),
                    SearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSearchHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentSearchHistories_StudentId_SearchedAt",
                table: "StudentSearchHistories",
                columns: new[] { "StudentId", "SearchedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentSearchHistories");
        }
    }
}
