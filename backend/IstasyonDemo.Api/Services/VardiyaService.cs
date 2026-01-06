using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;
using System.Xml.Linq;
using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace IstasyonDemo.Api.Services
{
    public class VardiyaService : IVardiyaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VardiyaService> _logger;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IVardiyaFinancialService _financialService;
        private readonly IYakitService _yakitService;
        private readonly VardiyaArsivService _arsivService;
        private readonly IServiceScopeFactory _scopeFactory;

        public VardiyaService(
            AppDbContext context, 
            ILogger<VardiyaService> logger, 
            IMapper mapper, 
            INotificationService notificationService, 
            IVardiyaFinancialService financialService, 
            IYakitService yakitService,
            VardiyaArsivService arsivService,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _notificationService = notificationService;
            _financialService = financialService;
            _yakitService = yakitService;
            _arsivService = arsivService;
            _scopeFactory = scopeFactory;
        }

        public async Task<Vardiya> CreateVardiyaAsync(CreateVardiyaDto dto, int userId, string? userRole, string? userName)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Vardiya oluştur
                var vardiya = _mapper.Map<Vardiya>(dto);
                
                _context.Vardiyalar.Add(vardiya);
                await _context.SaveChangesAsync();

                // 2. Otomasyon Satışlarını Ekle
                foreach (var satisDto in dto.OtomasyonSatislar)
                {
                    Personel? personel = null;

                    // 1. Önce bu Key ID şu an kimde var?
                    if (!string.IsNullOrEmpty(satisDto.PersonelKeyId))
                    {
                        var keyOwner = await _context.Personeller
                            .Where(p => p.IstasyonId == vardiya.IstasyonId)
                            .FirstOrDefaultAsync(p => p.KeyId == satisDto.PersonelKeyId);

                        if (keyOwner != null)
                        {
                            // İsimler eşleşiyor mu?
                            bool isNameMatch = keyOwner.AdSoyad == satisDto.PersonelAdi || keyOwner.OtomasyonAdi == satisDto.PersonelAdi;

                            if (isNameMatch)
                            {
                                personel = keyOwner; // Her şey yolunda, aynı kişi
                            }
                            else
                            {
                                // KEY TRANSFERİ SENARYOSU: Key var ama başka isme ait.
                                // KRONOLOJİ KONTROLÜ: Yüklenen vardiya, personelin son güncellemesinden YENİ mi?
                                bool isNewerUpdate = vardiya.BaslangicTarihi > (keyOwner.KeyGuncellemeTarihi ?? DateTime.MinValue);

                                if (isNewerUpdate)
                                {
                                    // SADECE GÜNCEL VERİYSE DEĞİŞTİR
                                    _logger.LogWarning($"Key Devri (GÜNCEL): Key {satisDto.PersonelKeyId} | {keyOwner.AdSoyad} -> {satisDto.PersonelAdi}");

                                    keyOwner.Aktif = false;
                                    keyOwner.EskiKeyId = keyOwner.KeyId;
                                    keyOwner.KeyId = null; 
                                    keyOwner.KeyGuncellemeTarihi = DateTime.UtcNow;
                                    // (Yeni sahibi aşağıda isimle aranacak/yaratılacak ve key atanacak)
                                }
                                else
                                {
                                    _logger.LogInformation($"Key Devri (GEÇMİŞ): {dto.DosyaAdi} dosyası eski. Mevcut sahip ({keyOwner.AdSoyad}) değiştirilmedi.");
                                    // Bu durumda keyOwner şu anki sahip, ama geçmiteki satış başkasına ait.
                                    // KeyOwner'ı personel olarak ATAMIYORUZ. Aşağıda isme göre gerçek sahibini arayacağız.
                                }
                            }
                        }
                    }

                    // 2. Eğer personel hala belirlenmediyse (Key yoktu, devredildi veya geçmiş dosya), İsme göre ara
                    if (personel == null)
                    {
                        personel = await _context.Personeller
                            .Where(p => p.IstasyonId == vardiya.IstasyonId)
                            .FirstOrDefaultAsync(p => 
                                (p.AdSoyad == satisDto.PersonelAdi) || 
                                (p.OtomasyonAdi == satisDto.PersonelAdi));
                        
                        // İsimle bulunduysa ve yeni bir Key ID geldiyse -> GÜNCELLE
                        if (personel != null && !string.IsNullOrEmpty(satisDto.PersonelKeyId) && personel.KeyId != satisDto.PersonelKeyId)
                        {
                             // KRONOLOJİ KONTROLÜ
                             bool isNewerUpdate = vardiya.BaslangicTarihi > (personel.KeyGuncellemeTarihi ?? DateTime.MinValue);
                             
                             if (isNewerUpdate)
                             {
                                _logger.LogWarning($"Personel/Key Güncelleme (GÜNCEL): {personel.AdSoyad} Key {personel.KeyId} -> {satisDto.PersonelKeyId}");
                                personel.EskiKeyId = personel.KeyId;
                                personel.KeyId = satisDto.PersonelKeyId;
                                personel.KeyGuncellemeTarihi = DateTime.UtcNow;
                                if (personel.KeyOlusturmaTarihi == null) personel.KeyOlusturmaTarihi = DateTime.UtcNow;
                             }
                             else
                             {
                                 _logger.LogInformation($"Personel/Key Güncelleme (GEÇMİŞ): Dosya eski olduğu için {personel.AdSoyad} anahtarı güncellenmedi.");
                             }
                        }
                    }

                    // 3. Hala yoksa -> YENİ PERSONEL
                    if (personel == null && !string.IsNullOrWhiteSpace(satisDto.PersonelAdi))
                    {
                        personel = new Personel
                        {
                            OtomasyonAdi = satisDto.PersonelAdi.Trim(),
                            AdSoyad = satisDto.PersonelAdi.Trim(),
                            KeyId = satisDto.PersonelKeyId,
                            Rol = PersonelRol.POMPACI,
                            Aktif = true,
                            IstasyonId = vardiya.IstasyonId,
                            KeyOlusturmaTarihi = DateTime.UtcNow
                        };
                        _context.Personeller.Add(personel);
                        await _context.SaveChangesAsync();
                    }
                    
                    // (Değişiklikleri kaydet - keyOwner pasife alınmış olabilir veya personel güncellenmiş olabilir)
                    await _context.SaveChangesAsync();

                    // Validation: Sanity Check
                    var calculatedTotal = satisDto.Litre * satisDto.BirimFiyat;
                    if (Math.Abs(calculatedTotal - satisDto.ToplamTutar) > 1.0m) // Increased tolerance to 1.0 TL for rounding
                    {
                         throw new InvalidOperationException($"Hatalı Satış Verisi: Pompa {satisDto.PompaNo} için {satisDto.Litre} lt * {satisDto.BirimFiyat} != {satisDto.ToplamTutar}");
                    }

                    var satis = _mapper.Map<OtomasyonSatis>(satisDto);
                    satis.VardiyaId = vardiya.Id;
                    satis.PersonelId = personel?.Id;
                    
                    _context.OtomasyonSatislar.Add(satis);
                }

                // 3. Filo Satışlarını Ekle
                foreach (var filoDto in dto.FiloSatislar)
                {
                    var filo = _mapper.Map<FiloSatis>(filoDto);
                    filo.VardiyaId = vardiya.Id;
                    _context.FiloSatislar.Add(filo);
                }

                // 3.b Pompa Endekslerini Ekle
                if (dto.PompaEndeksleri != null)
                {
                    foreach (var endeksDto in dto.PompaEndeksleri)
                    {
                        var endeks = new VardiyaPompaEndeks
                        {
                            VardiyaId = vardiya.Id,
                            PompaNo = endeksDto.PompaNo,
                            TabancaNo = endeksDto.TabancaNo,
                            YakitTuru = endeksDto.YakitTuru,
                            BaslangicEndeks = endeksDto.BaslangicEndeks,
                            BitisEndeks = endeksDto.BitisEndeks
                        };
                        _context.VardiyaPompaEndeksleri.Add(endeks);
                    }
                }

                // 3.a Tank Envanterini Ekle
                if (dto.TankEnvanterleri != null)
                {
                    foreach (var tankDto in dto.TankEnvanterleri)
                    {
                        var tank = new VardiyaTankEnvanteri
                        {
                            VardiyaId = vardiya.Id,
                            TankNo = tankDto.TankNo,
                            TankAdi = tankDto.TankAdi,
                            YakitTipi = tankDto.YakitTipi,
                            BaslangicStok = tankDto.BaslangicStok,
                            BitisStok = tankDto.BitisStok,
                            SatilanMiktar = tankDto.SatilanMiktar,
                            SevkiyatMiktar = tankDto.SevkiyatMiktar,
                            KayitTarihi = DateTime.UtcNow,
                            
                            // Hesaplanan Değerler (Basit logic)
                            BeklenenTuketim = tankDto.BaslangicStok + tankDto.SevkiyatMiktar - tankDto.BitisStok,
                            FarkMiktar = (tankDto.BaslangicStok + tankDto.SevkiyatMiktar - tankDto.BitisStok) - tankDto.SatilanMiktar
                        };
                        _context.VardiyaTankEnvanterleri.Add(tank);
                    }
                }

                await _context.SaveChangesAsync();

                await _context.SaveChangesAsync();

                // 4. OTOMATİK PUSULA OLUŞTURMA (Herkes İçin)
                // Vardiyada satışı olan her personel için Pusula kaydı oluştur (Boş olsa bile)
                var activePersonnelIds = await _context.OtomasyonSatislar
                    .Where(s => s.VardiyaId == vardiya.Id && s.PersonelId.HasValue)
                    .Select(s => s.PersonelId.Value)
                    .Distinct()
                    .ToListAsync();
                
                // M-ODEM listesindeki personelleri de ekle (eğer otomasyon satışında henüz yoksa)
                foreach(var mo in dto.MobilOdemeler) {
                    var p = await _context.Personeller.FirstOrDefaultAsync(x => x.IstasyonId == vardiya.IstasyonId && (x.KeyId == mo.PersonelKeyId || x.AdSoyad == mo.PersonelIsmi));
                    if(p != null && !activePersonnelIds.Contains(p.Id)) activePersonnelIds.Add(p.Id);
                }

                foreach (var personelId in activePersonnelIds)
                {
                    bool exists = await _context.Pusulalar.AnyAsync(p => p.VardiyaId == vardiya.Id && p.PersonelId == personelId);
                    if (!exists)
                    {
                        var personnel = await _context.Personeller.FindAsync(personelId);
                        if (personnel != null)
                        {
                            var newPusula = new Pusula
                            {
                                VardiyaId = vardiya.Id,
                                PersonelId = personnel.Id,
                                PersonelAdi = personnel.AdSoyad,
                                OlusturmaTarihi = DateTime.UtcNow,
                                DigerOdemeler = new List<PusulaDigerOdeme>()
                            };
                            _context.Pusulalar.Add(newPusula);
                        }
                    }
                }
                await _context.SaveChangesAsync();


                // 5. M-ODEM ve Paro Otomatik Tahsilat İşleme (PERSISTENCE RESTORED)
                // Kullanıcı talebi üzerine bu kayıtlar statik olarak PusulaDigerOdemeler tablosuna ekleniyor.
                // Bu sayede her sorguda tekrar hesaplanması gerekmiyor ve veri tutarlılığı sağlanıyor.

                // A. PARO PUAN (Otomasyon Satışlarından)
                var paroSatislar = dto.OtomasyonSatislar
                    .Where(s => s.PuanKullanimi > 0)
                    .GroupBy(s => s.PersonelAdi)
                    .Select(g => new { PersonelAdi = g.Key, ToplamPuan = g.Sum(s => s.PuanKullanimi) })
                    .ToList();

                foreach (var paro in paroSatislar)
                {
                    if (string.IsNullOrEmpty(paro.PersonelAdi)) continue;

                    // Pusulayı bul
                    var pusula = await _context.Pusulalar
                        .Include(p => p.DigerOdemeler)
                        .FirstOrDefaultAsync(p => p.VardiyaId == vardiya.Id && p.PersonelAdi == paro.PersonelAdi);

                    if (pusula != null)
                    {
                        pusula.DigerOdemeler.Add(new PusulaDigerOdeme
                        {
                            PusulaId = pusula.Id,
                            TurKodu = "POMPA_PARO_PUAN",
                            TurAdi = "Pompa Paro Puan",
                            Tutar = paro.ToplamPuan,
                            Silinemez = true // Sistem tarafından oluşturuldu
                        });
                    }
                }

                // B. M-ODEM (Otomasyon Satışlarından - MobilOdemeTutar Alanından)
                // ARTIK M-ODEM Filo olarak değil, Otomasyon satışı içinde işaretli geliyor.
                var mOdemSatislar = dto.OtomasyonSatislar
                    .Where(s => s.MobilOdemeTutar > 0)
                    .GroupBy(s => s.PersonelAdi)
                    .Select(g => new { PersonelAdi = g.Key, ToplamTutar = g.Sum(s => s.MobilOdemeTutar) })
                    .ToList();

                foreach (var modem in mOdemSatislar)
                {
                    if (string.IsNullOrEmpty(modem.PersonelAdi)) continue;

                    // Trimmed ve Case-Insensitive arama
                    var pusula = await _context.Pusulalar
                        .Include(p => p.DigerOdemeler)
                        .Where(p => p.VardiyaId == vardiya.Id)
                        .ToListAsync(); // Client-side evaluation for complex string comparison if needed, but simple Equals(OrdinalIgnoreCase) works in EF Core 5+ for some providers, but safe bet is usually Match via standard methods.
                    
                    // En güvenli yöntem: Bellekte eşleştir veya SQL'de ToUpper/ToLower kullan.
                    // Burada performans kritik değil (az sayıda personel var), bellekten bulalım.
                    var targetPusula = pusula.FirstOrDefault(p => 
                        p.PersonelAdi != null && 
                        p.PersonelAdi.Trim().Equals(modem.PersonelAdi.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (targetPusula != null)
                    {
                        // Aynı türden kayıt varsa ekleme (Double check)
                        if (!targetPusula.DigerOdemeler.Any(d => d.TurKodu == "MOBIL_ODEME"))
                        {
                            targetPusula.DigerOdemeler.Add(new PusulaDigerOdeme
                            {
                                PusulaId = targetPusula.Id,
                                TurKodu = "MOBIL_ODEME",
                                TurAdi = "Mobil Ödeme",
                                Tutar = modem.ToplamTutar,
                                Silinemez = true
                            });
                        }
                    }
                }

                
                await _context.SaveChangesAsync();
                
                var log = new VardiyaLog
                {
                    VardiyaId = vardiya.Id,
                    Islem = "OLUSTURULDU",
                    Aciklama = $"Vardiya dosyası yüklendi: {dto.DosyaAdi}",
                    KullaniciId = userId,
                    KullaniciAdi = userName ?? "",
                    KullaniciRol = userRole ?? "",
                    IslemTarihi = DateTime.UtcNow,
                    YeniDurum = VardiyaDurum.ACIK.ToString()
                };
                _context.VardiyaLoglari.Add(log);
                await _context.SaveChangesAsync();

                await _context.SaveChangesAsync();

                // 2.a. VardiyaXmlLog Kaydı (Manual Upload için)
                if (!string.IsNullOrEmpty(dto.DosyaIcerik))
                {
                    var xmlLog = new VardiyaXmlLog
                    {
                        IstasyonId = vardiya.IstasyonId,
                        VardiyaId = vardiya.Id,
                        DosyaAdi = dto.DosyaAdi ?? "Manual_Upload.xml",
                        XmlIcerik = null, // dto.DosyaIcerik yerine null atandı (Kullanıcı isteği)
                        YuklemeTarihi = DateTime.UtcNow
                        // Parse edip Tank/Pump detaylarını doldurabiliriz ama şu anlık raw content yeterli
                    };
                    _context.VardiyaXmlLoglari.Add(xmlLog);
                    await _context.SaveChangesAsync();
                }

                // Kullanıcıya bildirim gönder (Kendi işlemi)
                await _notificationService.NotifyUserAsync(
                    userId,
                    "Vardiya Oluşturuldu",
                    $"{dto.DosyaAdi} başarıyla yüklendi.",
                    "VARDIYA_OLUSTURULDU",
                    "success",
                    relatedVardiyaId: vardiya.Id
                );

                // Eğer işlemi yapan kişi Vardiya Sorumlusu değilse (örn: Admin, Patron veya Otomasyon),
                // o istasyonun Vardiya Sorumlularına bildirim gönder.
                if (userRole != "vardiya sorumlusu")
                {
                    var vardiyaSorumlulari = await _context.Users
                        .Include(u => u.Role)
                        .Where(u => u.IstasyonId == vardiya.IstasyonId && u.Role != null &&
                                   u.Role.Ad == "vardiya sorumlusu")
                        .ToListAsync();

                    foreach (var sorumlu in vardiyaSorumlulari)
                    {
                        // İşlemi yapan kişi sorumlu listesindeyse ona tekrar atma (zaten yukarıda attık)
                        if (sorumlu.Id == userId) continue;

                        await _notificationService.NotifyUserAsync(
                            sorumlu.Id,
                            "Yeni Veri Yüklendi",
                            $"Sisteme yeni vardiya verisi yüklendi: {dto.DosyaAdi}. Lütfen kontrol ediniz.",
                            "VARDIYA_OLUSTURULDU",
                            "info",
                            relatedVardiyaId: vardiya.Id
                        );
                    }
                }

                await transaction.CommitAsync();
                return vardiya;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating vardiya");
                throw;
            }
        }

        public async Task<Vardiya?> GetVardiyaByIdAsync(int id)
        {
             return await _context.Vardiyalar
                .AsNoTracking()
                .Include(v => v.OtomasyonSatislar)
                .Include(v => v.FiloSatislar)
                .Include(v => v.Pusulalar)
                    .ThenInclude(p => p.KrediKartiDetaylari)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<List<Vardiya>> GetOnayBekleyenlerAsync(int userId, string? userRole)
        {
            IQueryable<Vardiya> query = _context.Vardiyalar
                .Include(v => v.Istasyon)
                .Where(v => v.Durum == VardiyaDurum.ONAY_BEKLIYOR || v.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR);

            if (userRole == "patron")
            {
                query = query.Where(v => v.Istasyon != null && v.Istasyon.Firma != null && v.Istasyon.Firma.PatronId == userId);
            }

            return await query.OrderByDescending(v => v.BaslangicTarihi).ToListAsync();
        }

        public async Task OnayaGonderAsync(int id, int userId, string? userRole)
        {
            var vardiya = await _context.Vardiyalar
                .Include(v => v.Istasyon)
                .ThenInclude(i => i!.Firma)
                .Include(v => v.Istasyon)
                .ThenInclude(i => i!.Firma)
                // Removed heavy Includes (OtomasyonSatislar, FiloSatislar) for performance
                // Optimized: Only load Pusulalar for logic, others via aggregation
                .Include(v => v.Pusulalar)
                    .ThenInclude(p => p.DigerOdemeler)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            if (userRole != "admin")
            {
                var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
                if (user?.IstasyonId != vardiya.IstasyonId) throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok.");
            }

            if (vardiya.Durum != VardiyaDurum.ACIK && vardiya.Durum != VardiyaDurum.REDDEDILDI)
            {
                throw new InvalidOperationException("Sadece AÇIK veya REDDEDİLMİŞ vardiyalar onaya gönderilebilir.");
            }

            // --- Server-Side Validation & Recording: Calculate Shift Difference ---
    // UPDATED: Use centralized logic to prevent double counting and ensure consistency
    var financials = await CalculateVardiyaFinancials(id);
    var fark = financials.GenelOzet.Fark;

    vardiya.Fark = fark; // Save the difference
    
    // Log logic if needed, or just persist as we do above.
    if (Math.Abs(fark) > 0.5m)
    {
        // Just log to history for audit, but don't block
         await LogVardiyaIslem(
            vardiya.Id,
            "FARKLI_ONAY_ISTEGI",
            $"Vardiya fark ile onaya gönderildi. Fark: {fark:N2} TL",
            userId,
            "", 
            userRole,
            vardiya.Durum.ToString(),
            VardiyaDurum.ONAY_BEKLIYOR.ToString()
        );
    }
    // -------------------------------------------------------------

            vardiya.Durum = VardiyaDurum.ONAY_BEKLIYOR;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Kullanıcıya bildirim gönder (Kendi işlemi)
            await _notificationService.NotifyUserAsync(
                userId,
                "Onaya Gönderildi",
                $"{vardiya.DosyaAdi} onaya gönderildi.",
                "VARDIYA_ONAY_BEKLIYOR",
                "info",
                relatedVardiyaId: vardiya.Id
            );

            // Adminlere bildirim gönder
            await _notificationService.NotifyAdminsAsync(
                "Yeni Vardiya Onayı",
                $"{vardiya.DosyaAdi} için onay bekleniyor." + (Math.Abs(fark) > 0.5m ? $" (Fark: {fark:N2} TL)" : ""),
                "VARDIYA_ONAY_BEKLIYOR",
                "info",
                relatedVardiyaId: vardiya.Id
            );

            // Patron'a bildirim gönder
            if (vardiya.Istasyon?.Firma?.PatronId != null)
            {
                await _notificationService.NotifyUserAsync(
                    vardiya.Istasyon.Firma.PatronId.Value,
                    "Yeni Vardiya Onayı",
                    $"{vardiya.DosyaAdi} onayınızı bekliyor." + (Math.Abs(fark) > 0.5m ? $" (Fark: {fark:N2} TL)" : ""),
                    "VARDIYA_ONAY_BEKLIYOR",
                    "info",
                    relatedVardiyaId: vardiya.Id
                );
            }
        }

        public async Task SilmeTalebiOlusturAsync(int id, SilmeTalebiDto dto, int userId, string? userRole, string? userName)
        {
            var vardiya = await _context.Vardiyalar.Include(v => v.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(v => v.Id == id);
            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            if (userRole == "patron" && (vardiya.Istasyon == null || vardiya.Istasyon.Firma == null || vardiya.Istasyon.Firma.PatronId != userId)) throw new UnauthorizedAccessException();
            if (userRole != "admin" && userRole != "patron")
            {
                var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
                if (user?.IstasyonId != vardiya.IstasyonId) throw new UnauthorizedAccessException();
            }

            if (vardiya.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR)
            {
                 throw new InvalidOperationException("Zaten silinme onayı bekliyor.");
            }

            if (userRole != "admin" && userRole != "patron" && vardiya.Durum == VardiyaDurum.ONAYLANDI)
            {
                throw new InvalidOperationException("Onaylanmış vardiyalar sorumlular tarafından silinemez.");
            }

            var eskiDurum = vardiya.Durum.ToString();
            
            vardiya.Durum = VardiyaDurum.SILINME_ONAYI_BEKLIYOR;
            vardiya.SilinmeTalebiNedeni = dto.Nedeni;
            vardiya.SilinmeTalebiOlusturanId = userId;
            vardiya.SilinmeTalebiOlusturanAdi = userName;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await LogVardiyaIslem(
                vardiya.Id,
                "SILME_TALEP_EDILDI",
                $"Silme nedeni: {dto.Nedeni}",
                userId,
                userName,
                userRole,
                eskiDurum,
                VardiyaDurum.SILINME_ONAYI_BEKLIYOR.ToString()
            );

            // Kullanıcıya bildirim gönder (Kendi işlemi)
            await _notificationService.NotifyUserAsync(
                userId,
                "Silme Talebi Oluşturuldu",
                $"{vardiya.DosyaAdi} için silme talebi oluşturuldu.",
                "VARDIYA_SILME_ONAYI_BEKLIYOR",
                "warn",
                relatedVardiyaId: vardiya.Id
            );

            // Adminlere bildirim gönder
            await _notificationService.NotifyAdminsAsync(
                "Vardiya Silme Talebi",
                $"{vardiya.DosyaAdi} için silme onayı bekleniyor. Neden: {dto.Nedeni}",
                "VARDIYA_SILME_ONAYI_BEKLIYOR",
                "warn",
                relatedVardiyaId: vardiya.Id
            );

            // Patron'a bildirim gönder
            if (vardiya.Istasyon?.Firma?.PatronId != null)
            {
                await _notificationService.NotifyUserAsync(
                    vardiya.Istasyon.Firma.PatronId.Value,
                    "Vardiya Silme Talebi",
                    $"{vardiya.DosyaAdi} silinmek isteniyor. Neden: {dto.Nedeni}",
                    "VARDIYA_SILME_ONAYI_BEKLIYOR",
                    "warn",
                    relatedVardiyaId: vardiya.Id
                );
            }
        }

        public async Task OnaylaAsync(int id, OnayDto dto, int userId, string? userRole)
        {
            var vardiya = await _context.Vardiyalar.Include(v => v.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(v => v.Id == id);
            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            if (userRole == "patron" && (vardiya.Istasyon == null || vardiya.Istasyon.Firma == null || vardiya.Istasyon.Firma.PatronId != userId)) throw new UnauthorizedAccessException();

            if (vardiya.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR)
            {
                // Soft Delete
                vardiya.Durum = VardiyaDurum.SILINDI;
                vardiya.OnaylayanId = dto.OnaylayanId;
                vardiya.OnaylayanAdi = dto.OnaylayanAdi;
                vardiya.OnayTarihi = DateTime.UtcNow;
                vardiya.GuncellemeTarihi = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                await LogVardiyaIslem(
                    vardiya.Id,
                    "SILINDI",
                    $"Silme işlemi onaylandı. Onaylayan: {dto.OnaylayanAdi}",
                    userId,
                    dto.OnaylayanAdi,
                    userRole,
                    VardiyaDurum.SILINME_ONAYI_BEKLIYOR.ToString(),
                    VardiyaDurum.SILINDI.ToString()
                );

                // Silme talebini oluşturan kişiye bildirim gönder
                if (vardiya.SilinmeTalebiOlusturanId.HasValue)
                {
                    await _notificationService.NotifyUserAsync(
                        vardiya.SilinmeTalebiOlusturanId.Value,
                        "Vardiya Silindi",
                        $"{vardiya.DosyaAdi} silme talebiniz onaylandı.",
                        "VARDIYA_SILME_ONAYLANDI",
                        "success",
                        relatedVardiyaId: vardiya.Id
                    );
                }
                return;
            }

            if (vardiya.Durum != VardiyaDurum.ONAY_BEKLIYOR)
            {
                throw new InvalidOperationException("Sadece ONAY BEKLEYEN veya SILINME ONAYI BEKLEYEN vardiyalar onaylanabilir.");
            }

            // 🔒 ATOMIK İŞLEM: Tüm onay işlemleri tek transaction içinde
            // Finansal + Arşivleme birlikte başarılı veya birlikte geri alınır
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                vardiya.Durum = VardiyaDurum.ONAYLANDI;
                vardiya.OnaylayanId = dto.OnaylayanId;
                vardiya.OnaylayanAdi = dto.OnaylayanAdi;
                vardiya.OnayTarihi = DateTime.UtcNow;
                vardiya.GuncellemeTarihi = DateTime.UtcNow;

                // 1. Finansal İşlemleri Tetikle (Veresiye varsa Cari Hareket oluştur)
                await _financialService.ProcessVardiyaApproval(vardiya.Id, dto.OnaylayanId);

                // 2. ARŞİVLEME: Onaylanan vardiyayı arşivle
                // Tüm rapor verileri hesaplanıp JSON olarak saklanacak
                await _arsivService.ArsivleVardiya(vardiya.Id, dto.OnaylayanId, dto.OnaylayanAdi ?? "");
                _logger.LogInformation("Vardiya {VardiyaId} başarıyla arşivlendi.", vardiya.Id);

                await _context.SaveChangesAsync();
                
                // Tüm işlemler başarılı, commit et
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // Herhangi bir hata olursa tüm işlemi geri al
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Vardiya {VardiyaId} onaylanırken hata oluştu, işlem geri alındı.", vardiya.Id);
                throw new InvalidOperationException($"Vardiya onaylama işlemi başarısız: {ex.Message}", ex);
            }

            // 🗑️ Ham verileri temizle (Transaction dışında - ayrı işlem)
            // Bu işlem başarısız olsa bile onay geçerli kalır
            // Fire-and-forget: Kullanıcıyı bekletmemek ve bağlantı kopsa bile işlemin devam etmesi için arka planda çalıştır
            _ = Task.Run(async () => 
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedArsivService = scope.ServiceProvider.GetRequiredService<VardiyaArsivService>();
                    await scopedArsivService.TemizleHamVeriler(vardiya.Id);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Vardiya {VardiyaId} ham verileri temizlenirken hata oluştu (Arka Plan).", vardiya.Id);
                }
            });
            
            // Transaction dışındaki işlemler (loglama ve bildirimler)
            // Bunlar kritik değil, hata olsa bile onay geçerli kalır
            try
            {
                await LogVardiyaIslem(
                    vardiya.Id,
                    "ONAYLANDI",
                    $"Vardiya onaylandı. Onaylayan: {dto.OnaylayanAdi}",
                    userId,
                    dto.OnaylayanAdi,
                    userRole,
                    VardiyaDurum.ONAY_BEKLIYOR.ToString(),
                    VardiyaDurum.ONAYLANDI.ToString()
                );

                // Vardiyayı oluşturan kişiye bildirim gönder
                var olusturanLog = await _context.VardiyaLoglari
                    .Where(l => l.VardiyaId == vardiya.Id && l.Islem == "OLUSTURULDU")
                    .OrderByDescending(l => l.IslemTarihi)
                    .FirstOrDefaultAsync();

                if (olusturanLog != null && olusturanLog.KullaniciId.HasValue)
                {
                    await _notificationService.NotifyUserAsync(
                        olusturanLog.KullaniciId.Value,
                        "Vardiya Onaylandı",
                        $"{vardiya.DosyaAdi} onaylandı.",
                        "VARDIYA_ONAYLANDI",
                        "success",
                        relatedVardiyaId: vardiya.Id
                    );
                }
            }
            catch (Exception logEx)
            {
                // Loglama/bildirim hatası kritik değil, sadece logla
                _logger.LogWarning(logEx, "Vardiya {VardiyaId} onay sonrası loglama/bildirim hatası.", vardiya.Id);
            }
        }

        public async Task ReddetAsync(int id, RedDto dto, int userId, string? userRole)
        {
            var vardiya = await _context.Vardiyalar.Include(v => v.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(v => v.Id == id);
            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            if (userRole == "patron" && (vardiya.Istasyon == null || vardiya.Istasyon.Firma == null || vardiya.Istasyon.Firma.PatronId != userId)) throw new UnauthorizedAccessException();

            if (vardiya.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR)
            {
                // Revert to ACIK so it's not deleted
                vardiya.Durum = VardiyaDurum.ACIK;
                vardiya.RedNedeni = dto.RedNedeni;
                vardiya.OnaylayanId = dto.OnaylayanId;
                vardiya.OnaylayanAdi = dto.OnaylayanAdi;
                vardiya.GuncellemeTarihi = DateTime.UtcNow;
                
                // Clear delete request info
                var silmeTalepEdenId = vardiya.SilinmeTalebiOlusturanId;
                vardiya.SilinmeTalebiNedeni = null;
                vardiya.SilinmeTalebiOlusturanId = null;
                vardiya.SilinmeTalebiOlusturanAdi = null;

                await _context.SaveChangesAsync();

                await LogVardiyaIslem(
                    vardiya.Id,
                    "SILME_REDDEDILDI",
                    $"Silme talebi reddedildi. Neden: {dto.RedNedeni}",
                    userId,
                    dto.OnaylayanAdi,
                    userRole,
                    VardiyaDurum.SILINME_ONAYI_BEKLIYOR.ToString(),
                    VardiyaDurum.ACIK.ToString()
                );

                if (silmeTalepEdenId.HasValue)
                {
                    await _notificationService.NotifyUserAsync(
                        silmeTalepEdenId.Value,
                        "Silme Talebi Reddedildi",
                        $"{vardiya.DosyaAdi} silme talebiniz reddedildi. Neden: {dto.RedNedeni}",
                        "VARDIYA_SILME_REDDEDILDI",
                        "error",
                        relatedVardiyaId: vardiya.Id
                    );
                }
                return;
            }

            if (vardiya.Durum != VardiyaDurum.ONAY_BEKLIYOR)
            {
                throw new InvalidOperationException("Sadece ONAY BEKLEYEN vardiyalar reddedilebilir.");
            }

            vardiya.Durum = VardiyaDurum.REDDEDILDI;
            vardiya.RedNedeni = dto.RedNedeni;
            vardiya.OnaylayanId = dto.OnaylayanId;
            vardiya.OnaylayanAdi = dto.OnaylayanAdi;
            vardiya.OnayTarihi = DateTime.UtcNow;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogVardiyaIslem(
                vardiya.Id,
                "REDDEDILDI",
                $"Vardiya reddedildi. Neden: {dto.RedNedeni}",
                userId,
                dto.OnaylayanAdi,
                userRole,
                VardiyaDurum.ONAY_BEKLIYOR.ToString(),
                VardiyaDurum.REDDEDILDI.ToString()
            );

            // Vardiyayı oluşturan kişiye bildirim gönder
            var olusturanLog = await _context.VardiyaLoglari
                .Where(l => l.VardiyaId == vardiya.Id && l.Islem == "OLUSTURULDU")
                .OrderByDescending(l => l.IslemTarihi)
                .FirstOrDefaultAsync();

            if (olusturanLog != null && olusturanLog.KullaniciId.HasValue)
            {
                await _notificationService.NotifyUserAsync(
                    olusturanLog.KullaniciId.Value,
                    "Vardiya Reddedildi",
                    $"{vardiya.DosyaAdi} reddedildi. Neden: {dto.RedNedeni}",
                    "VARDIYA_REDDEDILDI",
                    "error",
                    relatedVardiyaId: vardiya.Id
                );
            }
        }

        private async Task LogVardiyaIslem(int vardiyaId, string islem, string aciklama, int userId, string? userName, string? userRole, string? eskiDurum, string? yeniDurum)
        {
            var log = new VardiyaLog
            {
                VardiyaId = vardiyaId,
                Islem = islem,
                Aciklama = aciklama,
                KullaniciId = userId,
                KullaniciAdi = userName ?? "",
                KullaniciRol = userRole ?? "",
                IslemTarihi = DateTime.UtcNow,
                EskiDurum = eskiDurum,
                YeniDurum = yeniDurum
            };
            _context.VardiyaLoglari.Add(log);
            await _context.SaveChangesAsync();
        }
        public async Task ProcessXmlZipAsync(Stream zipStream, string fileName, int userId, string? userRole, string? userName)
        {
            try
            {
                // 1. ZIP'i Belleğe (byte[]) Al - Veritabanında saklamak için
                using var memoryStream = new MemoryStream();
                await zipStream.CopyToAsync(memoryStream);
                var zipBytes = memoryStream.ToArray();

                // 0. Dosya Hash Hesapla ve Mükerrer Kontrolü (PROD-READY)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(zipBytes);
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                // Silinmiş olanları hariç tut, aktif bir kayıt varsa mükerrer say.
                bool isDuplicate = await _context.Vardiyalar.AnyAsync(v => v.DosyaHash == hashString && v.Durum != VardiyaDurum.SILINDI);
                if (isDuplicate)
                {
                    _logger.LogWarning($"Mükerrer Dosya Yükleme Girişimi Engellendi. Hash: {hashString}, Dosya: {fileName}");
                    throw new InvalidOperationException($"Bu dosya daha önce yüklenmiş! (Hash: {hashString})");
                }

                // ZIP Çıkart
                using var archive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
                
                var xmlEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

                if (xmlEntry == null)
                    throw new InvalidOperationException("ZIP dosyası içinde .xml uzantılı dosya bulunamadı.");

                // 2. XML'i Belleğe Al (Parsing için string olarak)
                string xmlContent;
                using (var stream = xmlEntry.Open())
                using (var reader = new StreamReader(stream, System.Text.Encoding.GetEncoding("windows-1254")))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                var xdoc = XDocument.Parse(xmlContent);

                // 2. İstasyon ve Ayarları Çözümle - Namespace Agnostic
                var globalParams = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "GlobalParams");
                // Element lookups also need to be namespace-agnostic if they are direct children
                var stationCode = globalParams?.Elements().FirstOrDefault(x => x.Name.LocalName == "StationCode")?.Value;
                var swVersion = globalParams?.Elements().FirstOrDefault(x => x.Name.LocalName == "Version")?.Value;

                Istasyon? station = null;

                if (!string.IsNullOrEmpty(stationCode))
                {
                    station = await _context.Istasyonlar.FirstOrDefaultAsync(i => i.IstasyonKodu == stationCode);
                }

                if (station == null)
                {
                    // Kullanıcının istasyonuna fallback
                    var user = await _context.Users.FindAsync(userId);
                    if (user?.IstasyonId != null)
                    {
                        station = await _context.Istasyonlar.FindAsync(user.IstasyonId);
                        
                        // İlk kez eşleşiyorsa kodu kaydet
                        if (station != null && string.IsNullOrEmpty(station.IstasyonKodu) && !string.IsNullOrEmpty(stationCode))
                        {
                            station.IstasyonKodu = stationCode;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                if (station == null)
                    throw new InvalidOperationException($"İstasyon tanımlanamadı! XML StationCode: {stationCode}");

                _logger.LogInformation($"XML Dosyası İşleniyor: {fileName}, Boyut: {xmlContent.Length} bytes");
                _logger.LogInformation($"İstasyon Bulundu: {station.Ad} ({station.IstasyonKodu})");

                // 3. İstasyon Ayarlarını Güncelle (GlobalParams)
                // 3. İstasyon Ayarlarını Güncelle (GlobalParams) - ARTIK AKTİF
                if (globalParams != null)
                {
                    var version = globalParams.Elements().FirstOrDefault(x => x.Name.LocalName == "Version")?.Value;
                    var unitPriceDecimalStr = globalParams.Elements().FirstOrDefault(x => x.Name.LocalName == "UnitPriceDecimal")?.Value;
                    var amountDecimalStr = globalParams.Elements().FirstOrDefault(x => x.Name.LocalName == "AmountDecimal")?.Value;
                    var totalDecimalStr = globalParams.Elements().FirstOrDefault(x => x.Name.LocalName == "TotalDecimal")?.Value;

                    int.TryParse(unitPriceDecimalStr, out int unitPriceDecimal);
                    int.TryParse(amountDecimalStr, out int amountDecimal);
                    int.TryParse(totalDecimalStr, out int totalDecimal);

                    // Varsayılan değerler (eğer parse edilemezse 2 kullan)
                    if (unitPriceDecimal == 0) unitPriceDecimal = 2;
                    if (amountDecimal == 0) amountDecimal = 2;
                    if (totalDecimal == 0) totalDecimal = 2;

                    var ayarlar = await _context.IstasyonAyarlari.FirstOrDefaultAsync(a => a.IstasyonId == station.Id);
                    if (ayarlar == null)
                    {
                        ayarlar = new IstasyonAyarlari
                        {
                            IstasyonId = station.Id,
                            Version = version,
                            UnitPriceDecimal = unitPriceDecimal,
                            AmountDecimal = amountDecimal,
                            TotalDecimal = totalDecimal,
                            GuncellemeTarihi = DateTime.UtcNow
                        };
                        _context.IstasyonAyarlari.Add(ayarlar);
                    }
                    else
                    {
                        ayarlar.Version = version;
                        ayarlar.UnitPriceDecimal = unitPriceDecimal;
                        ayarlar.AmountDecimal = amountDecimal;
                        ayarlar.TotalDecimal = totalDecimal;
                        ayarlar.GuncellemeTarihi = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"İstasyon Ayarları Güncellendi: Ver={version}, Decimals={unitPriceDecimal}/{amountDecimal}/{totalDecimal}");
                }

                // 4. Detayları JSON olarak hazırla (KALDIRILDI)
                /*
                var tanks = xdoc.Descendants().Where(x => x.Name.LocalName == "TankDetails").Select(t => new 
                {
                    TankNo = t.Elements().FirstOrDefault(x => x.Name.LocalName == "TankNo")?.Value,
                    FuelType = t.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value,
                    Volume = t.Elements().FirstOrDefault(x => x.Name.LocalName == "CurrentVolume")?.Value
                }).ToList();

                var pumps = xdoc.Descendants().Where(x => x.Name.LocalName == "PumpDetails").Select(p => new
                {
                    PumpNr = p.Elements().FirstOrDefault(x => x.Name.LocalName == "PumpNr")?.Value,
                    Volume = p.Elements().FirstOrDefault(x => x.Name.LocalName == "Volume")?.Value,
                    Amount = p.Elements().FirstOrDefault(x => x.Name.LocalName == "Amount")?.Value
                }).ToList();
                */

                var rawTxns = xdoc.Descendants().Where(x => x.Name.LocalName.Equals("Txn", StringComparison.OrdinalIgnoreCase)).ToList();
                _logger.LogInformation($"XML Parsing Debug: Toplam {rawTxns.Count} adet 'Txn' elementi bulundu.");

                if (rawTxns.Any())
                {
                    var firstTxn = rawTxns.First();
                    _logger.LogInformation($"İlk Txn Debug: {firstTxn}");
                    var saleDebug = firstTxn.Elements().FirstOrDefault(x => x.Name.LocalName == "SaleDetails");
                    _logger.LogInformation($"İlk Txn SaleDetails bulundu mu?: {saleDebug != null}");
                    if(saleDebug != null)
                    {
                        var receiptDebug = saleDebug.Elements().FirstOrDefault(x => x.Name.LocalName == "ReceiptNr");
                        _logger.LogInformation($"İlk Txn ReceiptNr Değeri: {receiptDebug?.Value}");
                    }
                }

                var txnList = rawTxns
                    .Select(txn => {
                        var s = txn.Elements().FirstOrDefault(x => x.Name.LocalName == "SaleDetails");
                        var tag = txn.Elements().FirstOrDefault(x => x.Name.LocalName == "TagDetails");
                        
                        return new {
                            ReceiptNr = s?.Elements().FirstOrDefault(x => x.Name.LocalName == "ReceiptNr")?.Value,
                            DateTime = s?.Elements().FirstOrDefault(x => x.Name.LocalName == "DateTime")?.Value,
                            Total = s?.Elements().FirstOrDefault(x => x.Name.LocalName == "Total")?.Value,
                            Plate = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value 
                                    ?? s?.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRPlate")?.Value
                        };
                    })
                    .Where(x => x.ReceiptNr != null) // Filter out empty parses
                    .ToList();

                _logger.LogInformation($"XML İstatistikleri: {rawTxns.Count} Satış bulundu.");

                // 5. VardiyaXmlLog Kaydı
                var xmlLog = new VardiyaXmlLog
                {
                    IstasyonId = station.Id,
                    DosyaAdi = fileName,
                    ZipDosyasi = zipBytes,
                    XmlIcerik = null, // xmlContent yerine null atandı (Kullanıcı isteği)
                    YuklemeTarihi = DateTime.UtcNow
                };

                _context.VardiyaXmlLoglari.Add(xmlLog);
                await _context.SaveChangesAsync(); 

                // 6. Standart Vardiya Oluşturma
                var header = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "Header");
                
                string dateFormat = "yyyyMMddHHmmss";
                DateTime ParseDate(string? val) 
                {
                    if (string.IsNullOrEmpty(val)) return DateTime.UtcNow;
                    if (val.Length == 8) val += "000000"; 
                    return DateTime.ParseExact(val, dateFormat, CultureInfo.InvariantCulture);
                }

                DateTime baslangic;
                DateTime? bitis;

                if (header != null)
                {
                    var startDateStr = header.Elements().FirstOrDefault(x => x.Name.LocalName == "ShiftStartTime")?.Value 
                                     ?? header.Elements().FirstOrDefault(x => x.Name.LocalName == "ReportDate")?.Value;
                    var endDateStr = header.Elements().FirstOrDefault(x => x.Name.LocalName == "ShiftEndTime")?.Value;

                    baslangic = ParseDate(startDateStr);
                    bitis = !string.IsNullOrEmpty(endDateStr) ? ParseDate(endDateStr) : null;
                }
                else
                {
                    // Header yoksa transactionlardan tarih bul
                    var validDates = txnList
                        .Where(t => !string.IsNullOrEmpty(t.DateTime))
                        .Select(t => ParseDate(t.DateTime))
                        .OrderBy(d => d)
                        .ToList();

                    if (validDates.Any())
                    {
                        baslangic = validDates.First();
                        bitis = validDates.Last();
                        _logger.LogInformation($"Tarihler Satışlardan Türetildi: Başlangıç={baslangic}, Bitiş={bitis}");
                    }
                    else
                    {
                        baslangic = DateTime.UtcNow; 
                        bitis = null;
                        _logger.LogWarning("Hiçbir satışta tarih bulunamadı! Başlangıç şu anki zaman olarak ayarlandı.");
                    }
                }

                var dto = new CreateVardiyaDto
                {
                    IstasyonId = station.Id,
                    BaslangicTarihi = baslangic,
                    BitisTarihi = bitis,
                    DosyaAdi = fileName,
                    DosyaHash = hashString,
                    OtomasyonSatislar = new List<CreateOtomasyonSatisDto>()
                };

                // 6.1. Yakıt Tanımlarını Hazırla (Dynamic Mapping)
                var yakitlar = await _context.Yakitlar.AsNoTracking().ToListAsync();
                var yakitMap = new Dictionary<string, Yakit>();

                foreach (var y in yakitlar)
                {
                    if (!string.IsNullOrEmpty(y.TurpakUrunKodu))
                    {
                        var codes = y.TurpakUrunKodu.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var code in codes)
                        {
                            var trimmed = code.Trim();
                            if (!yakitMap.ContainsKey(trimmed))
                            {
                                yakitMap[trimmed] = y;
                            }
                        }
                    }
                }

                foreach (var txn in rawTxns)
                {
                    var sale = txn.Elements().FirstOrDefault(x => x.Name.LocalName == "SaleDetails");
                    if (sale == null) continue;
                    
                    var tag = txn.Elements().FirstOrDefault(x => x.Name.LocalName == "TagDetails");
                    var fleetCode = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "FleetCode")?.Value;

                    var fuelTypeRaw = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value;
                    
                    // Dynamic Fuel Mapping
                    Yakit? matchedYakit = null;
                    string fuelTypeStr = "DIGER";
                    int? yakitId = null;

                    if (!string.IsNullOrEmpty(fuelTypeRaw) && yakitMap.ContainsKey(fuelTypeRaw))
                    {
                        matchedYakit = yakitMap[fuelTypeRaw];
                        fuelTypeStr = matchedYakit.Ad; // Use standardized name
                        yakitId = matchedYakit.Id;
                    }

                    
                    var satisTarihi = ParseDate(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "DateTime")?.Value);

                    var amountRaw = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Amount")?.Value ?? "0", CultureInfo.InvariantCulture);
                    var priceRaw = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "UnitPrice")?.Value ?? "0", CultureInfo.InvariantCulture);
                    var totalRaw = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Total")?.Value ?? "0", CultureInfo.InvariantCulture);

                    // Dynamic Code = İstasyon/Pompacı Satışı -> OtomasyonSatislar
                    string dynamicCode = !string.IsNullOrWhiteSpace(station!.OtomasyonFiloKodu) ? station.OtomasyonFiloKodu.Trim() : "C0000";
                    var fleetCodeCheck = fleetCode?.Trim();
                    
                    
                    // Paro Puan Kontrolü (Redemption > 0 && LoyaltyCardNo dolu)
                    var redemptionRaw = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Redemption")?.Value ?? "0", CultureInfo.InvariantCulture);
                    var loyaltyCardNo = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "LoyaltyCardNo")?.Value;
                    bool isParoSale = redemptionRaw > 0 && !string.IsNullOrWhiteSpace(loyaltyCardNo);

                    if (isParoSale)
                    {
                        var personelName = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value ?? "";
                        var personelKey = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "TagNr")?.Value ?? "";

                        // CHANGED: We STOP adding MobilOdemeDto manually for Paro to avoid double counting.
                        // We will rely on PuanKullanimi field in OtomasyonSatis table for dynamic calculation.
                        /*
                        dto.MobilOdemeler.Add(new MobilOdemeDto 
                        {
                            PersonelIsmi = personelName,
                            PersonelKeyId = personelKey,
                            Tutar = redemptionRaw / 100m,
                            Aciklama = $"Pompa Paro Puan (Kart: {loyaltyCardNo})",
                            TurKodu = "PARO_PUAN_POMPA",
                            Silinemez = true
                        });
                        */
                    }

                    bool isAutomationSale = string.IsNullOrEmpty(fleetCodeCheck) || 
                                            fleetCodeCheck.Equals(dynamicCode, StringComparison.OrdinalIgnoreCase) ||
                                            isParoSale; // Paro satışları da otomasyon satışı sayılmalı

                    // M-ODEM (Mobil Ödeme) Check -> Treat as Automation Sale + Auto Payment
                    // CHANGED: We now want M-ODEM to stay in OtomasyonSatis, but populate "MobilOdemeTutar"
                    // CHANGED: We now want M-ODEM to stay in OtomasyonSatis, but populate "MobilOdemeTutar"
                    bool isMOdemSale = fleetCodeCheck != null && fleetCodeCheck.Equals("M-ODEM", StringComparison.OrdinalIgnoreCase);
                    
                    if (isMOdemSale)
                    {
                        isAutomationSale = true; // Ensure it enters the Automation block
                    }

                    if (isAutomationSale)
                    {
                        dto.OtomasyonSatislar.Add(new CreateOtomasyonSatisDto
                        {
                            PompaNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "PumpNr")?.Value ?? "0"),

                            YakitTuru = fuelTypeStr,
                            YakitId = yakitId,
                            Litre = amountRaw / 100m,
                            BirimFiyat = priceRaw / 100m,
                            ToplamTutar = totalRaw / 100m,
                            SatisTarihi = satisTarihi,
                            FisNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ReceiptNr")?.Value ?? "0"),
                            // Personel Ismi Mapping (TagDetails.Plate -> PersonelAdi)
                            PersonelAdi = (tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value ?? "").Trim(), 
                            // Personel Key ID Mapping (TagDetails.TagNr)
                            PersonelKeyId = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "TagNr")?.Value ?? "",
                            // Plaka Mapping Önceliği: ECRPlate > Sale.Plate >> NOT TagDetails.Plate (O Personel Ismi)
                            Plaka = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRPlate")?.Value
                                    ?? sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value,
                            
                            // Yeni Alanlar
                            FiloAdi = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "FleetName")?.Value ?? "",
                            TagNr = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "TagNr")?.Value ?? "", // Explicit TagNr
                            MotorSaati = int.Parse(tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "EngineHour")?.Value ?? "0"),
                            Kilometre = int.Parse(tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Odometer")?.Value ?? "0"),
                            SatisTuru = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "TxnType")?.Value ?? "0"),
                            TabancaNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "NozzleNr")?.Value ?? "0"),
                            OdemeTuru = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "PaymentType")?.Value ?? "0"),
                            YazarKasaPlaka = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRPlate")?.Value ?? "",
                            YazarKasaFisNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRReceiptNr")?.Value ?? "0"),
                            PuanKullanimi = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Redemption")?.Value.Replace(",", ".") ?? "0", CultureInfo.InvariantCulture),
                            IndirimTutar = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "DiscountAmount")?.Value.Replace(",", ".") ?? "0", CultureInfo.InvariantCulture),
                            KazanilanPuan = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "EarnedPoints")?.Value ?? "0"),
                            KazanilanPara = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "EarnedMoney")?.Value.Replace(",", ".") ?? "0", CultureInfo.InvariantCulture),
                            SadakatKartNo = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "LoyaltyCardNo")?.Value,
                            SadakatKartTipi = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "LoyaltyCardType")?.Value ?? "0"),

                            TamBirimFiyat = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "FullUnitPrice")?.Value ?? "0") / 100m, // Usually same format as UnitPrice
                            
                            // M-ODEM Mapping
                            MobilOdemeTutar = isMOdemSale ? (totalRaw / 100m) : 0
                        });
                    }
                    else
                    {
                        // Diğerleri -> FiloSatislar
                        string plakaVal;
                        
                        // M-ODEM veya PARO ise TagDetails.Plate (Personel İsmi) öncelikli olsun
                        // The User specifically requested to read "Personel Name" from TagDetails.Plate for M-ODEM logic
                        if (fleetCode != null && (fleetCode.Equals("M-ODEM", StringComparison.OrdinalIgnoreCase) || isParoSale))
                        {
                            plakaVal = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value ?? "";
                        }
                        else
                        {
                            // Standard behavior: ECRPlate > Sale.Plate > Tag.Plate
                            plakaVal = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRPlate")?.Value
                                       ?? sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value
                                       ?? tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value ?? "";
                        }

                        dto.FiloSatislar.Add(new CreateFiloSatisDto
                        {
                           Tarih = satisTarihi,
                           FiloKodu = fleetCode ?? "",
                           Plaka = plakaVal,

                           YakitTuru = fuelTypeStr,
                           YakitId = yakitId,
                           Litre = amountRaw / 100m,
                           Tutar = totalRaw / 100m,
                           PompaNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "PumpNr")?.Value ?? "0"),
                           FisNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ReceiptNr")?.Value ?? "0"),

                            // Yeni Alanlar
                            FiloAdi = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "FleetName")?.Value ?? "",
                            TagNr = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "TagNr")?.Value ?? "",
                            MotorSaati = int.Parse(tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "EngineHour")?.Value ?? "0"),
                            Kilometre = int.Parse(tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Odometer")?.Value ?? "0"),
                            SatisTuru = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "TxnType")?.Value ?? "0"),
                            TabancaNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "NozzleNr")?.Value ?? "0"),
                            OdemeTuru = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "PaymentType")?.Value ?? "0"),
                            YazarKasaPlaka = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRPlate")?.Value ?? "",
                            YazarKasaFisNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRReceiptNr")?.Value ?? "0"),
                            PuanKullanimi = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Redemption")?.Value.Replace(",", ".") ?? "0", CultureInfo.InvariantCulture),
                            IndirimTutar = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "DiscountAmount")?.Value.Replace(",", ".") ?? "0", CultureInfo.InvariantCulture),
                            KazanilanPuan = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "EarnedPoints")?.Value ?? "0"),
                            KazanilanPara = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "EarnedMoney")?.Value.Replace(",", ".") ?? "0", CultureInfo.InvariantCulture),
                            SadakatKartNo = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "LoyaltyCardNo")?.Value,
                            SadakatKartTipi = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "LoyaltyCardType")?.Value ?? "0"),
                            TamBirimFiyat = decimal.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "FullUnitPrice")?.Value ?? "0") / 100m
                        });
                    }
                }

                // 2.a Tank Envanteri Parsing
                var tankInventory = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "TankInventory");
                if (tankInventory != null)
                {
                    foreach (var tank in tankInventory.Elements()) // loop <Tank>
                    {
                        // Basit XML okuma, hatalarda default değer
                        int.TryParse(tank.Elements().FirstOrDefault(x => x.Name.LocalName == "TankNr")?.Value, out int tankNo);
                        string tankAdi = tank.Elements().FirstOrDefault(x => x.Name.LocalName == "Product")?.Value ?? "";
                        decimal.TryParse(tank.Elements().FirstOrDefault(x => x.Name.LocalName == "Volume")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal volume);
                        
                        // Ekstra alanlar varsa (Tahmini)
                        decimal.TryParse(tank.Elements().FirstOrDefault(x => x.Name.LocalName == "OpeningVolume")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal openingVol);
                        decimal.TryParse(tank.Elements().FirstOrDefault(x => x.Name.LocalName == "DailySale")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal saleVol);
                        decimal.TryParse(tank.Elements().FirstOrDefault(x => x.Name.LocalName == "Delivery")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal deliveryVol);

                        dto.TankEnvanterleri.Add(new CreateVardiyaTankEnvanteriDto
                        {
                            TankNo = tankNo,
                            TankAdi = tankAdi,
                             YakitTipi = tankAdi, // Genelde aynıdır
                            BitisStok = volume,
                            BaslangicStok = openingVol, // XML'de varsa
                            SatilanMiktar = saleVol,     // XML'de varsa
                            SevkiyatMiktar = deliveryVol // XML'de varsa
                        });
                    }
                }
                
                _logger.LogInformation($"DTO Oluşturuldu: {dto.OtomasyonSatislar.Count} Otomasyon, {dto.FiloSatislar.Count} Filo, {dto.TankEnvanterleri.Count} Tank satışı eklendi.");
                
                // 2.b Pompa Endekslerini Hesapla
                var nozzleSales = new Dictionary<string, decimal>();
                foreach (var s in dto.OtomasyonSatislar)
                {
                    string key = $"{s.PompaNo}-{s.TabancaNo}";
                    if (!nozzleSales.ContainsKey(key)) nozzleSales[key] = 0;
                    nozzleSales[key] += s.Litre;
                }
                foreach (var s in dto.FiloSatislar)
                {
                    string key = $"{s.PompaNo}-{s.TabancaNo}";
                    if (!nozzleSales.ContainsKey(key)) nozzleSales[key] = 0;
                    nozzleSales[key] += s.Litre;
                }

                var pumpElements = xdoc.Descendants().Where(x => x.Name.LocalName == "Pump").ToList();
                foreach (var pumpElement in pumpElements)
                {
                    string pumpNameStr = pumpElement.Elements().FirstOrDefault(x => x.Name.LocalName == "PumpName")?.Value ?? "0";
                    int.TryParse(pumpNameStr, out int pumpNr);

                    var nozzles = pumpElement.Descendants().Where(x => x.Name.LocalName == "Nozzle").ToList();
                    foreach (var nozzle in nozzles)
                    {
                        int.TryParse(nozzle.Elements().FirstOrDefault(x => x.Name.LocalName == "NozzleNr")?.Value, out int nozzleNr);
                        string fuelTypeRaw = nozzle.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value ?? "";
                        decimal.TryParse(nozzle.Elements().FirstOrDefault(x => x.Name.LocalName == "Totalizer")?.Value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal bitisEndeks);

                        if (bitisEndeks > 1000000) bitisEndeks /= 100;

                        // Fuel Type Mapping (Reuse logic from above)
                        string fuelTypeStr = "DIGER";
                        if (!string.IsNullOrEmpty(fuelTypeRaw) && yakitMap.ContainsKey(fuelTypeRaw))
                        {
                            fuelTypeStr = yakitMap[fuelTypeRaw].Ad;
                        }


                        string key = $"{pumpNr}-{nozzleNr}";
                        decimal satilanLitre = nozzleSales.ContainsKey(key) ? nozzleSales[key] : 0;
                        decimal baslangicEndeks = bitisEndeks - satilanLitre;

                        dto.PompaEndeksleri.Add(new CreateVardiyaPompaEndeksDto
                        {
                            PompaNo = pumpNr,
                            TabancaNo = nozzleNr,
                            YakitTuru = fuelTypeStr,
                            BaslangicEndeks = baslangicEndeks,
                            BitisEndeks = bitisEndeks
                        });
                    }
                }

                // Genel Toplamı Hesapla (Otomasyon + Filo)
                dto.GenelToplam = dto.OtomasyonSatislar.Sum(s => s.ToplamTutar) + dto.FiloSatislar.Sum(f => f.Tutar);
                _logger.LogInformation($"Vardiya Genel Toplam Hesaplandı: {dto.GenelToplam:N2} TL (Oto: {dto.OtomasyonSatislar.Sum(s => s.ToplamTutar):N2} + Filo: {dto.FiloSatislar.Sum(f => f.Tutar):N2})");

                // Vardiyayı Kaydet
                var vardiya = await CreateVardiyaAsync(dto, userId, userRole, userName);

                // Logu Güncelle
                xmlLog.VardiyaId = vardiya.Id;
                await _context.SaveChangesAsync();

                // Tank Envanter Kayıtlarını Oluştur (Raw XElement kullan)
                var tankElements = xdoc.Descendants().Where(x => x.Name.LocalName == "TankDetails").ToList();
                var tankEnvanterleri = new List<VardiyaTankEnvanteri>();
                
                foreach (var tankElement in tankElements)
                {
                    var tankNo = int.Parse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "TankNo")?.Value ?? "0");
                    var tankAdi = tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "TankName")?.Value ?? "";
                    var previousVol = decimal.Parse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "PreviousVolume")?.Value ?? "0");
                    var currentVol = decimal.Parse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "CurrentVolume")?.Value ?? "0");
                    var delta = decimal.Parse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "Delta")?.Value ?? "0");
                    var delivery = decimal.Parse(tankElement.Elements().FirstOrDefault(x => x.Name.LocalName == "DeliveryVolume")?.Value ?? "0");
                    
                    // Delta = PreviousVolume - CurrentVolume (XML'den gelen)
                    // Pozitif Delta = Satış var (stok azaldı)
                    // Negatif Delta = Stok arttı (sevkiyat veya ölçüm hatası)
                    
                    // Beklenen Tüketim = Başlangıç + Sevkiyat - Bitiş
                    var beklenenTuketim = previousVol + delivery - currentVol;
                    
                    // Satılan Miktar = Delta (işaretli)
                    // Pozitif ise satış, negatif ise stok artışı
                    var satilanMiktar = delta;
                    
                    // Fark = Beklenen - Gerçek
                    // Pozitif fark = Kayıp/kaçak
                    // Negatif fark = Fazla stok (ölçüm hatası veya kayıt dışı sevkiyat)
                    var fark = beklenenTuketim - satilanMiktar;
                    
                    // Debug logging
                    _logger.LogInformation($"Tank {tankNo} ({tankAdi}): Başlangıç={previousVol}, Bitiş={currentVol}, Delta={delta}, Sevkiyat={delivery}, BeklenenTüketim={beklenenTuketim}, SatilanMiktar={satilanMiktar}, FARK={fark}");
                    
                    tankEnvanterleri.Add(new VardiyaTankEnvanteri
                    {
                        VardiyaId = vardiya.Id,
                        TankNo = tankNo,

                        TankAdi = tankAdi,
                        YakitTipi = (await _yakitService.IdentifyYakitAsync(tankAdi))?.Ad ?? "DIGER", // Tanklarda isimden gidiyoruz
                        BaslangicStok = previousVol,
                        BitisStok = currentVol,
                        SatilanMiktar = satilanMiktar,  // İşaretli değer
                        SevkiyatMiktar = delivery,
                        BeklenenTuketim = beklenenTuketim,
                        FarkMiktar = fark,
                        KayitTarihi = DateTime.UtcNow
                    });
                }

                if (tankEnvanterleri.Any())
                {
                    await _context.VardiyaTankEnvanterleri.AddRangeAsync(tankEnvanterleri);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Tank Envanteri Kaydedildi: {tankEnvanterleri.Count} tank kaydı oluşturuldu.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML/ZIP işlenirken hata: {FileName}", fileName);
                throw new InvalidOperationException($"Dosya işlenemedi: {ex.Message}", ex);
            }
        }



        private async Task<string> NormalizeYakitTipiAsync(string tankAdi)
        {
            if (string.IsNullOrEmpty(tankAdi)) return "DIGER";
            var yakit = await _yakitService.IdentifyYakitAsync(tankAdi);
            return yakit?.Ad ?? "DIGER";
        }

        private string NormalizeYakitTipi(string tankAdi)
        {
            if (string.IsNullOrEmpty(tankAdi)) return "DIGER";
            var yakit = _yakitService.IdentifyYakitAsync(tankAdi).GetAwaiter().GetResult();
            return yakit?.Ad ?? "DIGER";
        }

        public async Task<MutabakatViewModel> CalculateVardiyaFinancials(int vardiyaId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 1. Vardiya temel bilgileri
            var vardiya = await _context.Vardiyalar
                .AsNoTracking()
                .Where(v => v.Id == vardiyaId)
                .Select(v => new VardiyaSummaryDto
                {
                    Id = v.Id,
                    IstasyonId = v.IstasyonId,
                    IstasyonAdi = v.Istasyon!.Ad,
                    BaslangicTarihi = v.BaslangicTarihi,
                    BitisTarihi = v.BitisTarihi,
                    Durum = (int)v.Durum,
                    PompaToplam = v.PompaToplam,
                    MarketToplam = v.MarketToplam, // Will be overridden if Market Shift found
                    GenelToplam = v.GenelToplam,
                    OlusturmaTarihi = v.OlusturmaTarihi,
                    DosyaAdi = v.DosyaAdi,
                    RedNedeni = v.RedNedeni,
                    OlusturanKullaniciAdi = _context.VardiyaLoglari
                        .Where(l => l.VardiyaId == v.Id && l.Islem == "OLUSTURULDU")
                        .OrderByDescending(l => l.IslemTarihi)
                        .Join(_context.Users, l => l.KullaniciId, u => u.Id, (l, u) => u.AdSoyad)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (vardiya == null)
            {
                throw new KeyNotFoundException("Vardiya bulunamadı.");
            }

            // [NEW] Inject Market Data
            var marketShift = await _context.MarketVardiyalar
                .AsNoTracking()
                .Where(m => m.IstasyonId == vardiya.IstasyonId && m.Tarih.Date == vardiya.BaslangicTarihi.Date)
                .FirstOrDefaultAsync();
            
            if (marketShift != null)
            {
                vardiya.MarketToplam = marketShift.ToplamSatisTutari;
            }

            // 2. Personel bazında GRUPLANMIŞ otomasyon satışları
            var personelOzetler = await _context.OtomasyonSatislar
                .AsNoTracking()
                .Where(s => s.VardiyaId == vardiyaId)
                .GroupBy(s => new { s.PersonelAdi, s.PersonelId })
                .Select(g => new PersonelMutabakatOzetDto
                {
                    PersonelAdi = g.Key.PersonelAdi,
                    PersonelId = g.Key.PersonelId,
                    ToplamLitre = g.Sum(s => s.Litre),
                    ToplamTutar = g.Sum(s => s.ToplamTutar),
                    IslemSayisi = g.Count()
                })
                .ToListAsync();

            // FIX: Prioritize Personel.AdSoyad over Automation Name
            _logger.LogInformation($"[VardiyaService] Personel eşleştirme başladı. {personelOzetler.Count} satır var.");
            
            var existingIds = personelOzetler
                .Where(x => x.PersonelId.HasValue && x.PersonelId.Value > 0)
                .Select(x => x.PersonelId!.Value)
                .ToList();

            var names = personelOzetler
                .Where(x => !string.IsNullOrEmpty(x.PersonelAdi))
                .Select(x => x.PersonelAdi.Trim())
                .Distinct()
                .ToList();

            // Fetch candidates
            var personels = await _context.Personeller
                .AsNoTracking()
                .Where(p => 
                    existingIds.Contains(p.Id) || 
                    names.Contains(p.OtomasyonAdi)
                )
                .ToListAsync();
            
            _logger.LogInformation($"[VardiyaService] DB Adayları bulundu: {personels.Count} adet.");

            // Match and Update
            foreach (var item in personelOzetler)
            {
                Personel? match = null;

                // Priority 1: By ID
                if (item.PersonelId.HasValue && item.PersonelId.Value > 0)
                {
                    match = personels.FirstOrDefault(p => p.Id == item.PersonelId.Value);
                }

                // Priority 2: By Name (OtomasyonAdi = PersonelAdi)
                if (match == null && !string.IsNullOrEmpty(item.PersonelAdi))
                {
                    match = personels.FirstOrDefault(p => string.Equals(p.OtomasyonAdi, item.PersonelAdi.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (match != null && !string.IsNullOrWhiteSpace(match.AdSoyad))
                {
                    item.GercekPersonelAdi = match.AdSoyad;
                    item.PersonelKeyId = match.KeyId; // Corrected from match.Referans
                    _logger.LogInformation($"[VardiyaService] Eşleşti: {item.PersonelAdi} -> {item.GercekPersonelAdi} ({item.PersonelKeyId})");
                    
                    // Fix missing ID
                    if (!item.PersonelId.HasValue || item.PersonelId.Value == 0)
                    {
                        item.PersonelId = match.Id;
                    }
                }
                else
                {
                     _logger.LogWarning($"[VardiyaService] Eşleşemedi: {item.PersonelAdi} (ID: {item.PersonelId})");
                }
            }

            // 3. Filo detayları (gruplu) - M-ODEM dahil
            var filoDetaylari = await _context.FiloSatislar
                .AsNoTracking()
                .Where(f => f.VardiyaId == vardiyaId && f.FiloAdi != "İSTASYON")
                .GroupBy(f => f.FiloKodu == "M-ODEM" ? "M-ODEM" : ((f.FiloAdi == null || f.FiloAdi == "") ? "OTOBIL" : f.FiloAdi))
                .Select(g => new FiloMutabakatDetayDto
                {
                    FiloAdi = g.Key,
                    Tutar = g.Sum(f => f.Tutar),
                    Litre = g.Sum(f => f.Litre),
                    IslemSayisi = g.Count()
                })
                .ToListAsync();

            // 4. Filo satışları özeti (Hesaplanan Filo Detayları üzerinden)
            // Başlangıç özeti (M-ODEM dahil toplam)
            var filoOzet = new FiloMutabakatOzetDto
            {
                ToplamTutar = filoDetaylari.Sum(f => f.Tutar),
                ToplamLitre = filoDetaylari.Sum(f => f.Litre),
                IslemSayisi = filoDetaylari.Sum(f => f.IslemSayisi)
            };

            // 5. Pusulalar
            var pusulalar = await _context.Pusulalar
                .AsNoTracking()
                .Include(p => p.DigerOdemeler)
                .Include(p => p.Veresiyeler).ThenInclude(v => v.CariKart)
                .Include(p => p.KrediKartiDetaylari)
                .Where(p => p.VardiyaId == vardiyaId)
                .Select(p => new PusulaMutabakatDto
                {
                    Id = p.Id,
                    PersonelAdi = p.PersonelAdi,
                    PersonelId = p.PersonelId,
                    Nakit = p.Nakit,
                    KrediKarti = p.KrediKarti,
                    KrediKartiDetay = p.KrediKartiDetay,
                    KrediKartiDetayList = p.KrediKartiDetaylari.Select(k => new PusulaKrediKartiDetayDto 
                    {
                        BankaAdi = k.BankaAdi,
                        Tutar = k.Tutar
                    }).ToList(),
                    DigerOdemeler = p.DigerOdemeler.Select(d => new PusulaDigerOdemeDto
                    {
                        TurKodu = d.TurKodu,
                        TurAdi = d.TurAdi,
                        Tutar = d.Tutar,
                        Silinemez = d.Silinemez
                    }).ToList(),
                    Veresiyeler = p.Veresiyeler.Select(v => new PusulaVeresiyeDto
                    {
                        CariKartId = v.CariKartId,
                        CariAd = v.CariKart.Ad,
                        Plaka = v.Plaka,
                        Litre = v.Litre,
                        Tutar = v.Tutar,
                        Aciklama = v.Aciklama
                    }).ToList(),
                    Aciklama = p.Aciklama,
                    Toplam = 0 // Will be calculated after loading
                })
                .ToListAsync();

            // FIX: Prioritize Personel.AdSoyad over Automation Name for Pusulas too
            _logger.LogInformation($"[VardiyaService] Pusula eşleştirme başladı. {pusulalar.Count} pusula var.");

            var pusulaPersonelIds = pusulalar.Where(x => x.PersonelId.HasValue && x.PersonelId.Value > 0).Select(x => x.PersonelId!.Value).Distinct().ToList();
            var pusulaNames = pusulalar.Where(x => !string.IsNullOrEmpty(x.PersonelAdi)).Select(x => x.PersonelAdi.Trim()).Distinct().ToList();

            var pusulaPersonels = await _context.Personeller
                .AsNoTracking()
                .Where(p => 
                    pusulaPersonelIds.Contains(p.Id) || 
                    pusulaNames.Contains(p.OtomasyonAdi)
                )
                .ToListAsync();

            foreach (var item in pusulalar)
            {
                Personel? match = null;

                // Priority 1: By ID
                if (item.PersonelId.HasValue && item.PersonelId.Value > 0)
                {
                    match = pusulaPersonels.FirstOrDefault(p => p.Id == item.PersonelId.Value);
                }

                // Priority 2: By Name
                if (match == null && !string.IsNullOrEmpty(item.PersonelAdi))
                {
                   match = pusulaPersonels.FirstOrDefault(p => string.Equals(p.OtomasyonAdi, item.PersonelAdi.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (match != null && !string.IsNullOrWhiteSpace(match.AdSoyad))
                {
                    item.GercekPersonelAdi = match.AdSoyad;
                    // Fix missing ID
                    if (!item.PersonelId.HasValue || item.PersonelId.Value == 0)
                    {
                        item.PersonelId = match.Id;
                    }
                }
            }

            // Calculate Toplam after loading to ensure accuracy with navigation properties
            foreach (var p in pusulalar)
            {
                p.Toplam = p.Nakit + p.KrediKarti + 
                           p.DigerOdemeler.Sum(d => d.Tutar) +
                           p.Veresiyeler.Sum(v => v.Tutar);
            }

            // 6. Giderler
            var giderler = await _context.PompaGiderler
                .AsNoTracking()
                .Where(g => g.VardiyaId == vardiyaId)
                .Select(g => new GiderMutabakatDto
                {
                    Id = g.Id,
                    GiderTuru = g.GiderTuru,
                    Tutar = g.Tutar,
                    Aciklama = g.Aciklama
                })
                .ToListAsync();

            // ====================================================================================
            // 7. M-ODEM RECONCILIATION LOGIC
            // ====================================================================================
            
            // A. M-ODEM Satışlarını Bul (FiloSatislar - Yeni Yöntem)
            var mOdemList = await _context.FiloSatislar
                .AsNoTracking()
                .Where(f => f.VardiyaId == vardiyaId && f.FiloKodu == "M-ODEM")
                .ToListAsync();

            // FALLBACK: Eski vardiyalar için OtomasyonSatislar'da duran M-ODEM'leri de al
            var oldMOdemList = await _context.OtomasyonSatislar
                .AsNoTracking()
                .Where(s => s.VardiyaId == vardiyaId && (s.FiloAdi == "M-ODEM" || s.TagNr == "M-ODEM")) // TagNr might be abuse too? Just FiloAdi usually.
                .ToListAsync();

            if (oldMOdemList.Any())
            {
                 // Convert OtomasyonSatis to FiloSatis structure for unified processing
                 mOdemList.AddRange(oldMOdemList.Select(o => new FiloSatis
                 {
                     PompaNo = o.PompaNo,
                     Plaka = o.Plaka ?? "",
                     Tutar = o.ToplamTutar,
                     Litre = o.Litre,
                     FiloKodu = "M-ODEM"
                     // TagNr/Name extraction logic will use Plaka as Name because old parser put Name in Plaka for M-ODEM? 
                     // Wait, OLD parser: PersonelAdi = Plate, Plaka = ECRPlate ?? Plate.
                     // IMPORTANT: In "Calculate", we check "mo.Plaka" for Name matching.
                     // OtomasyonSatis has "PersonelAdi".
                     // So we should map OtomasyonSatis.PersonelAdi -> FiloSatis.Plaka (temporarily for calculation)
                 }));

                 // WAIT: FiloSatis doesn't have PersonelAdi. We are mapping to FiloSatis class.
                 // We need to verify what property we use for matching.
                 // Code uses: mo.Plaka for name match.
                 // In OtomasyonSatis, correct name is in PersonelAdi. Plaka might be "34ABC..." if ECRPlate existed.
                 // So we must map OtomasyonSatis.PersonelAdi to FiloSatis.Plaka for this hack to work.
                 
                 foreach(var old in oldMOdemList)
                 {
                     mOdemList.Add(new FiloSatis 
                     {
                         PompaNo = old.PompaNo,
                         Plaka = old.PersonelAdi, // Hijack Plaka to store Name for matching logic
                         Tutar = old.ToplamTutar,
                         Litre = old.Litre,
                         FiloKodu = "M-ODEM"
                     });
                 }
            }

            if (mOdemList.Any())
            {
                // B. Pompa -> Personel Eşleşmesini Bul
                var pumpPersonMapQuery = await _context.OtomasyonSatislar
                    .AsNoTracking()
                    .Where(s => s.VardiyaId == vardiyaId)
                    .Select(s => new { s.PompaNo, s.PersonelId })
                    .ToListAsync();
                
                var pumpPersonMap = pumpPersonMapQuery
                    .GroupBy(x => x.PompaNo)
                    .ToDictionary(g => g.Key, g => g.First().PersonelId);

                // C. Her M-ODEM işlemini personele dağıt
                foreach (var mo in mOdemList)
                {
                    // 1. Filo Toplamından Düş (HER ZAMAN - Kural: M-ODEM filo satışı olsa da pusulada tahsil edilir)
                    filoOzet.ToplamTutar -= mo.Tutar;
                    filoOzet.ToplamLitre -= mo.Litre;
                    filoOzet.IslemSayisi--;

                    // 2. Personeli Bul (Öncelik: Key -> İsim -> Pompa)
                    PusulaMutabakatDto? pusula = null;

                    // I. İsim ile arama (Plaka forced to Name in Parser)
                    if (!string.IsNullOrEmpty(mo.Plaka))
                    {
                        pusula = pusulalar.FirstOrDefault(p => p.PersonelAdi != null && p.PersonelAdi.Equals(mo.Plaka.Trim(), StringComparison.OrdinalIgnoreCase));
                    }

                    // II. Pompa ile arama (Fallback)
                    if (pusula == null && pumpPersonMap.ContainsKey(mo.PompaNo))
                    {
                        var pId = pumpPersonMap[mo.PompaNo];
                        pusula = pusulalar.FirstOrDefault(p => p.PersonelId == pId);
                    }

                    // 3. Eşleşen Pusulaya Ekle (Hibrid Mantık)
                    if (pusula != null)
                    {
                        // DB'de kayıtlı mı kontrol et (Persistence Check)
                        bool alreadyPersisted = pusula.DigerOdemeler.Any(d => d.TurKodu == "MOBIL_ODEME");

                        // Eğer DB'de yoksa, dinamik olarak ekle (Legacy Support)
                        if (!alreadyPersisted)
                        {
                             var existingDynamic = pusula.DigerOdemeler.FirstOrDefault(d => d.TurKodu == "MOBIL_ODEME");
                             if (existingDynamic != null)
                             {
                                 existingDynamic.Tutar += mo.Tutar;
                             }
                             else
                             {
                                 pusula.DigerOdemeler.Add(new PusulaDigerOdemeDto
                                 {
                                     TurKodu = "MOBIL_ODEME",
                                     TurAdi = "Mobil Ödeme",
                                     Tutar = mo.Tutar,
                                     Silinemez = true
                                 });
                             }
                             pusula.Toplam += mo.Tutar;
                        }
                    }
                }
            }

            // ====================================================================================
            // 8. PARO PUAN RECONCILIATION LOGIC (Hibrid)
            // ====================================================================================
            // Otomasyon verisindeki PuanKullanimi alanını toplayıp "Pompa Paro Puan" olarak ekle.
            
            var paroList = await _context.OtomasyonSatislar
                .AsNoTracking()
                .Where(s => s.VardiyaId == vardiyaId && s.PuanKullanimi > 0)
                .GroupBy(s => s.PersonelId)
                .Select(g => new 
                { 
                    PersonelId = g.Key, 
                    ToplamPuan = g.Sum(s => s.PuanKullanimi) 
                })
                .ToListAsync();

            foreach (var paro in paroList)
            {
                if (paro.PersonelId == null) continue;

                var pusula = pusulalar.FirstOrDefault(p => p.PersonelId == paro.PersonelId);
                if (pusula != null)
                {
                    // DB'de kayıtlı mı kontrol et
                    bool alreadyPersisted = pusula.DigerOdemeler.Any(d => d.TurKodu == "POMPA_PARO_PUAN");

                    // Eğer DB'de yoksa, dinamik olarak ekle
                    if (!alreadyPersisted)
                    {
                         pusula.DigerOdemeler.Add(new PusulaDigerOdemeDto
                         {
                             TurKodu = "POMPA_PARO_PUAN",
                             TurAdi = "Pompa Paro Puan",
                             Tutar = paro.ToplamPuan,
                             Silinemez = true
                         });
                         pusula.Toplam += paro.ToplamPuan;
                    }
                }
            }

            // 9. GENEL ÖZET HESAPLAMA
            var genelOzet = new GenelMutabakatOzetDto
            {
                ToplamOtomasyon = vardiya.GenelToplam,
                ToplamGider = giderler.Sum(g => g.Tutar),
                MarketToplam = vardiya.MarketToplam, // [NEW] Included Market Sales
                ToplamNakit = pusulalar.Sum(p => p.Nakit),
                ToplamKrediKarti = pusulalar.Sum(p => p.KrediKarti),
                ToplamPusula = pusulalar.Sum(p => p.Toplam)
            };

            var toplamTahsilat = genelOzet.ToplamPusula + filoOzet.ToplamTutar + genelOzet.ToplamGider;
            // Target = Fuel Sales + Market Sales
            genelOzet.Fark = toplamTahsilat - (genelOzet.ToplamOtomasyon + genelOzet.MarketToplam);

            stopwatch.Stop();

            return new MutabakatViewModel
            {
                Vardiya = vardiya,
                PersonelOzetler = personelOzetler,
                FiloOzet = filoOzet,
                FiloDetaylari = filoDetaylari,
                Pusulalar = pusulalar,
                Giderler = giderler,
                GenelOzet = genelOzet,
                _performanceMs = stopwatch.ElapsedMilliseconds
            };
        }
    }
}
