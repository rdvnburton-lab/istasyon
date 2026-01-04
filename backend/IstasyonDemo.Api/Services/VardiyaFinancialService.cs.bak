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

            // 1. Vardiya temel bilgileri
            var vardiya = await _context.Vardiyalar
                .AsNoTracking()
                .Where(v => v.Id == vardiyaId)
                .Select(v => new VardiyaSummaryDto
                {
                    Id = v.Id,
                    IstasyonId = v.IstasyonId,
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
                .Where(p => p.VardiyaId == vardiyaId)
                .Select(p => new PusulaMutabakatDto
                {
                    Id = p.Id,
                    PersonelAdi = p.PersonelAdi,
                    PersonelId = p.PersonelId,
                    Nakit = p.Nakit,
                    KrediKarti = p.KrediKarti,
                    KrediKartiDetay = p.KrediKartiDetay,
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

            // 7. M-ODEM Logic
            var mOdemList = await _context.FiloSatislar
                .AsNoTracking()
                .Where(f => f.VardiyaId == vardiyaId && f.FiloKodu == "M-ODEM")
                .ToListAsync();

            var oldMOdemList = await _context.OtomasyonSatislar
                .AsNoTracking()
                .Where(s => s.VardiyaId == vardiyaId && (s.FiloAdi == "M-ODEM" || s.TagNr == "M-ODEM"))
                .ToListAsync();

            if (oldMOdemList.Any())
            {
                 mOdemList.AddRange(oldMOdemList.Select(o => new FiloSatis
                 {
                     Plaka = o.PersonelAdi, // Mapped to Plaka
                     Tutar = o.ToplamTutar,
                     Litre = o.Litre,
                     FiloKodu = "M-ODEM-LEGACY"
                 }));
            }

            var mOdemGrouped = mOdemList
                 .GroupBy(m => m.Plaka) // Plaka holds Personel Name
                 .ToDictionary(g => g.Key, g => g.Sum(x => x.Tutar));

            // Hybrid Calculation & Logic...
            decimal toplamModemTutar = 0;

            foreach (var p in pusulalar)
            {
                // M-ODEM
                var existingModem = p.DigerOdemeler.FirstOrDefault(d => d.TurKodu == "MOBIL_ODEME");
                if (existingModem != null)
                {
                    toplamModemTutar += existingModem.Tutar;
                }
                else
                {
                    if (string.IsNullOrEmpty(p.PersonelAdi)) continue;

                    if (mOdemGrouped.TryGetValue(p.PersonelAdi.Trim(), out decimal mOdemTutar) && mOdemTutar > 0)
                    {
                        var dto = new PusulaDigerOdemeDto
                        {
                            TurKodu = "MOBIL_ODEME",
                            TurAdi = "Mobil Ödeme",
                            Tutar = mOdemTutar,
                            Silinemez = true 
                        };
                        p.DigerOdemeler.Add(dto);
                        p.Toplam += mOdemTutar; 
                        toplamModemTutar += mOdemTutar;
                    }
                }

                // Paro Puan (Dynamic Calc Check)
                var existingParo = p.DigerOdemeler.FirstOrDefault(d => d.TurKodu == "POMPA_PARO_PUAN");
                if (existingParo == null)
                {
                     // Calc dynamic
                    var puanTutar = await _context.OtomasyonSatislar
                        .AsNoTracking()
                        .Where(s => s.VardiyaId == vardiyaId && s.PersonelAdi == p.PersonelAdi)
                        .SumAsync(s => s.PuanKullanimi); 

                    if (puanTutar > 0)
                    {
                         var dto = new PusulaDigerOdemeDto
                         {
                             TurKodu = "POMPA_PARO_PUAN",
                             TurAdi = "Paro Puan",
                             Tutar = puanTutar,
                             Silinemez = true
                         };
                         p.DigerOdemeler.Add(dto);
                         p.Toplam += puanTutar;
                     }
                }
            }

            // Adjust Filo Ozet (Deduct M-ODEM to prevent double counting in Grand Total)
            filoOzet.ToplamTutar -= toplamModemTutar;

            // 8. Genel Özet Hesaplama
            var genelOzet = new GenelMutabakatOzetDto
            {
                ToplamOtomasyon = vardiya.GenelToplam,
                ToplamGider = giderler.Sum(g => g.Tutar),
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
