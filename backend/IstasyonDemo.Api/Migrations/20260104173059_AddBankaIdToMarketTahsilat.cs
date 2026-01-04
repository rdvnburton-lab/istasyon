using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBankaIdToMarketTahsilat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankaId",
                table: "MarketTahsilatlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketTahsilatlar_BankaId",
                table: "MarketTahsilatlar",
                column: "BankaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MarketTahsilatlar_SystemDefinitions_BankaId",
                table: "MarketTahsilatlar",
                column: "BankaId",
                principalTable: "SystemDefinitions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MarketTahsilatlar_SystemDefinitions_BankaId",
                table: "MarketTahsilatlar");

            migrationBuilder.DropIndex(
                name: "IX_MarketTahsilatlar_BankaId",
                table: "MarketTahsilatlar");

            migrationBuilder.DropColumn(
                name: "BankaId",
                table: "MarketTahsilatlar");
        }
    }
}
