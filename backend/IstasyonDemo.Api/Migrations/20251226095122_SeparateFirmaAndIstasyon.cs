using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeparateFirmaAndIstasyon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Firmalar table first
            migrationBuilder.CreateTable(
                name: "Firmalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Adres = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    PatronId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firmalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Firmalar_Users_PatronId",
                        column: x => x.PatronId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Firmalar_PatronId",
                table: "Firmalar",
                column: "PatronId");

            // 2. Insert default Firma (REMOVED for clean DB)
            // migrationBuilder.Sql("INSERT INTO \"Firmalar\" (\"Ad\", \"Aktif\") VALUES ('Varsayılan Firma', true);");

            // 3. Drop old columns
            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Istasyonlar_ParentIstasyonId",
                table: "Istasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Users_PatronId",
                table: "Istasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_ParentIstasyonId",
                table: "Istasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_PatronId",
                table: "Istasyonlar");

            migrationBuilder.DropColumn(
                name: "ParentIstasyonId",
                table: "Istasyonlar");

            migrationBuilder.DropColumn(
                name: "PatronId",
                table: "Istasyonlar");

            // 4. Add FirmaId without default value (assuming empty table)
            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "Istasyonlar",
                type: "integer",
                nullable: false,
                defaultValue: 0); // EF Core requires a default for non-nullable, but 0 is fine if table is empty

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_FirmaId",
                table: "Istasyonlar",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Firmalar_FirmaId",
                table: "Istasyonlar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Firmalar_FirmaId",
                table: "Istasyonlar");

            migrationBuilder.DropTable(
                name: "Firmalar");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_FirmaId",
                table: "Istasyonlar");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "Istasyonlar");

            migrationBuilder.AddColumn<int>(
                name: "ParentIstasyonId",
                table: "Istasyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatronId",
                table: "Istasyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_ParentIstasyonId",
                table: "Istasyonlar",
                column: "ParentIstasyonId");

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_PatronId",
                table: "Istasyonlar",
                column: "PatronId");

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Istasyonlar_ParentIstasyonId",
                table: "Istasyonlar",
                column: "ParentIstasyonId",
                principalTable: "Istasyonlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Users_PatronId",
                table: "Istasyonlar",
                column: "PatronId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
