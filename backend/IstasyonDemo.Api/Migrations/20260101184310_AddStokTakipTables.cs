using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStokTakipTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AylikStokOzetleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    YakitId = table.Column<int>(type: "integer", nullable: false),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    DevirStok = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AyGiris = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AySatis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KalanStok = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    HesaplamaZamani = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kilitli = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AylikStokOzetleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AylikStokOzetleri_Yakitlar_YakitId",
                        column: x => x.YakitId,
                        principalTable: "Yakitlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaturaStokTakipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FaturaNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    YakitId = table.Column<int>(type: "integer", nullable: false),
                    FaturaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GirenMiktar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KalanMiktar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Tamamlandi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaturaStokTakipleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaturaStokTakipleri_Yakitlar_YakitId",
                        column: x => x.YakitId,
                        principalTable: "Yakitlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AylikStokOzeti_YakitYilAy",
                table: "AylikStokOzetleri",
                columns: new[] { "YakitId", "Yil", "Ay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaturaStokTakip_YakitFaturaTarihi",
                table: "FaturaStokTakipleri",
                columns: new[] { "YakitId", "FaturaTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AylikStokOzetleri");

            migrationBuilder.DropTable(
                name: "FaturaStokTakipleri");
        }
    }
}
