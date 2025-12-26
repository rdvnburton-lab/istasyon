using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SplitIstasyonSorumlulari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Users_SorumluId",
                table: "Istasyonlar");

            migrationBuilder.RenameColumn(
                name: "SorumluId",
                table: "Istasyonlar",
                newName: "VardiyaSorumluId");

            migrationBuilder.RenameIndex(
                name: "IX_Istasyonlar_SorumluId",
                table: "Istasyonlar",
                newName: "IX_Istasyonlar_VardiyaSorumluId");

            migrationBuilder.AddColumn<int>(
                name: "IstasyonSorumluId",
                table: "Istasyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarketSorumluId",
                table: "Istasyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_IstasyonSorumluId",
                table: "Istasyonlar",
                column: "IstasyonSorumluId");

            migrationBuilder.CreateIndex(
                name: "IX_Istasyonlar_MarketSorumluId",
                table: "Istasyonlar",
                column: "MarketSorumluId");

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Users_IstasyonSorumluId",
                table: "Istasyonlar",
                column: "IstasyonSorumluId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Users_MarketSorumluId",
                table: "Istasyonlar",
                column: "MarketSorumluId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Users_VardiyaSorumluId",
                table: "Istasyonlar",
                column: "VardiyaSorumluId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Users_IstasyonSorumluId",
                table: "Istasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Users_MarketSorumluId",
                table: "Istasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Istasyonlar_Users_VardiyaSorumluId",
                table: "Istasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_IstasyonSorumluId",
                table: "Istasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Istasyonlar_MarketSorumluId",
                table: "Istasyonlar");

            migrationBuilder.DropColumn(
                name: "IstasyonSorumluId",
                table: "Istasyonlar");

            migrationBuilder.DropColumn(
                name: "MarketSorumluId",
                table: "Istasyonlar");

            migrationBuilder.RenameColumn(
                name: "VardiyaSorumluId",
                table: "Istasyonlar",
                newName: "SorumluId");

            migrationBuilder.RenameIndex(
                name: "IX_Istasyonlar_VardiyaSorumluId",
                table: "Istasyonlar",
                newName: "IX_Istasyonlar_SorumluId");

            migrationBuilder.AddForeignKey(
                name: "FK_Istasyonlar_Users_SorumluId",
                table: "Istasyonlar",
                column: "SorumluId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
