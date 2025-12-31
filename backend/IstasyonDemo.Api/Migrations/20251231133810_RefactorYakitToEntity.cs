using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorYakitToEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YakitTuru",
                table: "TankGirisler");

            migrationBuilder.AddColumn<int>(
                name: "YakitId",
                table: "TankGirisler",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Yakitlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OtomasyonUrunAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Sira = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yakitlar", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Yakitlar",
                columns: new[] { "Id", "Ad", "OtomasyonUrunAdi", "Renk", "Sira" },
                values: new object[,]
                {
                    { 1, "Motorin", "MOTORIN,DIZEL", "#F59E0B", 1 },
                    { 2, "Benzin (Kurşunsuz 95)", "BENZIN,KURŞUNSUZ,KURSUNSUZ", "#EF4444", 2 },
                    { 3, "LPG", "LPG,OTOGAZ", "#3B82F6", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TankGirisler_YakitId",
                table: "TankGirisler",
                column: "YakitId");

            migrationBuilder.AddForeignKey(
                name: "FK_TankGirisler_Yakitlar_YakitId",
                table: "TankGirisler",
                column: "YakitId",
                principalTable: "Yakitlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TankGirisler_Yakitlar_YakitId",
                table: "TankGirisler");

            migrationBuilder.DropTable(
                name: "Yakitlar");

            migrationBuilder.DropIndex(
                name: "IX_TankGirisler_YakitId",
                table: "TankGirisler");

            migrationBuilder.DropColumn(
                name: "YakitId",
                table: "TankGirisler");

            migrationBuilder.AddColumn<string>(
                name: "YakitTuru",
                table: "TankGirisler",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
