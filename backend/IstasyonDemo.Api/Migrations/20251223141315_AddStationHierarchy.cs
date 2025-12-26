using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IstasyonId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Istasyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Adres = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    ParentIstasyonId = table.Column<int>(type: "integer", nullable: true),
                    PatronId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Istasyonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Istasyonlar_Istasyonlar_ParentIstasyonId",
                        column: x => x.ParentIstasyonId,
                        principalTable: "Istasyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Istasyonlar_Users_PatronId",
                        column: x => x.PatronId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Insert Default Station manually to satisfy FK constraints (REMOVED for clean DB)
            // migrationBuilder.Sql("INSERT INTO \"Istasyonlar\" (\"Id\", \"Ad\", \"Aktif\") VALUES (1, 'Merkez İstasyon', true) ON CONFLICT (\"Id\") DO NOTHING;");

            migrationBuilder.CreateIndex(
                name: "IX_Vardiyalar_IstasyonId",
                table: "Vardiyalar",
                column: "IstasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IstasyonId",
                table: "Users",
                column: "IstasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_ParentIstasyonId",
                table: "Istasyonlar",
                column: "ParentIstasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_PatronId",
                table: "Istasyonlar",
                column: "PatronId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Istasyonlar_IstasyonId",
                table: "Users",
                column: "IstasyonId",
                principalTable: "Istasyonlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vardiyalar_Istasyonlar_IstasyonId",
                table: "Vardiyalar",
                column: "IstasyonId",
                principalTable: "Istasyonlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Istasyonlar_IstasyonId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Vardiyalar_Istasyonlar_IstasyonId",
                table: "Vardiyalar");

            migrationBuilder.DropTable(
                name: "Istasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Vardiyalar_IstasyonId",
                table: "Vardiyalar");

            migrationBuilder.DropIndex(
                name: "IX_Users_IstasyonId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IstasyonId",
                table: "Users");
        }
    }
}
