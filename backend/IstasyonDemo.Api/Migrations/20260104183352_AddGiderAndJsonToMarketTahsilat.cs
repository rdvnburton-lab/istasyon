using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGiderAndJsonToMarketTahsilat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Gider",
                table: "MarketTahsilatlar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "KrediKartiDetayJson",
                table: "MarketTahsilatlar",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gider",
                table: "MarketTahsilatlar");

            migrationBuilder.DropColumn(
                name: "KrediKartiDetayJson",
                table: "MarketTahsilatlar");
        }
    }
}
