using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstasyonDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIstasyonSorumlusuRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // İstasyon Sorumlusu rolünü ekle (eğer yoksa)
            migrationBuilder.Sql(@"
                INSERT INTO ""Roles"" (""Ad"", ""Aciklama"", ""IsSystemRole"")
                SELECT 'istasyon sorumlusu', 'İstasyon Sorumlusu', true
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Roles"" WHERE ""Ad"" = 'istasyon sorumlusu'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rolü geri al (eğer kullanılmıyorsa)
            migrationBuilder.Sql(@"
                DELETE FROM ""Roles"" 
                WHERE ""Ad"" = 'istasyon sorumlusu' 
                AND NOT EXISTS (
                    SELECT 1 FROM ""Users"" WHERE ""RoleId"" = (
                        SELECT ""Id"" FROM ""Roles"" WHERE ""Ad"" = 'istasyon sorumlusu'
                    )
                );
            ");
        }
    }
}
