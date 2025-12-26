using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIstasyonApiKeyAndOtomatikDosyaIstasyonId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IstasyonId",
                table: "OtomatikDosyalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Istasyonlar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OtomatikDosyalar_IstasyonId",
                table: "OtomatikDosyalar",
                column: "IstasyonId");

            migrationBuilder.AddForeignKey(
                name: "FK_OtomatikDosyalar_Istasyonlar_IstasyonId",
                table: "OtomatikDosyalar",
                column: "IstasyonId",
                principalTable: "Istasyonlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OtomatikDosyalar_Istasyonlar_IstasyonId",
                table: "OtomatikDosyalar");

            migrationBuilder.DropIndex(
                name: "IX_OtomatikDosyalar_IstasyonId",
                table: "OtomatikDosyalar");

            migrationBuilder.DropColumn(
                name: "IstasyonId",
                table: "OtomatikDosyalar");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Istasyonlar");
        }
    }
}
