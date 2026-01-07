using System.Text.Json;
using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Services
{
    /// <summary>
    /// Onaylanan vardiyaların rapor verilerini hesaplayıp arşivleyen servis.
    /// Performans optimizasyonu için tüm hesaplamalar bir kez yapılır ve saklanır.
    /// </summary>
    public class VardiyaArsivService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VardiyaArsivService> _logger;
        private readonly IYakitService _yakitService;

        public VardiyaArsivService(AppDbContext context, ILogger<VardiyaArsivService> logger, IYakitService yakitService)
        {
            _context = context;
            _logger = logger;
            _yakitService = yakitService;
        }

        /// <summary>
        /// Vardiyayı arşivler - tüm rapor verilerini hesaplayıp JSON olarak saklar.
        /// </summary>
        public async Task<VardiyaRaporArsiv?> ArsivleVardiya(int vardiyaId, int onaylayanId, string onaylayanAdi)
        {
            try
            {
                var mevcutArsiv = await _context.VardiyaRaporArsivleri
                    .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);

                if (mevcutArsiv != null)
                {
                    _logger.LogWarning("Vardiya {VardiyaId} zaten arşivlenmiş, güncelleniyor.", vardiyaId);
                    return await GuncelleArsiv(vardiyaId, mevcutArsiv.Id);
                }

                var vardiya = await GetVardiyaWithDetails(vardiyaId);
                if (vardiya == null)
                {
                    _logger.LogError("Vardiya {VardiyaId} bulunamadı.", vardiyaId);
                    return null;
                }

                var hesaplamalar = await HesaplaRaporVerileri(vardiya);

                var jsonOptions = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };

                var arsiv = new VardiyaRaporArsiv
                {
                    VardiyaId = vardiyaId,
                    IstasyonId = vardiya.IstasyonId,
                    Tarih = vardiya.BaslangicTarihi,
                    SistemToplam = hesaplamalar.SistemToplam,
                    TahsilatToplam = hesaplamalar.TahsilatToplam,
                    FiloToplam = hesaplamalar.FiloToplam,
                    GiderToplam = hesaplamalar.GiderToplam,
                    Fark = hesaplamalar.Fark,
                    FarkYuzde = hesaplamalar.FarkYuzde,
                    Durum = hesaplamalar.Durum,
                    KarsilastirmaRaporuJson = JsonSerializer.Serialize(hesaplamalar.KarsilastirmaRaporu, jsonOptions),
                    FarkRaporuJson = JsonSerializer.Serialize(hesaplamalar.FarkRaporu, jsonOptions),
                    PompaSatisRaporuJson = JsonSerializer.Serialize(hesaplamalar.PompaSatisRaporu, jsonOptions),
                    TahsilatDetayJson = JsonSerializer.Serialize(hesaplamalar.TahsilatDetay, jsonOptions),
                    GiderRaporuJson = JsonSerializer.Serialize(hesaplamalar.GiderRaporu, jsonOptions),
                    TankEnvanterJson = JsonSerializer.Serialize(vardiya.VardiyaTankEnvanteri.Select(t => new 
                    {
                        t.TankNo,
                        t.TankAdi,
                        t.YakitTipi,
                        t.BaslangicStok,
                        t.BitisStok,
                        t.SatilanMiktar,
                        t.SevkiyatMiktar,
                        t.BeklenenTuketim,
                        t.FarkMiktar
                    }), jsonOptions),
                    PersonelSatisDetayJson = JsonSerializer.Serialize(hesaplamalar.PersonelSatisDetay, jsonOptions),
                    FiloSatisDetayJson = JsonSerializer.Serialize(hesaplamalar.FiloSatisDetay, jsonOptions),
                    OnaylayanId = onaylayanId,
                    OnaylayanAdi = onaylayanAdi,
                    OnayTarihi = DateTime.UtcNow,
                    SorumluId = vardiya.SorumluId,
                    SorumluAdi = vardiya.SorumluAdi,
                    OlusturmaTarihi = DateTime.UtcNow
                };

                _context.VardiyaRaporArsivleri.Add(arsiv);
                await _context.SaveChangesAsync();

                vardiya.RaporArsivId = arsiv.Id;
                vardiya.Arsivlendi = true;
                vardiya.TahsilatToplam = hesaplamalar.TahsilatToplam;
                vardiya.OtomasyonToplam = hesaplamalar.SistemToplam;
                vardiya.FiloToplam = hesaplamalar.FiloToplam;
                vardiya.GiderToplam = hesaplamalar.GiderToplam;
                vardiya.Fark = hesaplamalar.Fark;

                await _context.SaveChangesAsync();
                return arsiv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vardiya {VardiyaId} arşivlenirken hata oluştu.", vardiyaId);
                throw;
            }
        }

        public async Task TemizleHamVeriler(int vardiyaId)
        {
            try
            {
                await _context.OtomasyonSatislar.Where(x => x.VardiyaId == vardiyaId).ExecuteDeleteAsync();
                await _context.FiloSatislar.Where(x => x.VardiyaId == vardiyaId).ExecuteDeleteAsync();
                await _context.VardiyaPompaEndeksleri.Where(x => x.VardiyaId == vardiyaId).ExecuteDeleteAsync();
                await _context.VardiyaTankEnvanterleri.Where(x => x.VardiyaId == vardiyaId).ExecuteDeleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vardiya {VardiyaId} ham verileri temizlenirken hata oluştu.", vardiyaId);
            }
        }

        public async Task<KarsilastirmaRaporuDto?> GetKarsilastirmaRaporuFromArsiv(int vardiyaId)
        {
            var arsiv = await _context.VardiyaRaporArsivleri.AsNoTracking().FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);
            if (arsiv?.KarsilastirmaRaporuJson == null) return null;
            return JsonSerializer.Deserialize<KarsilastirmaRaporuDto>(arsiv.KarsilastirmaRaporuJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<FarkRaporItemDto?> GetFarkRaporuFromArsiv(int vardiyaId)
        {
            var arsiv = await _context.VardiyaRaporArsivleri.AsNoTracking().FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);
            if (arsiv?.FarkRaporuJson == null) return null;
            return JsonSerializer.Deserialize<FarkRaporItemDto>(arsiv.FarkRaporuJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<List<VardiyaArsivOzetDto>> GetArsivListesi(int? istasyonId, DateTime baslangic, DateTime bitis)
        {
            var query = _context.VardiyaRaporArsivleri.AsNoTracking().Where(a => a.Tarih >= baslangic && a.Tarih <= bitis);
            if (istasyonId.HasValue) query = query.Where(a => a.IstasyonId == istasyonId.Value);
            return await query.OrderByDescending(a => a.Tarih).Select(a => new VardiyaArsivOzetDto
            {
                VardiyaId = a.VardiyaId,
                ArsivId = a.Id,
                Tarih = a.Tarih,
                SistemToplam = a.SistemToplam,
                TahsilatToplam = a.TahsilatToplam,
                FiloToplam = a.FiloToplam,
                GiderToplam = a.GiderToplam,
                Fark = a.Fark,
                FarkYuzde = a.FarkYuzde,
                Durum = a.Durum,
                OnaylayanAdi = a.OnaylayanAdi,
                OnayTarihi = a.OnayTarihi,
                SorumluAdi = a.SorumluAdi
            }).ToListAsync();
        }

        public async Task<VardiyaRaporArsiv?> GuncelleArsiv(int vardiyaId, int arsivId)
        {
            var arsiv = await _context.VardiyaRaporArsivleri.FindAsync(arsivId);
            if (arsiv == null) return null;
            var vardiya = await GetVardiyaWithDetails(vardiyaId);
            if (vardiya == null) return null;
            var hesaplamalar = await HesaplaRaporVerileri(vardiya);
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false, ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };

            arsiv.SistemToplam = hesaplamalar.SistemToplam;
            arsiv.TahsilatToplam = hesaplamalar.TahsilatToplam;
            arsiv.FiloToplam = hesaplamalar.FiloToplam;
            arsiv.GiderToplam = hesaplamalar.GiderToplam;
            arsiv.Fark = hesaplamalar.Fark;
            arsiv.FarkYuzde = hesaplamalar.FarkYuzde;
            arsiv.Durum = hesaplamalar.Durum;
            arsiv.KarsilastirmaRaporuJson = JsonSerializer.Serialize(hesaplamalar.KarsilastirmaRaporu, jsonOptions);
            arsiv.FarkRaporuJson = JsonSerializer.Serialize(hesaplamalar.FarkRaporu, jsonOptions);
            arsiv.PompaSatisRaporuJson = JsonSerializer.Serialize(hesaplamalar.PompaSatisRaporu, jsonOptions);
            arsiv.TahsilatDetayJson = JsonSerializer.Serialize(hesaplamalar.TahsilatDetay, jsonOptions);
            arsiv.GiderRaporuJson = JsonSerializer.Serialize(hesaplamalar.GiderRaporu, jsonOptions);
            arsiv.TankEnvanterJson = JsonSerializer.Serialize(vardiya.VardiyaTankEnvanteri.Select(t => new { t.TankNo, t.TankAdi, t.YakitTipi, t.BaslangicStok, t.BitisStok, t.SatilanMiktar, t.SevkiyatMiktar, t.BeklenenTuketim, t.FarkMiktar }), jsonOptions);
            arsiv.PersonelSatisDetayJson = JsonSerializer.Serialize(hesaplamalar.PersonelSatisDetay, jsonOptions);
            arsiv.FiloSatisDetayJson = JsonSerializer.Serialize(hesaplamalar.FiloSatisDetay, jsonOptions);
            arsiv.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return arsiv;
        }

        public async Task<KarsilastirmaRaporuDto?> GetTaslakKarsilastirmaRaporu(int vardiyaId)
        {
            var vardiya = await GetVardiyaWithDetails(vardiyaId);
            if (vardiya == null) return null;
            var sonuc = await HesaplaRaporVerileri(vardiya);
            return sonuc.KarsilastirmaRaporu;
        }

        public async Task<bool> OnayiKaldirVeGeriYukle(int vardiyaId, int userId, string userName)
        {
            try
            {
                var vardiya = await _context.Vardiyalar.FirstOrDefaultAsync(v => v.Id == vardiyaId);
                if (vardiya == null || vardiya.Durum != VardiyaDurum.ONAYLANDI) return false;
                var arsiv = await _context.VardiyaRaporArsivleri.FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);
                vardiya.Durum = VardiyaDurum.ONAY_BEKLIYOR;
                vardiya.Arsivlendi = false;
                vardiya.RaporArsivId = null;
                if (arsiv != null) _context.VardiyaRaporArsivleri.Remove(arsiv);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        #region Private Helper Methods

        public async Task<Vardiya?> GetVardiyaWithDetails(int vardiyaId)
        {
            return await _context.Vardiyalar
                .AsSplitQuery()
                .Include(v => v.OtomasyonSatislar).ThenInclude(s => s.Yakit)
                .Include(v => v.FiloSatislar).ThenInclude(f => f.Yakit)
                .Include(v => v.Pusulalar).ThenInclude(p => p.DigerOdemeler)
                .Include(v => v.Pusulalar).ThenInclude(p => p.Veresiyeler)
                .Include(v => v.PompaEndeksleri)
                .Include(v => v.Giderler)
                .Include(v => v.VardiyaTankEnvanteri)
                .FirstOrDefaultAsync(v => v.Id == vardiyaId);
        }

        public async Task<VardiyaHesaplamaSonucu> HesaplaRaporVerileri(Vardiya vardiya)
        {
            // Mobil Satışlar (OtomasyonSatislar içindeki MobilOdemeTutar alanından)
            var mobileSales = vardiya.OtomasyonSatislar.Where(s => s.MobilOdemeTutar > 0).ToList();

            // Geçerli Filo Satışları (Mobil ödemeler artık OtomasyonSatislar içinde olduğu için buradan ayıklamaya gerek yok ama ISTASYON'u dahil ediyoruz)
            var validFiloSatislar = vardiya.FiloSatislar.ToList();

            var filoToplam = validFiloSatislar.Sum(f => f.Tutar);
            var sistemToplam = vardiya.OtomasyonSatislar.Sum(s => s.ToplamTutar) + filoToplam; // FIX: Sistem Verisine Filo da dahildir.

            // Tahsilat Toplamı Hesabı:
            // Mobil ödemeler otomatik olarak pompacı pusulasına "Diğer Ödeme" olarak işlendiği için
            // burada sadece pusula toplamını almak yeterlidir. Ayrıca mobileSales eklenmemelidir.
            // FIX: Veresiyeler de toplama dahil edildi.
            var tahsilatToplam = vardiya.Pusulalar.Sum(p => p.Nakit + p.KrediKarti + (p.DigerOdemeler?.Sum(d => d.Tutar) ?? 0) + (p.Veresiyeler?.Sum(v => v.Tutar) ?? 0));
            var giderToplam = vardiya.Giderler.Sum(g => g.Tutar);
            
            // Fark = (Pusula + Gider + Filo(Sanal Tahsilat)) - (Otomasyon + Filo(Sistem))
            // Filo iki tarafta da olduğu için sadeleşir: Fark = Pusula + Gider - Otomasyon
            var fark = tahsilatToplam + filoToplam + giderToplam - sistemToplam;
            var farkYuzde = sistemToplam > 0 ? (fark / sistemToplam) * 100 : 0;
            
            var durum = "UYUMLU";
            if (Math.Abs(fark) > 100) durum = "KRITIK_FARK";
            else if (Math.Abs(fark) > 1) durum = "FARK_VAR";

            // Pompa satış özetleri (ALL SALES)
            var allSalesRaw = new List<PompaSatisOzetDto>();
            foreach (var s in vardiya.OtomasyonSatislar.Where(s => s.Litre > 0 || s.ToplamTutar > 0))
            {
                var yakit = await _yakitService.IdentifyYakitAsync(s.YakitTuru ?? "");
                allSalesRaw.Add(new PompaSatisOzetDto { PompaNo = s.PompaNo, YakitTuru = yakit?.Ad ?? s.YakitTuru ?? "DIGER", Litre = s.Litre, ToplamTutar = s.ToplamTutar, IslemSayisi = 1 });
            }
            foreach (var f in vardiya.FiloSatislar.Where(f => f.Litre > 0 || f.Tutar > 0))
            {
                var yakit = await _yakitService.IdentifyYakitAsync(f.YakitTuru ?? "");
                allSalesRaw.Add(new PompaSatisOzetDto { PompaNo = f.PompaNo, YakitTuru = yakit?.Ad ?? f.YakitTuru ?? "DIGER", Litre = f.Litre, ToplamTutar = f.Tutar, IslemSayisi = 1 });
            }

            var tumPompaSatislari = allSalesRaw
                .GroupBy(s => new { s.PompaNo, s.YakitTuru })
                .Select(g => new PompaSatisOzetDto { PompaNo = g.Key.PompaNo, YakitTuru = g.Key.YakitTuru, Litre = g.Sum(s => s.Litre), ToplamTutar = g.Sum(s => s.ToplamTutar), IslemSayisi = g.Count() })
                .OrderBy(p => p.PompaNo).ToList();

            // Personel Satış Detayları
            var personelSatisDetay = vardiya.OtomasyonSatislar
                .Where(s => s.Litre > 0 || s.ToplamTutar > 0)
                .GroupBy(s => new { s.PersonelKeyId, s.PersonelAdi })
                .Select(g => new PersonelSatisDetayDto
                {
                    PersonelKeyId = g.Key.PersonelKeyId,
                    PersonelAdi = g.Key.PersonelAdi,
                    ToplamIslemSayisi = g.Count(),
                    Satislar = g.GroupBy(s => s.Yakit != null ? s.Yakit.Ad : s.YakitTuru)
                        .Select(yg => new YakitSatisDetayDto 
                        { 
                            YakitTuru = yg.Key, 
                            Litre = yg.Sum(s => s.Litre), 
                            Tutar = yg.Sum(s => s.ToplamTutar), 
                            IslemSayisi = yg.Count() 
                        }).ToList(),
                }).OrderBy(p => p.PersonelAdi).ToList();

            // Filo Satış Detayları
            var filoSatisDetay = validFiloSatislar
                .GroupBy(f => (f.FiloAdi == null || f.FiloAdi == "") ? "OTOBIL" : f.FiloAdi)
                .Select(g => new FiloSatisDetayDto { YakitTuru = g.Key, Litre = g.Sum(f => f.Litre), Tutar = g.Sum(f => f.Tutar) }).ToList();

            // Personel Farkları
            var personelFarklari = vardiya.OtomasyonSatislar
                .GroupBy(s => new { s.PersonelKeyId, s.PersonelAdi })
                .Select(g => new PersonelFarkDto { PersonelKeyId = g.Key.PersonelKeyId, PersonelAdi = g.Key.PersonelAdi, Otomasyon = g.Sum(s => s.ToplamTutar) }).ToList();

            foreach (var p in personelFarklari)
            {
                var pusula = vardiya.Pusulalar.FirstOrDefault(ps => ps.PersonelAdi == p.PersonelAdi);
                if (pusula != null) p.Tahsilat = pusula.Nakit + pusula.KrediKarti + (pusula.DigerOdemeler?.Sum(d => d.Tutar) ?? 0) + (pusula.Veresiyeler?.Sum(v => v.Tutar) ?? 0);
                
                // Mobil ödemeleri bu personelin tahsilatına ekle
                var personelinMobilOdemeleri = mobileSales.Where(m => m.PersonelKeyId == p.PersonelKeyId || m.PersonelAdi == p.PersonelAdi).Sum(m => m.MobilOdemeTutar);
                p.Tahsilat += personelinMobilOdemeleri;
                
                p.Fark = p.Tahsilat - p.Otomasyon;
            }
            if (filoToplam > 0) personelFarklari.Add(new PersonelFarkDto { PersonelAdi = "FİLO SATIŞLARI", PersonelKeyId = "FILO", Otomasyon = filoToplam, Tahsilat = filoToplam, Fark = 0 });

            var karsilastirmaDetaylar = new List<KarsilastirmaDetayDto>
            {
                new() { OdemeYontemi = "POMPACI_SATISI", SistemTutar = sistemToplam - filoToplam, TahsilatTutar = tahsilatToplam, Fark = tahsilatToplam - (sistemToplam - filoToplam) },
                new() { OdemeYontemi = "FILO", SistemTutar = filoToplam, TahsilatTutar = filoToplam, Fark = 0 }
            };

            var tahsilatDetay = new TahsilatDetayRaporu
            {
                Nakit = vardiya.Pusulalar.Sum(p => p.Nakit),
                KrediKarti = vardiya.Pusulalar.Sum(p => p.KrediKarti),
                DigerOdemeler = vardiya.Pusulalar.SelectMany(p => p.DigerOdemeler ?? new List<PusulaDigerOdeme>())
                    .GroupBy(d => d.TurAdi)
                    .Select(g => new DigerOdemeOzet { OdemeTuru = g.Key, Tutar = g.Sum(x => x.Tutar) })
                    .ToList()
            };

            // Mobil ödemeleri tahsilat detayına ekle (eğer pusulada yoksa)
            if (mobileSales.Any() && !tahsilatDetay.DigerOdemeler.Any(d => d.OdemeTuru.Contains("Mobil", StringComparison.OrdinalIgnoreCase)))
            {
                tahsilatDetay.DigerOdemeler.Add(new DigerOdemeOzet { OdemeTuru = "Mobil Ödeme", Tutar = mobileSales.Sum(m => m.MobilOdemeTutar) });
            }

            var giderRaporu = vardiya.Giderler.Select(g => new GiderKalemi { GiderTuru = g.GiderTuru, Tutar = g.Tutar, Aciklama = g.Aciklama, BelgeTarihi = g.BelgeTarihi }).ToList();

            return new VardiyaHesaplamaSonucu
            {
                SistemToplam = sistemToplam,
                TahsilatToplam = tahsilatToplam + filoToplam,
                FiloToplam = filoToplam,
                GiderToplam = giderToplam,
                Fark = fark,
                FarkYuzde = farkYuzde,
                Durum = durum,
                KarsilastirmaRaporu = new KarsilastirmaRaporuDto { VardiyaId = vardiya.Id, Tarih = vardiya.BaslangicTarihi, SistemToplam = sistemToplam, TahsilatToplam = tahsilatToplam + filoToplam, Fark = fark, FarkYuzde = farkYuzde, Durum = durum, Detaylar = karsilastirmaDetaylar, PompaSatislari = tumPompaSatislari },
                FarkRaporu = new FarkRaporItemDto { VardiyaId = vardiya.Id, Tarih = vardiya.BaslangicTarihi, DosyaAdi = vardiya.DosyaAdi ?? "", OtomasyonToplam = sistemToplam, TahsilatToplam = tahsilatToplam + filoToplam, Fark = fark, Durum = vardiya.Durum.ToString(), PersonelFarklari = personelFarklari },
                PompaSatisRaporu = tumPompaSatislari,
                TahsilatDetay = tahsilatDetay,
                GiderRaporu = giderRaporu,
                PersonelSatisDetay = personelSatisDetay,
                FiloSatisDetay = filoSatisDetay
            };
        }

        #endregion
    }

    #region DTOs

    public class VardiyaArsivOzetDto
    {
        public int VardiyaId { get; set; }
        public int ArsivId { get; set; }
        public DateTime Tarih { get; set; }
        public decimal SistemToplam { get; set; }
        public decimal TahsilatToplam { get; set; }
        public decimal FiloToplam { get; set; }
        public decimal GiderToplam { get; set; }
        public decimal Fark { get; set; }
        public decimal FarkYuzde { get; set; }
        public string Durum { get; set; } = "";
        public string? OnaylayanAdi { get; set; }
        public DateTime? OnayTarihi { get; set; }
        public string? SorumluAdi { get; set; }
    }

    public class VardiyaHesaplamaSonucu
    {
        public decimal SistemToplam { get; set; }
        public decimal TahsilatToplam { get; set; }
        public decimal FiloToplam { get; set; }
        public decimal GiderToplam { get; set; }
        public decimal Fark { get; set; }
        public decimal FarkYuzde { get; set; }
        public string Durum { get; set; } = "";
        public KarsilastirmaRaporuDto KarsilastirmaRaporu { get; set; } = new();
        public FarkRaporItemDto FarkRaporu { get; set; } = new();
        public List<PompaSatisOzetDto> PompaSatisRaporu { get; set; } = new();
        public TahsilatDetayRaporu TahsilatDetay { get; set; } = new();
        public List<GiderKalemi> GiderRaporu { get; set; } = new();
        public List<PersonelSatisDetayDto> PersonelSatisDetay { get; set; } = new();
        public List<FiloSatisDetayDto> FiloSatisDetay { get; set; } = new();
    }

    public class PersonelSatisDetayDto
    {
        public string PersonelKeyId { get; set; } = string.Empty;
        public string PersonelAdi { get; set; } = string.Empty;
        public int ToplamIslemSayisi { get; set; }
        public List<YakitSatisDetayDto> Satislar { get; set; } = new();
    }

    public class FiloSatisDetayDto
    {
        public string YakitTuru { get; set; } = string.Empty;
        public decimal Litre { get; set; }
        public decimal Tutar { get; set; }
    }

    public class YakitSatisDetayDto
    {
        public string YakitTuru { get; set; } = string.Empty;
        public decimal Litre { get; set; }
        public decimal Tutar { get; set; }
        public int IslemSayisi { get; set; }
    }

    public class TahsilatDetayRaporu
    {
        public decimal Nakit { get; set; }
        public decimal KrediKarti { get; set; }
        public List<DigerOdemeOzet> DigerOdemeler { get; set; } = new();
    }

    public class DigerOdemeOzet
    {
        public string OdemeTuru { get; set; } = "";
        public decimal Tutar { get; set; }
    }

    public class GiderKalemi
    {
        public string GiderTuru { get; set; } = "";
        public decimal Tutar { get; set; }
        public string Aciklama { get; set; } = "";
        public DateTime? BelgeTarihi { get; set; }
    }

    #endregion
}
