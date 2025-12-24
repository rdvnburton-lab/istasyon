using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndDeletionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SilinmeTalebiNedeni",
                table: "Vardiyalar",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SilinmeTalebiOlusturanAdi",
                table: "Vardiyalar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SilinmeTalebiOlusturanId",
                table: "Vardiyalar",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SilinmeTalebiNedeni",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "SilinmeTalebiOlusturanAdi",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "SilinmeTalebiOlusturanId",
                table: "Vardiyalar");
        }
    }
}
