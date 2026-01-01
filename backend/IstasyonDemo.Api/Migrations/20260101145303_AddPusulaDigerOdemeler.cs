using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPusulaDigerOdemeler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PusulaDigerOdemeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PusulaId = table.Column<int>(type: "integer", nullable: false),
                    TurKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TurAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PusulaDigerOdemeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PusulaDigerOdemeleri_Pusulalar_PusulaId",
                        column: x => x.PusulaId,
                        principalTable: "Pusulalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PusulaDigerOdemeleri_PusulaId",
                table: "PusulaDigerOdemeleri",
                column: "PusulaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PusulaDigerOdemeleri");
        }
    }
}
