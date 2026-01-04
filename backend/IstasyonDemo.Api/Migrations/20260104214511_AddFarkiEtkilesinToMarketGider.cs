using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFarkiEtkilesinToMarketGider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FarkiEtkilesin",
                table: "MarketGiderler",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FarkiEtkilesin",
                table: "MarketGiderler");
        }
    }
}
