using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Services
{
    public class VardiyaFinancialService : IVardiyaFinancialService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VardiyaFinancialService> _logger;

        public VardiyaFinancialService(AppDbContext context, ILogger<VardiyaFinancialService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MutabakatViewModel> CalculateVardiyaFinancials(int vardiyaId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation($"🚀 GetMutabakat (VardiyaFinancialService) başladı - ID: {vardiyaId}");

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
                    MarketToplam = v.MarketToplam,
                    GenelToplam = v.GenelToplam,
                    OlusturmaTarihi = v.OlusturmaTarihi,
                    DosyaAdi = v.DosyaAdi,
                    RedNedeni = v.RedNedeni
                })
                .FirstOrDefaultAsync();

            if (vardiya == null)
            {
                throw new KeyNotFoundException("Vardiya bulunamadı.");
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
            // 1. Get IDs and Names
            var existingIds = personelOzetler
                .Where(x => x.PersonelId.HasValue && x.PersonelId.Value > 0)
                .Select(x => x.PersonelId!.Value)
                .ToList();

            var names = personelOzetler
                .Where(x => !string.IsNullOrEmpty(x.PersonelAdi))
                .Select(x => x.PersonelAdi.Trim())
                .Distinct()
                .ToList();


            _logger.LogInformation($"DEBUG: Vardiya {vardiyaId} için {personelOzetler.Count} özet bulundu.");
            foreach(var n in names) _logger.LogInformation($"DEBUG: Aranacak İsim (Otomasyon): '{n}'");

            // 2. Fetch candidates from DB
            var personels = await _context.Personeller
                .AsNoTracking()
                .Where(p => 
                    existingIds.Contains(p.Id) || 
                    names.Contains(p.OtomasyonAdi)
                )
                .ToListAsync();

            _logger.LogInformation($"DEBUG: DB'den {personels.Count} personel adayı bulundu.");
            foreach(var p in personels) _logger.LogInformation($"DEBUG: DB Aday -> ID: {p.Id}, OtoAd: '{p.OtomasyonAdi}', GercekAd: '{p.AdSoyad}'");

            // 3. Match and Update
            foreach (var item in personelOzetler)
            {
                _logger.LogInformation($"DEBUG: Eşleştiriliyor -> PersonelAdi: '{item.PersonelAdi}', ID: {item.PersonelId}");
                Personel? match = null;

                // Priority 1: By ID
                if (item.PersonelId.HasValue && item.PersonelId.Value > 0)
                {
                    match = personels.FirstOrDefault(p => p.Id == item.PersonelId.Value);
                    if(match != null) _logger.LogInformation("DEBUG: ID ile eşleşti.");
                }

                // Priority 2: By Name (OtomasyonAdi = PersonelAdi)
                if (match == null && !string.IsNullOrEmpty(item.PersonelAdi))
                {
                    match = personels.FirstOrDefault(p => string.Equals(p.OtomasyonAdi, item.PersonelAdi.Trim(), StringComparison.OrdinalIgnoreCase));
                    if(match != null) _logger.LogInformation($"DEBUG: İsim ile eşleşti. (OtoAd: {match.OtomasyonAdi})");
                    else _logger.LogInformation("DEBUG: İsim ile eşleşemedi.");
                }

                if (match != null && !string.IsNullOrWhiteSpace(match.AdSoyad))
                {
                    item.GercekPersonelAdi = match.AdSoyad;
                    _logger.LogInformation($"DEBUG: Atanan Gerçek İsim: {item.GercekPersonelAdi}");
                    // Fix missing ID
                    if (!item.PersonelId.HasValue || item.PersonelId.Value == 0)
                    {
                        item.PersonelId = match.Id;
                    }
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

            // 4. Filo satışları özeti
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
            // FIX: Prioritize Personel.AdSoyad over Automation Name for Pusulas too
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
            // 7. M-ODEM & PARO RECONCILIATION LOGIC (Unified)
            // ====================================================================================

            // A. Otomasyon Satışlarından (Yeni Yapı + Paro)
            var otomasyonOzelOdemeler = await _context.OtomasyonSatislar
                .AsNoTracking()
                .Where(s => s.VardiyaId == vardiyaId && (s.MobilOdemeTutar > 0 || s.PuanKullanimi > 0))
                .GroupBy(s => s.PersonelAdi)
                .Select(g => new 
                { 
                    PersonelAdi = g.Key, 
                    MobilOdeme = g.Sum(s => s.MobilOdemeTutar),
                    ParoPuan = g.Sum(s => s.PuanKullanimi)
                })
                .ToListAsync();

            // Aggregate into a Dictionary for fast lookup
            // Key: PersonelAdi, Value: (MobilTutar, ParoTutar)
            var paymentSummary = otomasyonOzelOdemeler
                .GroupBy(x => x.PersonelAdi?.Trim() ?? "")
                .ToDictionary(
                    g => g.Key, 
                    g => new { 
                        Mobil = g.Sum(x => x.MobilOdeme), 
                        Paro = g.Sum(x => x.ParoPuan) 
                    }, 
                    StringComparer.OrdinalIgnoreCase
                );

            // B. M-ODEM (Legacy/Filo Fallback) - REMOVED (Clean Install)
            
            // C. Pusulalara Dağıt
            foreach (var p in pusulalar)
            {
                if (string.IsNullOrEmpty(p.PersonelAdi)) continue;

                if (paymentSummary.TryGetValue(p.PersonelAdi.Trim(), out var amounts))
                {
                    // 1. Mobil Ödeme
                    if (amounts.Mobil > 0)
                    {
                        var existing = p.DigerOdemeler.FirstOrDefault(d => d.TurKodu == "MOBIL_ODEME");
                        if (existing == null)
                        {
                            p.DigerOdemeler.Add(new PusulaDigerOdemeDto
                            {
                                TurKodu = "MOBIL_ODEME",
                                TurAdi = "Mobil Ödeme",
                                Tutar = amounts.Mobil,
                                Silinemez = true
                            });
                            p.Toplam += amounts.Mobil;
                        }
                        else
                        {
                            // Always update with the calculated fresh value
                            existing.Tutar = amounts.Mobil;
                        }
                    }

                    // 2. Paro Puan
                    if (amounts.Paro > 0)
                    {
                         var existing = p.DigerOdemeler.FirstOrDefault(d => d.TurKodu == "POMPA_PARO_PUAN");
                         if (existing == null)
                         {
                             p.DigerOdemeler.Add(new PusulaDigerOdemeDto
                             {
                                 TurKodu = "POMPA_PARO_PUAN",
                                 TurAdi = "Pompa Paro Puan",
                                 Tutar = amounts.Paro,
                                 Silinemez = true
                             });
                             p.Toplam += amounts.Paro;
                         }
                         else
                         {
                             existing.Tutar = amounts.Paro;
                         }
                    }
                }
            }

            // FIXED: M-ODEM is now purely an Automation Sale with a special payment type.
            // It is NOT in FiloDetaylari, so we do NOT need to deduct anything from FiloOzet.
            // Legacy correction code removed as per user request (Clean DB).

            // 8. Genel Özet Hesaplama
            var genelOzet = new GenelMutabakatOzetDto
            {
                ToplamOtomasyon = vardiya.GenelToplam,
                ToplamGider = giderler.Sum(g => g.Tutar),
                MarketToplam = vardiya.MarketToplam,
                ToplamNakit = pusulalar.Sum(p => p.Nakit),
                ToplamKrediKarti = pusulalar.Sum(p => p.KrediKarti),
                ToplamPusula = pusulalar.Sum(p => p.Toplam)
            };

            // Toplam tahsilat = Pusula + Filo + Gider
            var toplamTahsilat = genelOzet.ToplamPusula + filoOzet.ToplamTutar + genelOzet.ToplamGider;
            genelOzet.Fark = toplamTahsilat - genelOzet.ToplamOtomasyon;

            stopwatch.Stop();
            _logger.LogInformation($"Vardiya ({vardiyaId}) finansal hesaplaması {stopwatch.ElapsedMilliseconds}ms sürdü. Fark: {genelOzet.Fark}");

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

        public async Task ProcessVardiyaApproval(int vardiyaId, int onaylayanId)
        {
            // 1. Veresiye Satışlarını Bul
            var veresiyeler = await _context.PusulaVeresiyeler
                .Include(pv => pv.CariKart)
                .Where(pv => pv.Pusula.VardiyaId == vardiyaId)
                .ToListAsync();

            if (!veresiyeler.Any()) return;

            // 2. Her veresiye için Cari Hareket oluştur
            foreach (var veresiye in veresiyeler)
            {
                // Mükerrer Kontrolü (Opsiyonel ama güvenli)
                // Aynı Pusula Veresiye ID'sine referans veren hareket var mı?
                // Şu an CariHareket'te PusulaVeresiyeId yok, o yüzden Description'dan veya başka yolla kontrol edebiliriz
                // Ama Vardiya Onayı transactional olduğu için güvende sayılırız.

                var hareket = new CariHareket
                {
                    CariKartId = veresiye.CariKartId,
                    Tarih = DateTime.UtcNow,
                    IslemTipi = "SATIS", // veya VERESIYE
                    Tutar = veresiye.Tutar,
                    Aciklama = $"Vardiya #{vardiyaId} - Plaka: {veresiye.Plaka} - {veresiye.Litre:F2} L - {veresiye.Tutar:F2} ₺" + (string.IsNullOrEmpty(veresiye.Aciklama) ? "" : $" - {veresiye.Aciklama}"),
                    OlusturanId = onaylayanId,
                    OlusturmaTarihi = DateTime.UtcNow
                };

                _context.CariHareketler.Add(hareket);

                // 3. Cari Bakiye Güncelle
                veresiye.CariKart.Bakiye += veresiye.Tutar;
                veresiye.CariKart.GuncellemeTarihi = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
