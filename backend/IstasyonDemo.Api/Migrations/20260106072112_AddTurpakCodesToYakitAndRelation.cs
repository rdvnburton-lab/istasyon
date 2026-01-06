using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTurpakCodesToYakitAndRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TurpakUrunKodu",
                table: "Yakitlar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YakitId",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YakitId",
                table: "FiloSatislar",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Yakitlar",
                keyColumn: "Id",
                keyValue: 1,
                column: "TurpakUrunKodu",
                value: null);

            migrationBuilder.UpdateData(
                table: "Yakitlar",
                keyColumn: "Id",
                keyValue: 2,
                column: "TurpakUrunKodu",
                value: null);

            migrationBuilder.UpdateData(
                table: "Yakitlar",
                keyColumn: "Id",
                keyValue: 3,
                column: "TurpakUrunKodu",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_OtomasyonSatislar_YakitId",
                table: "OtomasyonSatislar",
                column: "YakitId");

            migrationBuilder.CreateIndex(
                name: "IX_FiloSatislar_YakitId",
                table: "FiloSatislar",
                column: "YakitId");

            migrationBuilder.AddForeignKey(
                name: "FK_FiloSatislar_Yakitlar_YakitId",
                table: "FiloSatislar",
                column: "YakitId",
                principalTable: "Yakitlar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OtomasyonSatislar_Yakitlar_YakitId",
                table: "OtomasyonSatislar",
                column: "YakitId",
                principalTable: "Yakitlar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiloSatislar_Yakitlar_YakitId",
                table: "FiloSatislar");

            migrationBuilder.DropForeignKey(
                name: "FK_OtomasyonSatislar_Yakitlar_YakitId",
                table: "OtomasyonSatislar");

            migrationBuilder.DropIndex(
                name: "IX_OtomasyonSatislar_YakitId",
                table: "OtomasyonSatislar");

            migrationBuilder.DropIndex(
                name: "IX_FiloSatislar_YakitId",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "TurpakUrunKodu",
                table: "Yakitlar");

            migrationBuilder.DropColumn(
                name: "YakitId",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "YakitId",
                table: "FiloSatislar");
        }
    }
}
