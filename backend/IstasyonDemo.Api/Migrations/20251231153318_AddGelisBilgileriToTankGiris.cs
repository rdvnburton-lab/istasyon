using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGelisBilgileriToTankGiris : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GelisYontemi",
                table: "TankGirisler",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Plaka",
                table: "TankGirisler",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UrunGirisTarihi",
                table: "TankGirisler",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GelisYontemi",
                table: "TankGirisler");

            migrationBuilder.DropColumn(
                name: "Plaka",
                table: "TankGirisler");

            migrationBuilder.DropColumn(
                name: "UrunGirisTarihi",
                table: "TankGirisler");
        }
    }
}
