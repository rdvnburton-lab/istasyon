using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;
using System.Xml.Linq;
using System.Globalization;
using System.Text.Json;

namespace IstasyonDemo.Api.Services
{
    public class VardiyaService : IVardiyaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VardiyaService> _logger;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public VardiyaService(AppDbContext context, ILogger<VardiyaService> logger, IMapper mapper, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _notificationService = notificationService;
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


                // 5. M-ODEM Otomatik Tahsilat İşleme
                if (dto.MobilOdemeler != null && dto.MobilOdemeler.Any())
                {
                    foreach (var mobilOdeme in dto.MobilOdemeler)
                    {
                        // İlgili personeli bul
                        var personel = await _context.Personeller
                            .FirstOrDefaultAsync(p => p.IstasyonId == vardiya.IstasyonId && 
                                                    (p.KeyId == mobilOdeme.PersonelKeyId || p.AdSoyad == mobilOdeme.PersonelIsmi));

                        if (personel != null)
                        {
                            // Bu personelin bu vardiyadaki pusulasını bul (Artık KESİN var)
                            var pusula = await _context.Pusulalar
                                .Include(p => p.DigerOdemeler)
                                .FirstOrDefaultAsync(p => p.VardiyaId == vardiya.Id && p.PersonelId == personel.Id);

                            if (pusula != null)
                            {
                                if (pusula.DigerOdemeler == null) pusula.DigerOdemeler = new List<PusulaDigerOdeme>();

                                if (pusula.DigerOdemeler == null) pusula.DigerOdemeler = new List<PusulaDigerOdeme>();

                                    string turAdi = "Turpak Mobil Ödeme"; // Eski adı: Mobil Ödeme
                                    if (mobilOdeme.TurKodu == "PARO_PUAN_POMPA") turAdi = "Pompa Paro Puan";
                                    else if (mobilOdeme.TurKodu == "MOBIL_ODEME") turAdi = "Turpak Mobil Ödeme";

                                    var existingItem = pusula.DigerOdemeler.FirstOrDefault(d => d.TurKodu == mobilOdeme.TurKodu);
                                    if (existingItem != null)
                                    {
                                        existingItem.Tutar += mobilOdeme.Tutar;
                                        existingItem.Silinemez = true; // Mevcutsa da kilitli olduğundan emin ol
                                        existingItem.TurAdi = turAdi;  // Adı değiştiyse güncelle
                                    }
                                    else
                                    {
                                        pusula.DigerOdemeler.Add(new PusulaDigerOdeme
                                        {
                                            TurKodu = mobilOdeme.TurKodu,
                                            TurAdi = turAdi,
                                            Tutar = mobilOdeme.Tutar,
                                            Silinemez = true // Yeni eklenen kesinlikle silinemez
                                        });
                                    }
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                
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
                        XmlIcerik = dto.DosyaIcerik, // Text olarak sakla
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
                if (userRole != "vardiya_sorumlusu" && userRole != "vardiya sorumlusu")
                {
                    var vardiyaSorumlulari = await _context.Users
                        .Include(u => u.Role)
                        .Where(u => u.IstasyonId == vardiya.IstasyonId && 
                                   (u.Role.Ad == "Vardiya Sorumlusu" || u.Role.Ad == "vardiya_sorumlusu"))
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
                .ThenInclude(i => i.Firma)
                .Include(v => v.Istasyon)
                .ThenInclude(i => i.Firma)
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
            var otomasyonToplam = vardiya.GenelToplam; // System Sales
            
            var pusulaNakit = vardiya.Pusulalar.Sum(p => p.Nakit);
            var pusulaKredi = vardiya.Pusulalar.Sum(p => p.KrediKarti);
            var pusulaDiger = vardiya.Pusulalar.Sum(p => p.DigerOdemeler?.Sum(d => d.Tutar) ?? 0);
            
            // Optimized: Calculate aggregations via DB
            var filoToplam = await _context.FiloSatislar.Where(f => f.VardiyaId == vardiya.Id).SumAsync(f => f.Tutar);
            var giderToplam = await _context.PompaGiderler.Where(g => g.VardiyaId == vardiya.Id).SumAsync(g => g.Tutar);

            // Added GiderToplam to calculation
            var toplamTahsilat = pusulaNakit + pusulaKredi + pusulaDiger + filoToplam + giderToplam;
            
            // Fark = Tahsilat - Satış. 
            // Negatif ise AÇIK (Eksik), Pozitif ise FAZLA.
            var fark = toplamTahsilat - otomasyonToplam;

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

            vardiya.Durum = VardiyaDurum.ONAYLANDI;
            vardiya.OnaylayanId = dto.OnaylayanId;
            vardiya.OnaylayanAdi = dto.OnaylayanAdi;
            vardiya.OnayTarihi = DateTime.UtcNow;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
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

                // 4. Detayları JSON olarak hazırla
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

                _logger.LogInformation($"XML İstatistikleri: {tanks.Count} Tank, {pumps.Count} Pompa, {txnList.Count} Satış bulundu.");

                // 5. VardiyaXmlLog Kaydı
                var xmlLog = new VardiyaXmlLog
                {
                    IstasyonId = station.Id,
                    DosyaAdi = fileName,
                    ZipDosyasi = zipBytes,
                    TankDetailsJson = JsonSerializer.Serialize(tanks),
                    PumpDetailsJson = JsonSerializer.Serialize(pumps),
                    SaleDetailsJson = JsonSerializer.Serialize(txnList),
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

                foreach (var txn in rawTxns)
                {
                    var sale = txn.Elements().FirstOrDefault(x => x.Name.LocalName == "SaleDetails");
                    if (sale == null) continue;
                    
                    var tag = txn.Elements().FirstOrDefault(x => x.Name.LocalName == "TagDetails");
                    var fleetCode = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "FleetCode")?.Value;

                    var fuelType = MapFuelType(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "FuelType")?.Value);
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

                        dto.MobilOdemeler.Add(new MobilOdemeDto 
                        {
                            PersonelIsmi = personelName,
                            PersonelKeyId = personelKey,
                            Tutar = redemptionRaw / 100m,
                            Aciklama = $"Pompa Paro Puan (Kart: {loyaltyCardNo})",
                            TurKodu = "PARO_PUAN_POMPA",
                            Silinemez = true
                        });
                    }

                    bool isAutomationSale = string.IsNullOrEmpty(fleetCodeCheck) || 
                                            fleetCodeCheck.Equals(dynamicCode, StringComparison.OrdinalIgnoreCase) ||
                                            isParoSale; // Paro satışları da otomasyon satışı sayılmalı

                    // M-ODEM (Mobil Ödeme) Check -> Treat as Automation Sale + Auto Payment
                    if (!isAutomationSale && fleetCodeCheck.Equals("M-ODEM", StringComparison.OrdinalIgnoreCase))
                    {
                        isAutomationSale = true;
                        
                        // Add to Auto-Payment List
                        var personelName = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value ?? "";
                        var personelKey = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "TagNr")?.Value ?? "";
                        
                        dto.MobilOdemeler.Add(new MobilOdemeDto 
                        {
                            PersonelIsmi = personelName,
                            PersonelKeyId = personelKey,
                            Tutar = totalRaw / 100m,
                            Aciklama = $"Turpak Mobil Ödeme (Fiş: {sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ReceiptNr")?.Value})",
                            TurKodu = "MOBIL_ODEME",
                            Silinemez = true
                        });
                    }

                    if (isAutomationSale)
                    {
                        dto.OtomasyonSatislar.Add(new CreateOtomasyonSatisDto
                        {
                            PompaNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "PumpNr")?.Value ?? "0"),
                            YakitTuru = fuelType,
                            Litre = amountRaw / 100m,
                            BirimFiyat = priceRaw / 100m,
                            ToplamTutar = totalRaw / 100m,
                            SatisTarihi = satisTarihi,
                            FisNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ReceiptNr")?.Value ?? "0"),
                            // Personel Ismi Mapping (TagDetails.Plate -> PersonelAdi)
                            PersonelAdi = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value ?? "", 
                            // Personel Key ID Mapping (TagDetails.TagNr)
                            PersonelKeyId = tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "TagNr")?.Value ?? "",
                            // Plaka Mapping Önceliği: ECRPlate > Sale.Plate >> NOT TagDetails.Plate (O Personel Ismi)
                            Plaka = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRPlate")?.Value
                                    ?? sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value 
                        });
                    }
                    else
                    {
                        // Diğerleri -> FiloSatislar
                        dto.FiloSatislar.Add(new CreateFiloSatisDto
                        {
                           Tarih = satisTarihi,
                           FiloKodu = fleetCode,
                           Plaka = sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ECRPlate")?.Value
                                   ?? sale.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value
                                   ?? tag?.Elements().FirstOrDefault(x => x.Name.LocalName == "Plate")?.Value ?? "",
                           YakitTuru = fuelType,
                           Litre = amountRaw / 100m,
                           Tutar = totalRaw / 100m,
                           PompaNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "PumpNr")?.Value ?? "0"),
                           FisNo = int.Parse(sale.Elements().FirstOrDefault(x => x.Name.LocalName == "ReceiptNr")?.Value ?? "0")
                        });
                    }
                }
                
                _logger.LogInformation($"DTO Oluşturuldu: {dto.OtomasyonSatislar.Count} Otomasyon, {dto.FiloSatislar.Count} Filo satışı eklendi.");
                
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
                        YakitTipi = NormalizeYakitTipi(tankAdi),
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

        private string MapFuelType(string? code)
        {
            return code switch
            {
                "4" => "MOTORIN", // Diesel
                "5" => "BENZIN",  // Unleaded 95
                "6" => "LPG",     // Autogas
                "1" => "MOTORIN", // Sometimes 1 is Diesel too
                "2" => "BENZIN",
                _ => "DIGER"
            };
        }

        private string NormalizeYakitTipi(string tankAdi)
        {
            if (string.IsNullOrEmpty(tankAdi)) return "DIGER";
            
            var normalized = tankAdi.ToUpperInvariant();
            if (normalized.Contains("MOTORIN") || normalized.Contains("DIESEL")) return "MOTORIN";
            if (normalized.Contains("BENZIN") || normalized.Contains("KURŞUNSUZ") || normalized.Contains("KURSUN")) return "BENZIN";
            if (normalized.Contains("LPG") || normalized.Contains("AUTOGAS")) return "LPG";
            
            return "DIGER";
        }
    }
}
