using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Personel> Personeller { get; set; }
        public DbSet<Vardiya> Vardiyalar { get; set; }
        public DbSet<OtomasyonSatis> OtomasyonSatislar { get; set; }
        public DbSet<FiloSatis> FiloSatislar { get; set; }
        public DbSet<Pusula> Pusulalar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // İlişkileri ve kısıtlamaları burada tanımlayabiliriz
            modelBuilder.Entity<OtomasyonSatis>()
                .HasOne(o => o.Vardiya)
                .WithMany(v => v.OtomasyonSatislar)
                .HasForeignKey(o => o.VardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FiloSatis>()
                .HasOne(f => f.Vardiya)
                .WithMany(v => v.FiloSatislar)
                .HasForeignKey(f => f.VardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pusula>()
                .HasOne(p => p.Vardiya)
                .WithMany(v => v.Pusulalar)
                .HasForeignKey(p => p.VardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            // PERFORMANCE INDEXES
            // Index on VardiyaId for faster lookups
            modelBuilder.Entity<OtomasyonSatis>()
                .HasIndex(o => o.VardiyaId)
                .HasDatabaseName("IX_OtomasyonSatislar_VardiyaId");

            modelBuilder.Entity<FiloSatis>()
                .HasIndex(f => f.VardiyaId)
                .HasDatabaseName("IX_FiloSatislar_VardiyaId");

            modelBuilder.Entity<Pusula>()
                .HasIndex(p => p.VardiyaId)
                .HasDatabaseName("IX_Pusulalar_VardiyaId");

            // Index on Vardiya.BaslangicTarihi for faster ordering
            modelBuilder.Entity<Vardiya>()
                .HasIndex(v => v.BaslangicTarihi)
                .HasDatabaseName("IX_Vardiyalar_BaslangicTarihi")
                .IsDescending();

            // Index on Vardiya.Durum for filtered queries
            modelBuilder.Entity<Vardiya>()
                .HasIndex(v => v.Durum)
                .HasDatabaseName("IX_Vardiyalar_Durum");

            // Index on PersonelId for faster personnel reports
            modelBuilder.Entity<OtomasyonSatis>()
                .HasIndex(o => o.PersonelId)
                .HasDatabaseName("IX_OtomasyonSatislar_PersonelId");

            modelBuilder.Entity<Pusula>()
                .HasIndex(p => p.PersonelId)
                .HasDatabaseName("IX_Pusulalar_PersonelId");
        }
    }
}
