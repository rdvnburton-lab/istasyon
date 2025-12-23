using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePersonelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ad",
                table: "Personeller");

            migrationBuilder.RenameColumn(
                name: "Soyad",
                table: "Personeller",
                newName: "OtomasyonAdi");

            migrationBuilder.AddColumn<string>(
                name: "AdSoyad",
                table: "Personeller",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Rol",
                table: "Personeller",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdSoyad",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Personeller");

            migrationBuilder.RenameColumn(
                name: "OtomasyonAdi",
                table: "Personeller",
                newName: "Soyad");

            migrationBuilder.AddColumn<string>(
                name: "Ad",
                table: "Personeller",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
