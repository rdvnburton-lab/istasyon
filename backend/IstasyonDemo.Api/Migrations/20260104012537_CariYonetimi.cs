using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class CariYonetimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CariKartlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IstasyonId = table.Column<int>(type: "integer", nullable: false),
                    Ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VergiDairesi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TCKN_VKN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Bakiye = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Limit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Kod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariKartlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CariKartlar_Istasyonlar_IstasyonId",
                        column: x => x.IstasyonId,
                        principalTable: "Istasyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CariHareketler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CariKartId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IslemTipi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BelgeNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VardiyaId = table.Column<int>(type: "integer", nullable: true),
                    OlusturanId = table.Column<int>(type: "integer", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariHareketler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CariHareketler_CariKartlar_CariKartId",
                        column: x => x.CariKartId,
                        principalTable: "CariKartlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CariHareketler_CariKartId",
                table: "CariHareketler",
                column: "CariKartId");

            migrationBuilder.CreateIndex(
                name: "IX_CariKartlar_IstasyonId",
                table: "CariKartlar",
                column: "IstasyonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CariHareketler");

            migrationBuilder.DropTable(
                name: "CariKartlar");
        }
    }
}
