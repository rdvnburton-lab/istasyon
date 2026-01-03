using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFullXmlFieldsToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiloAdi",
                table: "OtomasyonSatislar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "IndirimTutar",
                table: "OtomasyonSatislar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KazanilanPara",
                table: "OtomasyonSatislar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "KazanilanPuan",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kilometre",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MotorSaati",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OdemeTuru",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PuanKullanimi",
                table: "OtomasyonSatislar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SadakatKartNo",
                table: "OtomasyonSatislar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SadakatKartTipi",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SatisTuru",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TabancaNo",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TagNr",
                table: "OtomasyonSatislar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TamBirimFiyat",
                table: "OtomasyonSatislar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "YazarKasaFisNo",
                table: "OtomasyonSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "YazarKasaPlaka",
                table: "OtomasyonSatislar",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FiloAdi",
                table: "FiloSatislar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "IndirimTutar",
                table: "FiloSatislar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KazanilanPara",
                table: "FiloSatislar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "KazanilanPuan",
                table: "FiloSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kilometre",
                table: "FiloSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MotorSaati",
                table: "FiloSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OdemeTuru",
                table: "FiloSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PuanKullanimi",
                table: "FiloSatislar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SadakatKartNo",
                table: "FiloSatislar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SadakatKartTipi",
                table: "FiloSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SatisTuru",
                table: "FiloSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TabancaNo",
                table: "FiloSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TagNr",
                table: "FiloSatislar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TamBirimFiyat",
                table: "FiloSatislar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "YazarKasaFisNo",
                table: "FiloSatislar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "YazarKasaPlaka",
                table: "FiloSatislar",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiloAdi",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "IndirimTutar",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "KazanilanPara",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "KazanilanPuan",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "Kilometre",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "MotorSaati",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "OdemeTuru",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "PuanKullanimi",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "SadakatKartNo",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "SadakatKartTipi",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "SatisTuru",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "TabancaNo",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "TagNr",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "TamBirimFiyat",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "YazarKasaFisNo",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "YazarKasaPlaka",
                table: "OtomasyonSatislar");

            migrationBuilder.DropColumn(
                name: "FiloAdi",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "IndirimTutar",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "KazanilanPara",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "KazanilanPuan",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "Kilometre",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "MotorSaati",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "OdemeTuru",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "PuanKullanimi",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "SadakatKartNo",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "SadakatKartTipi",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "SatisTuru",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "TabancaNo",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "TagNr",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "TamBirimFiyat",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "YazarKasaFisNo",
                table: "FiloSatislar");

            migrationBuilder.DropColumn(
                name: "YazarKasaPlaka",
                table: "FiloSatislar");
        }
    }
}
