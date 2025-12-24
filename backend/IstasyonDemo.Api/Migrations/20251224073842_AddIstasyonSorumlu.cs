using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIstasyonSorumlu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SorumluId",
                table: "Istasyonlar",
                type: "integer",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Users_SorumluId1",
                table: "Istasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_SorumluId1",
                table: "Istasyonlar");

            migrationBuilder.DropColumn(
                name: "SorumluId",
                table: "Istasyonlar");

            migrationBuilder.DropColumn(
                name: "SorumluId1",
                table: "Istasyonlar");
        }
    }
}
