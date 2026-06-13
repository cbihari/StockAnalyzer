using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAnalyzer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAffiliateClicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffiliateClicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Broker = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Ticker = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    ClickedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateClicks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateClicks_Broker_ClickedAt",
                table: "AffiliateClicks",
                columns: new[] { "Broker", "ClickedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffiliateClicks");
        }
    }
}
