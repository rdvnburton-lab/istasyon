using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            // Seed initial roles
            migrationBuilder.Sql("INSERT INTO \"Roles\" (\"Ad\", \"Aciklama\", \"IsSystemRole\") VALUES ('admin', 'Sistem Yöneticisi', true);");
            migrationBuilder.Sql("INSERT INTO \"Roles\" (\"Ad\", \"Aciklama\", \"IsSystemRole\") VALUES ('patron', 'İstasyon Sahibi', true);");
            migrationBuilder.Sql("INSERT INTO \"Roles\" (\"Ad\", \"Aciklama\", \"IsSystemRole\") VALUES ('vardiya sorumlusu', 'Vardiya Sorumlusu', true);");
            migrationBuilder.Sql("INSERT INTO \"Roles\" (\"Ad\", \"Aciklama\", \"IsSystemRole\") VALUES ('market sorumlusu', 'Market Sorumlusu', true);");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Update existing users to have a valid RoleId based on their old string Role column
            migrationBuilder.Sql("UPDATE \"Users\" SET \"RoleId\" = (SELECT \"Id\" FROM \"Roles\" WHERE \"Ad\" = 'admin') WHERE \"Role\" = 'admin';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"RoleId\" = (SELECT \"Id\" FROM \"Roles\" WHERE \"Ad\" = 'patron') WHERE \"Role\" = 'patron';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"RoleId\" = (SELECT \"Id\" FROM \"Roles\" WHERE \"Ad\" = 'vardiya sorumlusu') WHERE \"Role\" = 'vardiya sorumlusu' OR \"Role\" = 'vardiya';");
            
            // Fallback for any other roles
            migrationBuilder.Sql("UPDATE \"Users\" SET \"RoleId\" = (SELECT \"Id\" FROM \"Roles\" WHERE \"Ad\" = 'vardiya sorumlusu') WHERE \"RoleId\" = 0;");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
