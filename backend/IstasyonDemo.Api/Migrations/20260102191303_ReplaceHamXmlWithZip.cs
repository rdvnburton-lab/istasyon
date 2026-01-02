using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceHamXmlWithZip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HamXmlIcerik",
                table: "VardiyaXmlLoglari");

            migrationBuilder.AddColumn<byte[]>(
                name: "ZipDosyasi",
                table: "VardiyaXmlLoglari",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZipDosyasi",
                table: "VardiyaXmlLoglari");

            migrationBuilder.AddColumn<string>(
                name: "HamXmlIcerik",
                table: "VardiyaXmlLoglari",
                type: "text",
                nullable: true);
        }
    }
}
