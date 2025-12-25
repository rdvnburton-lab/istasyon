using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPusulaKrediKartiDetayAndFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Users_SorumluId1",
                table: "Istasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_SorumluId1",
                table: "Istasyonlar");

            migrationBuilder.DropColumn(
                name: "SorumluId1",
                table: "Istasyonlar");

            migrationBuilder.CreateTable(
                name: "PusulaKrediKartiDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PusulaId = table.Column<int>(type: "integer", nullable: false),
                    BankaAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PusulaKrediKartiDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PusulaKrediKartiDetaylari_Pusulalar_PusulaId",
                        column: x => x.PusulaId,
                        principalTable: "Pusulalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_SorumluId",
                table: "Istasyonlar",
                column: "SorumluId");

            migrationBuilder.CreateIndex(
                name: "IX_PusulaKrediKartiDetaylari_PusulaId",
                table: "PusulaKrediKartiDetaylari",
                column: "PusulaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Users_SorumluId",
                table: "Istasyonlar",
                column: "SorumluId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Users_SorumluId",
                table: "Istasyonlar");

            migrationBuilder.DropTable(
                name: "PusulaKrediKartiDetaylari");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_SorumluId",
                table: "Istasyonlar");

            migrationBuilder.AddColumn<int>(
                name: "SorumluId1",
                table: "Istasyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_SorumluId1",
                table: "Istasyonlar",
                column: "SorumluId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Users_SorumluId1",
                table: "Istasyonlar",
                column: "SorumluId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
