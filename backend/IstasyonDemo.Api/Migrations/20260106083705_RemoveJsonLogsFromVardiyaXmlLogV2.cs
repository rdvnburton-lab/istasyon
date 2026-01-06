using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJsonLogsFromVardiyaXmlLogV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PumpDetailsJson",
                table: "VardiyaXmlLoglari");

            migrationBuilder.DropColumn(
                name: "SaleDetailsJson",
                table: "VardiyaXmlLoglari");

            migrationBuilder.DropColumn(
                name: "TankDetailsJson",
                table: "VardiyaXmlLoglari");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PumpDetailsJson",
                table: "VardiyaXmlLoglari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaleDetailsJson",
                table: "VardiyaXmlLoglari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TankDetailsJson",
                table: "VardiyaXmlLoglari",
                type: "text",
                nullable: true);
        }
    }
}
