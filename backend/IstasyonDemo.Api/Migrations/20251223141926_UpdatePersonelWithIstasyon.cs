using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePersonelWithIstasyon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IstasyonId",
                table: "Personeller",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Personeller_IstasyonId",
                table: "Personeller",
                column: "IstasyonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personeller_Istasyonlar_IstasyonId",
                table: "Personeller",
                column: "IstasyonId",
                principalTable: "Istasyonlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personeller_Istasyonlar_IstasyonId",
                table: "Personeller");

            migrationBuilder.DropIndex(
                name: "IX_Personeller_IstasyonId",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "IstasyonId",
                table: "Personeller");
        }
    }
}
