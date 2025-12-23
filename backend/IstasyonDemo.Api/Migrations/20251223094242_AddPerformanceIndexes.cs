using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Vardiyalar_BaslangicTarihi",
                table: "Vardiyalar",
                column: "BaslangicTarihi",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Vardiyalar_Durum",
                table: "Vardiyalar",
                column: "Durum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vardiyalar_BaslangicTarihi",
                table: "Vardiyalar");

            migrationBuilder.DropIndex(
                name: "IX_Vardiyalar_Durum",
                table: "Vardiyalar");
        }
    }
}
