using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAnalyzer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Accuracy = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    Precision = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    Recall = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    ConfusionMatrix = table.Column<string>(type: "jsonb", nullable: false),
                    FeatureImportance = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelMetrics_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prediction_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    prediction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    probability_up = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: true),
                    probability_down = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actual_result = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Predictions_Stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPrices_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModelMetrics_StockId_CreatedAt",
                table: "ModelMetrics",
                columns: new[] { "StockId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_stock_id_created_at",
                table: "Predictions",
                columns: new[] { "stock_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPrices_StockId_Date",
                table: "StockPrices",
                columns: new[] { "StockId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Ticker",
                table: "Stocks",
                column: "Ticker",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelMetrics");

            migrationBuilder.DropTable(
                name: "Predictions");

            migrationBuilder.DropTable(
                name: "StockPrices");

            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
