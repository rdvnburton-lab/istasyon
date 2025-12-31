using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTankGiris : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TankGirisler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FaturaNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    YakitTuru = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Litre = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Kaydeden = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TankGirisler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TankGirisler_Tarih",
                table: "TankGirisler",
                column: "Tarih",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TankGirisler");
        }
    }
}
