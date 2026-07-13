using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skanly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceUrlToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvidenceUrl",
                table: "Reports",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvidenceUrl",
                table: "Reports");
        }
    }
}
