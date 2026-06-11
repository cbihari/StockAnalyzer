using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAnalyzer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionModelMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "model_accuracy",
                table: "Predictions",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_status",
                table: "Predictions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "model_accuracy",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "model_status",
                table: "Predictions");
        }
    }
}
