using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPusulaVeresiye : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PusulaVeresiyeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PusulaId = table.Column<int>(type: "integer", nullable: false),
                    CariKartId = table.Column<int>(type: "integer", nullable: false),
                    Plaka = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    YakitCinsi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Litre = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PusulaVeresiyeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PusulaVeresiyeler_CariKartlar_CariKartId",
                        column: x => x.CariKartId,
                        principalTable: "CariKartlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PusulaVeresiyeler_Pusulalar_PusulaId",
                        column: x => x.PusulaId,
                        principalTable: "Pusulalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PusulaVeresiyeler_CariKartId",
                table: "PusulaVeresiyeler",
                column: "CariKartId");

            migrationBuilder.CreateIndex(
                name: "IX_PusulaVeresiyeler_PusulaId",
                table: "PusulaVeresiyeler",
                column: "PusulaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PusulaVeresiyeler");
        }
    }
}
