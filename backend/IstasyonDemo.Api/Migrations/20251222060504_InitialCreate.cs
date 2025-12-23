using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Personeller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Soyad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personeller", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vardiyalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IstasyonId = table.Column<int>(type: "integer", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    PompaToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MarketToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GenelToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DosyaAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vardiyalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiloSatislar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VardiyaId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FiloKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Plaka = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    YakitTuru = table.Column<int>(type: "integer", nullable: false),
                    Litre = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PompaNo = table.Column<int>(type: "integer", nullable: false),
                    FisNo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiloSatislar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiloSatislar_Vardiyalar_VardiyaId",
                        column: x => x.VardiyaId,
                        principalTable: "Vardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtomasyonSatislar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VardiyaId = table.Column<int>(type: "integer", nullable: false),
                    PersonelId = table.Column<int>(type: "integer", nullable: true),
                    PersonelAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PersonelKeyId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PompaNo = table.Column<int>(type: "integer", nullable: false),
                    YakitTuru = table.Column<int>(type: "integer", nullable: false),
                    Litre = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SatisTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FisNo = table.Column<int>(type: "integer", nullable: true),
                    Plaka = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtomasyonSatislar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtomasyonSatislar_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OtomasyonSatislar_Vardiyalar_VardiyaId",
                        column: x => x.VardiyaId,
                        principalTable: "Vardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiloSatislar_VardiyaId",
                table: "FiloSatislar",
                column: "VardiyaId");

            migrationBuilder.CreateIndex(
                name: "IX_OtomasyonSatislar_PersonelId",
                table: "OtomasyonSatislar",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_OtomasyonSatislar_VardiyaId",
                table: "OtomasyonSatislar",
                column: "VardiyaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiloSatislar");

            migrationBuilder.DropTable(
                name: "OtomasyonSatislar");

            migrationBuilder.DropTable(
                name: "Personeller");

            migrationBuilder.DropTable(
                name: "Vardiyalar");
        }
    }
}
