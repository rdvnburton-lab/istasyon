using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVardiyaRaporArsivTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Arsivlendi",
                table: "Vardiyalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "FiloToplam",
                table: "Vardiyalar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GiderToplam",
                table: "Vardiyalar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtomasyonToplam",
                table: "Vardiyalar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RaporArsivId",
                table: "Vardiyalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TahsilatToplam",
                table: "Vardiyalar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "VardiyaRaporArsivleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VardiyaId = table.Column<int>(type: "integer", nullable: false),
                    IstasyonId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SistemToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TahsilatToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FiloToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GiderToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Fark = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FarkYuzde = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Durum = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KarsilastirmaRaporuJson = table.Column<string>(type: "jsonb", nullable: true),
                    FarkRaporuJson = table.Column<string>(type: "jsonb", nullable: true),
                    PompaSatisRaporuJson = table.Column<string>(type: "jsonb", nullable: true),
                    TahsilatDetayJson = table.Column<string>(type: "jsonb", nullable: true),
                    GiderRaporuJson = table.Column<string>(type: "jsonb", nullable: true),
                    KarsilastirmaPdfIcerik = table.Column<byte[]>(type: "bytea", nullable: true),
                    FarkRaporuPdfIcerik = table.Column<byte[]>(type: "bytea", nullable: true),
                    VardiyaOzetPdfIcerik = table.Column<byte[]>(type: "bytea", nullable: true),
                    OnaylayanId = table.Column<int>(type: "integer", nullable: true),
                    OnaylayanAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SorumluId = table.Column<int>(type: "integer", nullable: true),
                    SorumluAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VardiyaRaporArsivleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VardiyaRaporArsivleri_Istasyonlar_IstasyonId",
                        column: x => x.IstasyonId,
                        principalTable: "Istasyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vardiyalar_RaporArsivId",
                table: "Vardiyalar",
                column: "RaporArsivId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VardiyaRaporArsiv_IstasyonTarih",
                table: "VardiyaRaporArsivleri",
                columns: new[] { "IstasyonId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_VardiyaRaporArsiv_Tarih",
                table: "VardiyaRaporArsivleri",
                column: "Tarih",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_VardiyaRaporArsiv_VardiyaId_Unique",
                table: "VardiyaRaporArsivleri",
                column: "VardiyaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vardiyalar_VardiyaRaporArsivleri_RaporArsivId",
                table: "Vardiyalar",
                column: "RaporArsivId",
                principalTable: "VardiyaRaporArsivleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vardiyalar_VardiyaRaporArsivleri_RaporArsivId",
                table: "Vardiyalar");

            migrationBuilder.DropTable(
                name: "VardiyaRaporArsivleri");

            migrationBuilder.DropIndex(
                name: "IX_Vardiyalar_RaporArsivId",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "Arsivlendi",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "FiloToplam",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "GiderToplam",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "OtomasyonToplam",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "RaporArsivId",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "TahsilatToplam",
                table: "Vardiyalar");
        }
    }
}
