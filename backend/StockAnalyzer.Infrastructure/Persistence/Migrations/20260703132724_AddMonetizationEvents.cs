using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAnalyzer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonetizationEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonetizationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    EventName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FeatureKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PlanKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonetizationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonetizationEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonetizationEvents_ClientId_OccurredAt",
                table: "MonetizationEvents",
                columns: new[] { "ClientId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MonetizationEvents_EventName_OccurredAt",
                table: "MonetizationEvents",
                columns: new[] { "EventName", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MonetizationEvents_UserId_OccurredAt",
                table: "MonetizationEvents",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonetizationEvents");
        }
    }
}
