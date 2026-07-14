using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skanly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatbotTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatbotConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RelatedPropertyId = table.Column<int>(type: "int", nullable: true),
                    ConversationTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatbotConversations_Properties_RelatedPropertyId",
                        column: x => x.RelatedPropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatbotMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedIntent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsInstantAnswer = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatbotMessages_ChatbotConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatbotConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotConversations_RelatedPropertyId",
                table: "ChatbotConversations",
                column: "RelatedPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotConversations_UserId_LastMessageAt",
                table: "ChatbotConversations",
                columns: new[] { "UserId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotMessages_ConversationId_SentAt",
                table: "ChatbotMessages",
                columns: new[] { "ConversationId", "SentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatbotMessages");

            migrationBuilder.DropTable(
                name: "ChatbotConversations");
        }
    }
}
