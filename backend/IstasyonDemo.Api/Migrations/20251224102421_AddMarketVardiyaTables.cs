using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketVardiyaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketVardiyalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IstasyonId = table.Column<int>(type: "integer", nullable: false),
                    SorumluId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    ToplamSatisTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamTeslimatTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamFark = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ZRaporuTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ZRaporuNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OnaylayanId = table.Column<int>(type: "integer", nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RedNedeni = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketVardiyalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketVardiyalar_Istasyonlar_IstasyonId",
                        column: x => x.IstasyonId,
                        principalTable: "Istasyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketVardiyalar_Users_SorumluId",
                        column: x => x.SorumluId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketGelirler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MarketVardiyaId = table.Column<int>(type: "integer", nullable: false),
                    GelirTuru = table.Column<int>(type: "integer", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BelgeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketGelirler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketGelirler_MarketVardiyalar_MarketVardiyaId",
                        column: x => x.MarketVardiyaId,
                        principalTable: "MarketVardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketGiderler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MarketVardiyaId = table.Column<int>(type: "integer", nullable: false),
                    GiderTuru = table.Column<int>(type: "integer", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BelgeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketGiderler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketGiderler_MarketVardiyalar_MarketVardiyaId",
                        column: x => x.MarketVardiyaId,
                        principalTable: "MarketVardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketTahsilatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MarketVardiyaId = table.Column<int>(type: "integer", nullable: false),
                    PersonelId = table.Column<int>(type: "integer", nullable: false),
                    Nakit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KrediKarti = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParoPuan = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Toplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketTahsilatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketTahsilatlar_MarketVardiyalar_MarketVardiyaId",
                        column: x => x.MarketVardiyaId,
                        principalTable: "MarketVardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketTahsilatlar_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketZRaporlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MarketVardiyaId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GenelToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Kdv0 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Kdv1 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Kdv10 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Kdv20 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvHaricToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketZRaporlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketZRaporlari_MarketVardiyalar_MarketVardiyaId",
                        column: x => x.MarketVardiyaId,
                        principalTable: "MarketVardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketGelirler_MarketVardiyaId",
                table: "MarketGelirler",
                column: "MarketVardiyaId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketGiderler_MarketVardiyaId",
                table: "MarketGiderler",
                column: "MarketVardiyaId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTahsilatlar_MarketVardiyaId",
                table: "MarketTahsilatlar",
                column: "MarketVardiyaId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTahsilatlar_PersonelId",
                table: "MarketTahsilatlar",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketVardiyalar_IstasyonId",
                table: "MarketVardiyalar",
                column: "IstasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketVardiyalar_SorumluId",
                table: "MarketVardiyalar",
                column: "SorumluId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketVardiyalar_Tarih",
                table: "MarketVardiyalar",
                column: "Tarih",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_MarketZRaporlari_MarketVardiyaId",
                table: "MarketZRaporlari",
                column: "MarketVardiyaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketGelirler");

            migrationBuilder.DropTable(
                name: "MarketGiderler");

            migrationBuilder.DropTable(
                name: "MarketTahsilatlar");

            migrationBuilder.DropTable(
                name: "MarketZRaporlari");

            migrationBuilder.DropTable(
                name: "MarketVardiyalar");
        }
    }
}
