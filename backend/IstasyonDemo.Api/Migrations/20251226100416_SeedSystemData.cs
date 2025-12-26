using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed Admin User
            // Hash: $2a$11$hJPe6ezWYIoDT7CLZrgpIOY6iYWumP62WFqGwzKrC1ztCo0Z5ELIi (admin123)
            migrationBuilder.Sql(@"
                INSERT INTO ""Users"" (""Username"", ""PasswordHash"", ""RoleId"", ""AdSoyad"", ""Telefon"", ""LastActivity"", ""CreatedAt"") 
                SELECT 'admin', '$2a$11$hJPe6ezWYIoDT7CLZrgpIOY6iYWumP62WFqGwzKrC1ztCo0Z5ELIi', ""Id"", 'Sistem Yöneticisi', '', NOW(), NOW()
                FROM ""Roles"" WHERE ""Ad"" = 'admin'
                AND NOT EXISTS (SELECT 1 FROM ""Users"" WHERE ""Username"" = 'admin');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
