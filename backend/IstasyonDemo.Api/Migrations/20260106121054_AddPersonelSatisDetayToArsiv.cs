using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonelSatisDetayToArsiv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VardiyaId1",
                table: "VardiyaTankEnvanterleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonelSatisDetayJson",
                table: "VardiyaRaporArsivleri",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VardiyaTankEnvanterleri_VardiyaId1",
                table: "VardiyaTankEnvanterleri",
                column: "VardiyaId1");

            migrationBuilder.AddForeignKey(
                name: "FK_VardiyaTankEnvanterleri_Vardiyalar_VardiyaId1",
                table: "VardiyaTankEnvanterleri",
                column: "VardiyaId1",
                principalTable: "Vardiyalar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VardiyaTankEnvanterleri_Vardiyalar_VardiyaId1",
                table: "VardiyaTankEnvanterleri");

            migrationBuilder.DropIndex(
                name: "IX_VardiyaTankEnvanterleri_VardiyaId1",
                table: "VardiyaTankEnvanterleri");

            migrationBuilder.DropColumn(
                name: "VardiyaId1",
                table: "VardiyaTankEnvanterleri");

            migrationBuilder.DropColumn(
                name: "PersonelSatisDetayJson",
                table: "VardiyaRaporArsivleri");
        }
    }
}
