using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedImportUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IstasyonKodu",
                table: "Istasyonlar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DosyaYuklemeAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IstasyonId = table.Column<int>(type: "integer", nullable: true),
                    DosyaUzantisi = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HedefTablo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    XmlNodeMappingJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DosyaYuklemeAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DosyaYuklemeAyarlari_Istasyonlar_IstasyonId",
                        column: x => x.IstasyonId,
                        principalTable: "Istasyonlar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IstasyonAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IstasyonId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnitPriceDecimal = table.Column<int>(type: "integer", nullable: false),
                    AmountDecimal = table.Column<int>(type: "integer", nullable: false),
                    TotalDecimal = table.Column<int>(type: "integer", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IstasyonAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IstasyonAyarlari_Istasyonlar_IstasyonId",
                        column: x => x.IstasyonId,
                        principalTable: "Istasyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VardiyaXmlLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IstasyonId = table.Column<int>(type: "integer", nullable: false),
                    VardiyaId = table.Column<int>(type: "integer", nullable: true),
                    DosyaAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HamXmlIcerik = table.Column<string>(type: "text", nullable: true),
                    TankDetailsJson = table.Column<string>(type: "text", nullable: true),
                    PumpDetailsJson = table.Column<string>(type: "text", nullable: true),
                    SaleDetailsJson = table.Column<string>(type: "text", nullable: true),
                    YuklemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VardiyaXmlLoglari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VardiyaXmlLoglari_Istasyonlar_IstasyonId",
                        column: x => x.IstasyonId,
                        principalTable: "Istasyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VardiyaXmlLoglari_Vardiyalar_VardiyaId",
                        column: x => x.VardiyaId,
                        principalTable: "Vardiyalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DosyaYuklemeAyarlari_IstasyonId",
                table: "DosyaYuklemeAyarlari",
                column: "IstasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_IstasyonAyarlari_IstasyonId",
                table: "IstasyonAyarlari",
                column: "IstasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_VardiyaXmlLoglari_IstasyonId",
                table: "VardiyaXmlLoglari",
                column: "IstasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_VardiyaXmlLoglari_VardiyaId",
                table: "VardiyaXmlLoglari",
                column: "VardiyaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DosyaYuklemeAyarlari");

            migrationBuilder.DropTable(
                name: "IstasyonAyarlari");

            migrationBuilder.DropTable(
                name: "VardiyaXmlLoglari");

            migrationBuilder.DropColumn(
                name: "IstasyonKodu",
                table: "Istasyonlar");
        }
    }
}
