using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OnayTarihi",
                table: "Vardiyalar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnaylayanAdi",
                table: "Vardiyalar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OnaylayanId",
                table: "Vardiyalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedNedeni",
                table: "Vardiyalar",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnayTarihi",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "OnaylayanAdi",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "OnaylayanId",
                table: "Vardiyalar");

            migrationBuilder.DropColumn(
                name: "RedNedeni",
                table: "Vardiyalar");
        }
    }
}
