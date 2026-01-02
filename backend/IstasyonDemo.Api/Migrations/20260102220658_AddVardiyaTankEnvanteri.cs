using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVardiyaTankEnvanteri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VardiyaTankEnvanterleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VardiyaId = table.Column<int>(type: "integer", nullable: false),
                    TankNo = table.Column<int>(type: "integer", nullable: false),
                    TankAdi = table.Column<string>(type: "text", nullable: false),
                    YakitTipi = table.Column<string>(type: "text", nullable: false),
                    BaslangicStok = table.Column<decimal>(type: "numeric", nullable: false),
                    BitisStok = table.Column<decimal>(type: "numeric", nullable: false),
                    SatilanMiktar = table.Column<decimal>(type: "numeric", nullable: false),
                    SevkiyatMiktar = table.Column<decimal>(type: "numeric", nullable: false),
                    BeklenenTuketim = table.Column<decimal>(type: "numeric", nullable: false),
                    FarkMiktar = table.Column<decimal>(type: "numeric", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VardiyaTankEnvanterleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VardiyaTankEnvanterleri_Vardiyalar_VardiyaId",
                        column: x => x.VardiyaId,
                        principalTable: "Vardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VardiyaTankEnvanterleri_VardiyaId",
                table: "VardiyaTankEnvanterleri",
                column: "VardiyaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VardiyaTankEnvanterleri");
        }
    }
}
