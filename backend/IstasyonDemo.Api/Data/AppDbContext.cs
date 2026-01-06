using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;


namespace IstasyonDemo.Api.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Personel> Personeller { get; set; }
        public DbSet<Vardiya> Vardiyalar { get; set; }
        public DbSet<OtomasyonSatis> OtomasyonSatislar { get; set; }
        public DbSet<FiloSatis> FiloSatislar { get; set; }
        public DbSet<Pusula> Pusulalar { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Firma> Firmalar { get; set; }
        public DbSet<Istasyon> Istasyonlar { get; set; }
        public DbSet<VardiyaLog> VardiyaLoglari { get; set; }
        public DbSet<MarketVardiya> MarketVardiyalar { get; set; }
        public DbSet<MarketZRaporu> MarketZRaporlari { get; set; }
        public DbSet<MarketTahsilat> MarketTahsilatlar { get; set; }
        public DbSet<MarketGider> MarketGiderler { get; set; }
        public DbSet<MarketGelir> MarketGelirler { get; set; }
        public DbSet<PusulaKrediKartiDetay> PusulaKrediKartiDetaylari { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OtomatikDosya> OtomatikDosyalar { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<TankGiris> TankGirisler { get; set; }
        public DbSet<Yakit> Yakitlar { get; set; }
        public DbSet<SystemDefinition> SystemDefinitions { get; set; }
        public DbSet<PompaGider> PompaGiderler { get; set; }
        public DbSet<PusulaDigerOdeme> PusulaDigerOdemeleri { get; set; }
        public DbSet<PusulaVeresiye> PusulaVeresiyeler { get; set; }
        public DbSet<AylikStokOzeti> AylikStokOzetleri { get; set; }
        public DbSet<FaturaStokTakip> FaturaStokTakipleri { get; set; }
        public DbSet<IstasyonAyarlari> IstasyonAyarlari { get; set; }
        public DbSet<VardiyaXmlLog> VardiyaXmlLoglari { get; set; }
        public DbSet<DosyaYuklemeAyarlari> DosyaYuklemeAyarlari { get; set; }
        public DbSet<VardiyaTankEnvanteri> VardiyaTankEnvanterleri { get; set; }
        public DbSet<CariKart> CariKartlar { get; set; }
        public DbSet<CariHareket> CariHareketler { get; set; }
        public DbSet<VardiyaPompaEndeks> VardiyaPompaEndeksleri { get; set; }
        public DbSet<VardiyaRaporArsiv> VardiyaRaporArsivleri { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

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

            modelBuilder.Entity<PusulaKrediKartiDetay>()
                .HasOne(pk => pk.Pusula)
                .WithMany(p => p.KrediKartiDetaylari)
                .HasForeignKey(pk => pk.PusulaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PusulaVeresiye>()
                .HasOne(pv => pv.Pusula)
                .WithMany(p => p.Veresiyeler)
                .HasForeignKey(pv => pv.PusulaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PusulaVeresiye>()
                .HasOne(pv => pv.CariKart)
                .WithMany()
                .HasForeignKey(pv => pv.CariKartId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PompaGider>()
                .HasOne(g => g.Vardiya)
                .WithMany(v => v.Giderler)
                .HasForeignKey(g => g.VardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VardiyaTankEnvanteri>()
                .HasOne(t => t.Vardiya)
                .WithMany()
                .HasForeignKey(t => t.VardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VardiyaPompaEndeks>()
                .HasOne(e => e.Vardiya)
                .WithMany(v => v.PompaEndeksleri)
                .HasForeignKey(e => e.VardiyaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cari Kart ve Hareket İlişkileri
            modelBuilder.Entity<CariKart>()
                .HasOne(c => c.Istasyon)
                .WithMany()
                .HasForeignKey(c => c.IstasyonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CariHareket>()
                .HasOne(h => h.CariKart)
                .WithMany() // CariKart.Hareketler koleksiyonu varsa WithMany(c => c.Hareketler) diyebiliriz ama modelde ICollection tanımladıysak otomatik görür.
                            // CariKart modeline bakarsak: 'Relations' yorum satırı altında ICollection yoktu ama ben sonradan modele bakınca olmadığını görüyorum.
                            // Wait, step 4175 overwrote it WITHOUT ICollection.
                            // So WithMany() is correct (uni-directional or implicit).
                .HasForeignKey(h => h.CariKartId)
                .OnDelete(DeleteBehavior.Cascade);

            // PERFORMANCE INDEXES
            modelBuilder.Entity<OtomasyonSatis>()
                .HasIndex(o => o.VardiyaId)
                .HasDatabaseName("IX_OtomasyonSatislar_VardiyaId");

            modelBuilder.Entity<FiloSatis>()
                .HasIndex(f => f.VardiyaId)
                .HasDatabaseName("IX_FiloSatislar_VardiyaId");

            modelBuilder.Entity<Pusula>()
                .HasIndex(p => p.VardiyaId)
                .HasDatabaseName("IX_Pusulalar_VardiyaId");

            modelBuilder.Entity<Vardiya>()
                .HasIndex(v => v.BaslangicTarihi)
                .HasDatabaseName("IX_Vardiyalar_BaslangicTarihi")
                .IsDescending();

            modelBuilder.Entity<Vardiya>()
                .HasIndex(v => v.Durum)
                .HasDatabaseName("IX_Vardiyalar_Durum");

            modelBuilder.Entity<OtomasyonSatis>()
                .HasIndex(o => o.PersonelId)
                .HasDatabaseName("IX_OtomasyonSatislar_PersonelId");

            modelBuilder.Entity<Pusula>()
                .HasIndex(p => p.PersonelId)
                .HasDatabaseName("IX_Pusulalar_PersonelId");
            
            modelBuilder.Entity<Personel>()
                .HasIndex(p => p.IstasyonId)
                .HasDatabaseName("IX_Personeller_IstasyonId");

            // AylikStokOzeti - Unique constraint for (YakitId, Yil, Ay)
            modelBuilder.Entity<AylikStokOzeti>()
                .HasIndex(a => new { a.YakitId, a.Yil, a.Ay })
                .IsUnique()
                .HasDatabaseName("IX_AylikStokOzeti_YakitYilAy");

            modelBuilder.Entity<AylikStokOzeti>()
                .HasOne(a => a.Yakit)
                .WithMany()
                .HasForeignKey(a => a.YakitId)
                .OnDelete(DeleteBehavior.Cascade);

            // FaturaStokTakip - Index for FIFO ordering
            modelBuilder.Entity<FaturaStokTakip>()
                .HasIndex(f => new { f.YakitId, f.FaturaTarihi })
                .HasDatabaseName("IX_FaturaStokTakip_YakitFaturaTarihi");

            modelBuilder.Entity<FaturaStokTakip>()
                .HasOne(f => f.Yakit)
                .WithMany()
                .HasForeignKey(f => f.YakitId)
                .OnDelete(DeleteBehavior.Cascade);

            // Firma Relations
            modelBuilder.Entity<Firma>()
                .HasOne(f => f.Patron)
                .WithMany()
                .HasForeignKey(f => f.PatronId)
                .OnDelete(DeleteBehavior.SetNull);

            // Istasyon Relations
            modelBuilder.Entity<Istasyon>()
                .HasOne(i => i.Firma)
                .WithMany(f => f.Istasyonlar)
                .HasForeignKey(i => i.FirmaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Notification Relations
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.RelatedVardiya)
                .WithMany()
                .HasForeignKey(n => n.RelatedVardiyaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.RelatedMarketVardiya)
                .WithMany()
                .HasForeignKey(n => n.RelatedMarketVardiyaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Notification Indexes
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId)
                .HasDatabaseName("IX_Notifications_UserId");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.IsRead)
                .HasDatabaseName("IX_Notifications_IsRead");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.CreatedAt)
                .HasDatabaseName("IX_Notifications_CreatedAt")
                .IsDescending();

            // OtomatikDosya Relations
            modelBuilder.Entity<OtomatikDosya>()
                .HasOne(o => o.Istasyon)
                .WithMany()
                .HasForeignKey(o => o.IstasyonId)
                .OnDelete(DeleteBehavior.SetNull);

            // RolePermission Relations
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserSettings Relations
            modelBuilder.Entity<UserSettings>()
                .HasOne(us => us.User)
                .WithOne()
                .HasForeignKey<UserSettings>(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // TankGiris Indexes
            modelBuilder.Entity<TankGiris>()
                .HasIndex(t => t.Tarih)
                .HasDatabaseName("IX_TankGirisler_Tarih")
                .IsDescending();
            
            modelBuilder.Entity<TankGiris>()
                .HasOne(t => t.Yakit)
                .WithMany()
                .HasForeignKey(t => t.YakitId)
                .OnDelete(DeleteBehavior.Restrict);


            // Seed Yakitlar
            modelBuilder.Entity<Yakit>().HasData(
                new Yakit { Id = 1, Ad = "Motorin", OtomasyonUrunAdi = "MOTORIN,DIZEL", Renk = "#F59E0B", Sira = 1 },
                new Yakit { Id = 2, Ad = "Benzin (Kurşunsuz 95)", OtomasyonUrunAdi = "BENZIN,KURŞUNSUZ,KURSUNSUZ", Renk = "#EF4444", Sira = 2 },
                 new Yakit { Id = 3, Ad = "LPG", OtomasyonUrunAdi = "LPG,OTOGAZ", Renk = "#3B82F6", Sira = 3 }
            );
        }
    }
}
