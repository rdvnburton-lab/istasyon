using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVardiyaPompaEndeks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VardiyaPompaEndeksleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VardiyaId = table.Column<int>(type: "integer", nullable: false),
                    PompaNo = table.Column<int>(type: "integer", nullable: false),
                    TabancaNo = table.Column<int>(type: "integer", nullable: false),
                    YakitTuru = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaslangicEndeks = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BitisEndeks = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VardiyaPompaEndeksleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VardiyaPompaEndeksleri_Vardiyalar_VardiyaId",
                        column: x => x.VardiyaId,
                        principalTable: "Vardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VardiyaPompaEndeksleri_VardiyaId",
                table: "VardiyaPompaEndeksleri",
                column: "VardiyaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VardiyaPompaEndeksleri");
        }
    }
}
