using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaticPusulaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MobilOdeme",
                table: "Pusulalar");

            migrationBuilder.DropColumn(
                name: "ParoPuan",
                table: "Pusulalar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MobilOdeme",
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
        }
    }
}
