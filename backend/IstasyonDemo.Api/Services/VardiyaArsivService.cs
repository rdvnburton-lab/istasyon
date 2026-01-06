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

        public VardiyaArsivService(AppDbContext context, ILogger<VardiyaArsivService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Vardiyayı arşivler - tüm rapor verilerini hesaplayıp JSON olarak saklar.
        /// Bu metod vardiya onaylandığında çağrılır.
        /// </summary>
        public async Task<VardiyaRaporArsiv?> ArsivleVardiya(int vardiyaId, int onaylayanId, string onaylayanAdi)
        {
            try
            {
                // Mevcut arşiv var mı kontrol et
                var mevcutArsiv = await _context.VardiyaRaporArsivleri
                    .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);

                if (mevcutArsiv != null)
                {
                    _logger.LogWarning("Vardiya {VardiyaId} zaten arşivlenmiş, güncelleniyor.", vardiyaId);
                    return await GuncelleArsiv(vardiyaId, mevcutArsiv.Id);
                }

                // Vardiya verilerini çek
                var vardiya = await GetVardiyaWithDetails(vardiyaId);
                if (vardiya == null)
                {
                    _logger.LogError("Vardiya {VardiyaId} bulunamadı.", vardiyaId);
                    return null;
                }




                // Hesaplamaları yap
                var hesaplamalar = HesaplaRaporVerileri(vardiya);

                // Raporları JSON'a çevir
                var jsonOptions = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };

                // Arşiv kaydı oluştur (sadece hesaplanmış raporlar)
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
                    }), jsonOptions), // 🆕 Tank verileri
                    PersonelSatisDetayJson = JsonSerializer.Serialize(hesaplamalar.PersonelSatisDetay, jsonOptions), // 🆕 Personel detayları
                    FiloSatisDetayJson = JsonSerializer.Serialize(hesaplamalar.FiloSatisDetay, jsonOptions), // 🆕 Filo detayları
                    OnaylayanId = onaylayanId,
                    OnaylayanAdi = onaylayanAdi,
                    OnayTarihi = DateTime.UtcNow,
                    SorumluId = vardiya.SorumluId,
                    SorumluAdi = vardiya.SorumluAdi,
                    OlusturmaTarihi = DateTime.UtcNow
                };



                _context.VardiyaRaporArsivleri.Add(arsiv);
                await _context.SaveChangesAsync();

                // Vardiyayı güncelle
                vardiya.RaporArsivId = arsiv.Id;
                vardiya.Arsivlendi = true;
                vardiya.TahsilatToplam = hesaplamalar.TahsilatToplam;
                vardiya.OtomasyonToplam = hesaplamalar.SistemToplam;
                vardiya.FiloToplam = hesaplamalar.FiloToplam;
                vardiya.GiderToplam = hesaplamalar.GiderToplam;
                vardiya.Fark = hesaplamalar.Fark;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Vardiya {VardiyaId} başarıyla arşivlendi. Arşiv ID: {ArsivId}",
                    vardiyaId, arsiv.Id);

                return arsiv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vardiya {VardiyaId} arşivlenirken hata oluştu.", vardiyaId);
                throw;
            }
        }



        /// <summary>
        /// Arşivlenen vardiyaya ait ham verileri siler - veritabanı optimizasyonu için.
        /// Bu metod transaction dışında çağrılmalıdır (ayrı bir işlem olarak).
        /// </summary>
        public async Task TemizleHamVeriler(int vardiyaId)
        {
            // 🛑 GÜVENLİK ÖNLEMİ:
            // Tank Raporları ve Personel Karnesi gibi ekranlar henüz arşivden okumadığı için
            // ana tabloları (OtomasyonSatis, TankEnvanter vb.) SİLMİYORUZ.
            // Veriler hem ana tablolarda hem de arşivde (performans için) saklanacak.
            

            try
            {
                // 1. Otomasyon Satışları Sil
                await _context.OtomasyonSatislar
                    .Where(x => x.VardiyaId == vardiyaId)
                    .ExecuteDeleteAsync();

                // 2. Filo Satışları Sil
                await _context.FiloSatislar
                    .Where(x => x.VardiyaId == vardiyaId)
                    .ExecuteDeleteAsync();

                // 3. Pompa Endeksleri Sil
                await _context.VardiyaPompaEndeksleri
                    .Where(x => x.VardiyaId == vardiyaId)
                    .ExecuteDeleteAsync();

                // 4. Tank Envanteri Sil
                await _context.VardiyaTankEnvanterleri
                    .Where(x => x.VardiyaId == vardiyaId)
                    .ExecuteDeleteAsync();

                _logger.LogInformation("Vardiya {VardiyaId} için ham veriler temizlendi.", vardiyaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vardiya {VardiyaId} ham verileri temizlenirken hata oluştu.", vardiyaId);
                // Hata fırlatmıyoruz, arka plan işlemi olduğu için ana akışı bozmasın
            }
        }

        /// <summary>
        /// Arşivlenmiş karşılaştırma raporunu getirir.
        /// Arşiv yoksa null döner.
        /// </summary>
        public async Task<KarsilastirmaRaporuDto?> GetKarsilastirmaRaporuFromArsiv(int vardiyaId)
        {
            var arsiv = await _context.VardiyaRaporArsivleri
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);

            if (arsiv?.KarsilastirmaRaporuJson == null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<KarsilastirmaRaporuDto>(
                    arsiv.KarsilastirmaRaporuJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arşiv JSON deserialize hatası. VardiyaId: {VardiyaId}", vardiyaId);
                return null;
            }
        }

        /// <summary>
        /// Arşivlenmiş fark raporunu getirir.
        /// </summary>
        public async Task<FarkRaporItemDto?> GetFarkRaporuFromArsiv(int vardiyaId)
        {
            var arsiv = await _context.VardiyaRaporArsivleri
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);

            if (arsiv?.FarkRaporuJson == null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<FarkRaporItemDto>(
                    arsiv.FarkRaporuJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fark raporu JSON deserialize hatası. VardiyaId: {VardiyaId}", vardiyaId);
                return null;
            }
        }

        /// <summary>
        /// Belirli bir tarih aralığındaki arşivlenmiş vardiyaları listeler.
        /// Çok hızlıdır çünkü hesaplama yapılmaz, sadece özet alanlar okunur.
        /// </summary>
        public async Task<List<VardiyaArsivOzetDto>> GetArsivListesi(
            int? istasyonId, 
            DateTime baslangic, 
            DateTime bitis,
            int? userId = null,
            string? userRole = null)
        {
            var query = _context.VardiyaRaporArsivleri
                .AsNoTracking()
                .Where(a => a.Tarih >= baslangic && a.Tarih <= bitis);

            // İstasyon filtresi
            if (istasyonId.HasValue)
            {
                query = query.Where(a => a.IstasyonId == istasyonId.Value);
            }

            var sonuc = await query
                .OrderByDescending(a => a.Tarih)
                .Select(a => new VardiyaArsivOzetDto
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
                })
                .ToListAsync();

            return sonuc;
        }

        /// <summary>
        /// Mevcut bir arşivi günceller.
        /// </summary>
        public async Task<VardiyaRaporArsiv?> GuncelleArsiv(int vardiyaId, int arsivId)
        {
            var arsiv = await _context.VardiyaRaporArsivleri.FindAsync(arsivId);
            if (arsiv == null) return null;

            var vardiya = await GetVardiyaWithDetails(vardiyaId);
            if (vardiya == null) return null;

            var hesaplamalar = HesaplaRaporVerileri(vardiya);
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false 
            };

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
            arsiv.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return arsiv;
        }

        /// <summary>
        /// PDF raporu arşive ekler.
        /// </summary>
        public async Task<bool> EklePdfRapor(int arsivId, string raporTipi, byte[] pdfIcerik)
        {
            var arsiv = await _context.VardiyaRaporArsivleri.FindAsync(arsivId);
            if (arsiv == null) return false;

            switch (raporTipi.ToUpperInvariant())
            {
                case "KARSILASTIRMA":
                    arsiv.KarsilastirmaPdfIcerik = pdfIcerik;
                    break;
                case "FARK":
                    arsiv.FarkRaporuPdfIcerik = pdfIcerik;
                    break;
                case "OZET":
                    arsiv.VardiyaOzetPdfIcerik = pdfIcerik;
                    break;
                default:
                    return false;
            }

            arsiv.GuncellemeTarihi = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// PDF raporu arşivden getirir.
        /// </summary>
        public async Task<byte[]?> GetPdfRapor(int vardiyaId, string raporTipi)
        {
            var arsiv = await _context.VardiyaRaporArsivleri
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);

            if (arsiv == null) return null;

            return raporTipi.ToUpperInvariant() switch
            {
                "KARSILASTIRMA" => arsiv.KarsilastirmaPdfIcerik,
                "FARK" => arsiv.FarkRaporuPdfIcerik,
                "OZET" => arsiv.VardiyaOzetPdfIcerik,
                _ => null
            };
        }

        /// <summary>
        /// Onay kaldırma - VardiyaXmlLog'daki XML'den verileri geri yükler.
        /// Sadece admin kullanabilir.
        /// </summary>
        public async Task<bool> OnayiKaldirVeGeriYukle(int vardiyaId, int userId, string userName)
        {
            try
            {
                // Vardiyayı bul
                var vardiya = await _context.Vardiyalar
                    .FirstOrDefaultAsync(v => v.Id == vardiyaId);

                if (vardiya == null)
                {
                    _logger.LogError("Vardiya {VardiyaId} bulunamadı.", vardiyaId);
                    throw new InvalidOperationException("Vardiya bulunamadı.");
                }

                if (vardiya.Durum != VardiyaDurum.ONAYLANDI)
                {
                    _logger.LogWarning("Vardiya {VardiyaId} onaylı değil, onay kaldırılamaz.", vardiyaId);
                    throw new InvalidOperationException("Vardiya onaylı değil, işlem yapılamaz.");
                }

                // 1. XML YEDEĞİ KONTROLÜ
                var xmlLog = await _context.VardiyaXmlLoglari
                    .FirstOrDefaultAsync(x => x.VardiyaId == vardiyaId);

                bool xmlVar = xmlLog != null && (!string.IsNullOrEmpty(xmlLog.XmlIcerik) || (xmlLog.ZipDosyasi != null && xmlLog.ZipDosyasi.Length > 0));
                
                // 2. HAM VERİ KONTROLÜ (XML yoksa belki veriler silinmemiştir)
                bool veriVar = await _context.OtomasyonSatislar.AnyAsync(x => x.VardiyaId == vardiyaId);

                if (!xmlVar && !veriVar)
                {
                    // KRİTİK DURUM: Hem XML yok hem veriler silinmiş.
                    // Bu durumda 'Onay Bekliyor'a çekemeyiz çünkü hesaplama yapacak veri yok.
                    // ANCAK: Kullanıcının amacı genelde raporu düzeltmek.
                    // Mevcut Arşiv kaydındaki verilerle FARK'ı yeniden hesaplayıp düzeltiyoruz (In-Place Fix).
                    
                    var mevcutArsiv = await _context.VardiyaRaporArsivleri.FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);
                    if (mevcutArsiv != null)
                    {
                        // FIX: Giderleri de hesaba kat
                        var yeniFark = mevcutArsiv.TahsilatToplam + mevcutArsiv.FiloToplam + mevcutArsiv.GiderToplam - mevcutArsiv.SistemToplam;
                        
                        // Sadece fark değiştiyse güncelle
                        if (mevcutArsiv.Fark != yeniFark)
                        {
                            mevcutArsiv.Fark = yeniFark;
                            mevcutArsiv.FarkYuzde = mevcutArsiv.SistemToplam > 0 ? (yeniFark / mevcutArsiv.SistemToplam) * 100 : 0;
                            mevcutArsiv.GuncellemeTarihi = DateTime.UtcNow;
                            
                            // Vardiya tablosunu da güncelle
                            vardiya.Fark = yeniFark;
                            
                            await _context.SaveChangesAsync();
                             _logger.LogInformation("Vardiya {VardiyaId} için veri bulunamadı ancak arşiv FARK değeri düzeltildi.", vardiyaId);
                             return false; // False dönerek controller'a "Restore olmadı ama işlem bitti" mesajı vereceğiz (veya exception fırlatıp handle edeceğiz)
                        }
                    }

                    _logger.LogError("Vardiya {VardiyaId} için ne XML ne de ham veri bulundu. Arşiv de güncel.", vardiyaId);
                    throw new InvalidOperationException("Bu vardiya için yedek veri bulunamadı ve rapor zaten güncel. Geri alma işlemi yapılamaz.");
                }

                // ... Buraya geldiysek ya XML var ya da Veri var. İşleme devam ...

                // Arşivi bul (varsa silinecek)
                var arsiv = await _context.VardiyaRaporArsivleri
                    .FirstOrDefaultAsync(a => a.VardiyaId == vardiyaId);

                // Vardiya durumunu güncelle
                vardiya.Durum = VardiyaDurum.ONAY_BEKLIYOR;
                vardiya.Arsivlendi = false;
                vardiya.RaporArsivId = null;
                vardiya.OnaylayanId = null;
                vardiya.OnaylayanAdi = null;
                vardiya.OnayTarihi = null;
                vardiya.GuncellemeTarihi = DateTime.UtcNow;

                // Arşiv kaydını sil (varsa)
                if (arsiv != null)
                {
                    _context.VardiyaRaporArsivleri.Remove(arsiv);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Vardiya {VardiyaId} onayı kaldırıldı. İşlemi yapan: {UserName} ({UserId})",
                    vardiyaId, userName, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vardiya {VardiyaId} onayı kaldırılırken hata oluştu.", vardiyaId);
                throw;
            }
        }

        #region Private Helper Methods

        private async Task<Vardiya?> GetVardiyaWithDetails(int vardiyaId)
        {
            // AsSplitQuery: Büyük include'ları ayrı sorgulara böler
            // Bu, tek büyük JOIN yerine birden fazla küçük sorgu yapar
            // ve timeout'u önler
            return await _context.Vardiyalar
                .AsSplitQuery()  // 🚀 Performans optimizasyonu
                .Include(v => v.OtomasyonSatislar)
                    .ThenInclude(s => s.Yakit)
                .Include(v => v.FiloSatislar)
                    .ThenInclude(f => f.Yakit)
                .Include(v => v.Pusulalar)
                    .ThenInclude(p => p.DigerOdemeler)
                .Include(v => v.PompaEndeksleri)
                .Include(v => v.Giderler)
                .Include(v => v.VardiyaTankEnvanteri) // 🆕 Tank verileri
                .FirstOrDefaultAsync(v => v.Id == vardiyaId);
        }

        private VardiyaHesaplamaSonucu HesaplaRaporVerileri(Vardiya vardiya)
        {
            // Sistem (Otomasyon) Toplamı
            var sistemToplam = vardiya.OtomasyonSatislar.Sum(s => s.ToplamTutar);
            
            // Filo Toplamı
            var filoToplam = vardiya.FiloSatislar.Sum(f => f.Tutar);
            
            // Tahsilat Toplamı (Nakit + KK + Diğer Ödemeler)
            var tahsilatToplam = vardiya.Pusulalar.Sum(p => 
                p.Nakit + p.KrediKarti + (p.DigerOdemeler?.Sum(d => d.Tutar) ?? 0));
            
            // Gider Toplamı
            var giderToplam = vardiya.Giderler.Sum(g => g.Tutar);
            
            // Fark hesaplama
            var fark = tahsilatToplam + filoToplam + giderToplam - sistemToplam;
            var farkYuzde = sistemToplam > 0 ? (fark / sistemToplam) * 100 : 0;
            
            // Durum belirleme
            var durum = "UYUMLU";
            if (Math.Abs(fark) > 100) durum = "KRITIK_FARK";
            else if (Math.Abs(fark) > 1) durum = "FARK_VAR";

            // Pompa satış özetleri
            var pompaSatislari = vardiya.OtomasyonSatislar
                .GroupBy(s => new { s.PompaNo, YakitAdi = s.Yakit?.Ad ?? s.YakitTuru })
                .Select(g => new PompaSatisOzetDto
                {
                    PompaNo = g.Key.PompaNo,
                    YakitTuru = g.Key.YakitAdi,
                    Litre = g.Sum(s => s.Litre),
                    ToplamTutar = g.Sum(s => s.ToplamTutar),
                    IslemSayisi = g.Count()
                }).ToList();

            // Filo özetlerini de ekle
            var filoOzetleri = vardiya.FiloSatislar
                .GroupBy(f => new { f.PompaNo, YakitAdi = f.Yakit?.Ad ?? f.YakitTuru })
                .Select(g => new PompaSatisOzetDto
                {
                    PompaNo = g.Key.PompaNo,
                    YakitTuru = g.Key.YakitAdi,
                    Litre = g.Sum(f => f.Litre),
                    ToplamTutar = g.Sum(f => f.Tutar),
                    IslemSayisi = g.Count()
                }).ToList();

            // Personel fark raporu
            var personelFarklari = vardiya.OtomasyonSatislar
                .GroupBy(s => new { s.PersonelKeyId, s.PersonelAdi })
                .Select(g => new PersonelFarkDto
                {
                    PersonelKeyId = g.Key.PersonelKeyId,
                    PersonelAdi = g.Key.PersonelAdi,
                    Otomasyon = g.Sum(s => s.ToplamTutar)
                }).ToList();

            foreach (var p in personelFarklari)
            {
                var pusula = vardiya.Pusulalar.FirstOrDefault(ps => ps.PersonelAdi == p.PersonelAdi);
                if (pusula != null)
                {
                    p.Tahsilat = pusula.Nakit + pusula.KrediKarti + 
                        (pusula.DigerOdemeler?.Sum(d => d.Tutar) ?? 0);
                }
                p.Fark = p.Tahsilat - p.Otomasyon;
            }

            // Filo satışları için personel farkı
            if (filoToplam > 0)
            {
                personelFarklari.Add(new PersonelFarkDto
                {
                    PersonelAdi = "FİLO SATIŞLARI",
                    PersonelKeyId = "FILO",
                    Otomasyon = filoToplam,
                    Tahsilat = filoToplam,
                    Fark = 0
                });
            }

            // Karşılaştırma detayları
            var karsilastirmaDetaylar = new List<KarsilastirmaDetayDto>
            {
                new() { 
                    OdemeYontemi = "POMPACI_SATISI", 
                    SistemTutar = sistemToplam,
                    TahsilatTutar = tahsilatToplam,
                    Fark = tahsilatToplam - sistemToplam
                },
                new() { 
                    OdemeYontemi = "FILO", 
                    SistemTutar = filoToplam, 
                    TahsilatTutar = filoToplam,
                    Fark = 0
                }
            };

            // Tahsilat detayları
            var tahsilatDetay = new TahsilatDetayRaporu
            {
                Nakit = vardiya.Pusulalar.Sum(p => p.Nakit),
                KrediKarti = vardiya.Pusulalar.Sum(p => p.KrediKarti),
                DigerOdemeler = vardiya.Pusulalar
                    .SelectMany(p => p.DigerOdemeler ?? new List<PusulaDigerOdeme>())
                    .GroupBy(d => d.TurAdi)
                    .Select(g => new DigerOdemeOzet { OdemeTuru = g.Key, Tutar = g.Sum(x => x.Tutar) })
                    .ToList()
            };

            // Gider raporu
            var giderRaporu = vardiya.Giderler
                .Select(g => new GiderKalemi
                {
                    GiderTuru = g.GiderTuru,
                    Tutar = g.Tutar,
                    Aciklama = g.Aciklama,
                    BelgeTarihi = g.BelgeTarihi
                }).ToList();

            // 🆕 Personel Satış Detayları (Personel Karnesi için)
            var personelSatisDetay = vardiya.OtomasyonSatislar
                .GroupBy(s => new { s.PersonelKeyId, s.PersonelAdi })
                .Select(g => new PersonelSatisDetayDto
                {
                    PersonelKeyId = g.Key.PersonelKeyId,
                    PersonelAdi = g.Key.PersonelAdi,
                    Satislar = g.GroupBy(s => s.Yakit != null ? s.Yakit.Ad : s.YakitTuru)
                        .Select(yg => new YakitSatisDetayDto
                        {
                            YakitTuru = yg.Key,
                            Litre = yg.Sum(s => s.Litre),
                            Tutar = yg.Sum(s => s.ToplamTutar)
                        }).ToList()
                }).ToList();

            // 🆕 Filo Satış Detayları (Stok takibi için) - FIX: Group by Fleet Name, not Fuel Type
            var filoSatisDetay = vardiya.FiloSatislar
                .GroupBy(f => f.FiloKodu == "M-ODEM" ? "M-ODEM" : ((f.FiloAdi == null || f.FiloAdi == "") ? "OTOBIL" : f.FiloAdi))
                .Select(g => new FiloSatisDetayDto
                {
                    YakitTuru = g.Key, // Mapping Fleet Name to 'YakitTuru' property for report compatibility
                    Litre = g.Sum(f => f.Litre),
                    Tutar = g.Sum(f => f.Tutar)
                }).ToList();

            // Pompa satışlarını birleştir ve grupla (Otomasyon + Filo)
            var tumPompaSatislari = pompaSatislari.Concat(filoOzetleri)
                .GroupBy(p => new { p.PompaNo, p.YakitTuru })
                .Select(g => new PompaSatisOzetDto
                {
                    PompaNo = g.Key.PompaNo,
                    YakitTuru = g.Key.YakitTuru,
                    Litre = g.Sum(x => x.Litre),
                    ToplamTutar = g.Sum(x => x.ToplamTutar),
                    IslemSayisi = g.Sum(x => x.IslemSayisi)
                })
                .OrderBy(p => p.PompaNo)
                .ToList();

            // Karşılaştırma raporu
            var karsilastirmaRaporu = new KarsilastirmaRaporuDto
            {
                VardiyaId = vardiya.Id,
                Tarih = vardiya.BaslangicTarihi,
                SistemToplam = sistemToplam,
                TahsilatToplam = tahsilatToplam + filoToplam,
                Fark = fark,
                FarkYuzde = farkYuzde,
                Durum = durum,
                Detaylar = karsilastirmaDetaylar,
                PompaSatislari = tumPompaSatislari
            };

            // Fark raporu
            var farkRaporu = new FarkRaporItemDto
            {
                VardiyaId = vardiya.Id,
                Tarih = vardiya.BaslangicTarihi,
                DosyaAdi = vardiya.DosyaAdi ?? "",
                OtomasyonToplam = sistemToplam,
                TahsilatToplam = tahsilatToplam + filoToplam,
                Fark = fark,
                Durum = vardiya.Durum.ToString(),
                PersonelFarklari = personelFarklari
            };

            return new VardiyaHesaplamaSonucu
            {
                SistemToplam = sistemToplam,
                TahsilatToplam = tahsilatToplam + filoToplam,
                FiloToplam = filoToplam,
                GiderToplam = giderToplam,
                Fark = fark,
                FarkYuzde = farkYuzde,
                Durum = durum,
                KarsilastirmaRaporu = karsilastirmaRaporu,
                FarkRaporu = farkRaporu,
                PompaSatisRaporu = tumPompaSatislari,
                TahsilatDetay = tahsilatDetay,
                GiderRaporu = giderRaporu,
                PersonelSatisDetay = personelSatisDetay, // 🆕
                FiloSatisDetay = filoSatisDetay // 🆕
            };
        }

        #endregion
    }

    #region DTOs

    /// <summary>
    /// Arşiv listesi için özet DTO
    /// </summary>
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

    /// <summary>
    /// Hesaplama sonuçlarını tutan iç sınıf
    /// </summary>
    internal class VardiyaHesaplamaSonucu
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
        public List<PersonelSatisDetayDto> PersonelSatisDetay { get; set; } = new(); // 🆕
        public List<FiloSatisDetayDto> FiloSatisDetay { get; set; } = new(); // 🆕
    }

    public class PersonelSatisDetayDto
    {
        public string PersonelKeyId { get; set; } = string.Empty;
        public string PersonelAdi { get; set; } = string.Empty;
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
    }

    /// <summary>
    /// Tahsilat detayları için rapor yapısı
    /// </summary>
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
