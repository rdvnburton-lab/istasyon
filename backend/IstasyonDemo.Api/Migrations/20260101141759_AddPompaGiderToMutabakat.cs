using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPompaGiderToMutabakat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PusulaTuru",
                table: "Pusulalar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PompaGiderler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VardiyaId = table.Column<int>(type: "integer", nullable: false),
                    GiderTuru = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BelgeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PompaGiderler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PompaGiderler_Vardiyalar_VardiyaId",
                        column: x => x.VardiyaId,
                        principalTable: "Vardiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PompaGiderler_VardiyaId",
                table: "PompaGiderler",
                column: "VardiyaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PompaGiderler");

            migrationBuilder.DropColumn(
                name: "PusulaTuru",
                table: "Pusulalar");
        }
    }
}
