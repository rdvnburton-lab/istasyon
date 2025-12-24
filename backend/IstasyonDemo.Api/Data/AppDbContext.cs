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
        public DbSet<User> Users { get; set; }
        public DbSet<Istasyon> Istasyonlar { get; set; }
        public DbSet<VardiyaLog> VardiyaLoglari { get; set; }
        public DbSet<MarketVardiya> MarketVardiyalar { get; set; }
        public DbSet<MarketZRaporu> MarketZRaporlari { get; set; }
        public DbSet<MarketTahsilat> MarketTahsilatlar { get; set; }
        public DbSet<MarketGider> MarketGiderler { get; set; }
        public DbSet<MarketGelir> MarketGelirler { get; set; }

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

            // Istasyon Relations
            modelBuilder.Entity<Istasyon>()
                .HasOne(i => i.ParentIstasyon)
                .WithMany(i => i.AltIstasyonlar)
                .HasForeignKey(i => i.ParentIstasyonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Istasyon>()
                .HasOne(i => i.Patron)
                .WithMany()
                .HasForeignKey(i => i.PatronId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Istasyon)
                .WithMany(i => i.Kullanicilar)
                .HasForeignKey(u => u.IstasyonId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Vardiya>()
                .HasOne(v => v.Istasyon)
                .WithMany(i => i.Vardiyalar)
                .HasForeignKey(v => v.IstasyonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Personel>()
                .HasOne(p => p.Istasyon)
                .WithMany(i => i.Calisanlar)
                .HasForeignKey(p => p.IstasyonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VardiyaLog>()
                .HasOne(vl => vl.Vardiya)
                .WithMany()
                .HasForeignKey(vl => vl.VardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index on VardiyaLog for faster queries
            modelBuilder.Entity<VardiyaLog>()
                .HasIndex(vl => vl.VardiyaId)
                .HasDatabaseName("IX_VardiyaLoglari_VardiyaId");

            modelBuilder.Entity<VardiyaLog>()
                .HasIndex(vl => vl.IslemTarihi)
                .HasDatabaseName("IX_VardiyaLoglari_IslemTarihi")
                .IsDescending();

            // Market Vardiya Relations
            modelBuilder.Entity<MarketVardiya>()
                .HasOne(m => m.Istasyon)
                .WithMany()
                .HasForeignKey(m => m.IstasyonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MarketVardiya>()
                .HasOne(m => m.Sorumlu)
                .WithMany()
                .HasForeignKey(m => m.SorumluId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MarketZRaporu>()
                .HasOne(z => z.MarketVardiya)
                .WithMany(m => m.ZRaporlari)
                .HasForeignKey(z => z.MarketVardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MarketTahsilat>()
                .HasOne(t => t.MarketVardiya)
                .WithMany(m => m.Tahsilatlar)
                .HasForeignKey(t => t.MarketVardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MarketTahsilat>()
                .HasOne(t => t.Personel)
                .WithMany()
                .HasForeignKey(t => t.PersonelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MarketGider>()
                .HasOne(g => g.MarketVardiya)
                .WithMany(m => m.Giderler)
                .HasForeignKey(g => g.MarketVardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MarketGelir>()
                .HasOne(g => g.MarketVardiya)
                .WithMany(m => m.Gelirler)
                .HasForeignKey(g => g.MarketVardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Market Indexes
            modelBuilder.Entity<MarketVardiya>()
                .HasIndex(m => m.Tarih)
                .HasDatabaseName("IX_MarketVardiyalar_Tarih")
                .IsDescending();

            modelBuilder.Entity<MarketVardiya>()
                .HasIndex(m => m.IstasyonId)
                .HasDatabaseName("IX_MarketVardiyalar_IstasyonId");
        }
    }
}
