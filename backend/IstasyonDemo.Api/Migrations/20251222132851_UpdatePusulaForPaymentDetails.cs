using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePusulaForPaymentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PusulaNo",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "Tarih",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "Turu",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "Tutar",
                table: "Pusulalar");

            migrationBuilder.AlterColumn<string>(
                name: "Aciklama",
                table: "Pusulalar",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KrediKarti",
                table: "Pusulalar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "KrediKartiDetay",
                table: "Pusulalar",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MobilOdeme",
                table: "Pusulalar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Nakit",
                table: "Pusulalar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ParoPuan",
                table: "Pusulalar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PersonelAdi",
                table: "Pusulalar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PersonelId",
                table: "Pusulalar",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KrediKarti",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "KrediKartiDetay",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "MobilOdeme",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "Nakit",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "ParoPuan",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "PersonelAdi",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "PersonelId",
                table: "Pusulalar");

            migrationBuilder.AlterColumn<string>(
                name: "Aciklama",
                table: "Pusulalar",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PusulaNo",
                table: "Pusulalar",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "Tarih",
                table: "Pusulalar",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Turu",
                table: "Pusulalar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Tutar",
                table: "Pusulalar",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
