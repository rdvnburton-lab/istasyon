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
            var vardiya = await _context.Vardiyalar
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vardiyaId);

            if (vardiya == null) throw new KeyNotFoundException("Vardiya bulunamadı.");

            // 1. Otomasyon Satışları (Gruplu)
            var personelOzetler = await _context.OtomasyonSatislar
                .AsNoTracking()
                .Where(s => s.VardiyaId == vardiyaId)
                .GroupBy(s => new { s.PersonelKeyId, s.PersonelAdi })
                .Select(g => new PersonelMutabakatOzetDto
                {
                    PersonelKeyId = g.Key.PersonelKeyId,
                    PersonelAdi = g.Key.PersonelAdi,
                    ToplamLitre = g.Sum(s => s.Litre),
                    ToplamTutar = g.Sum(s => s.ToplamTutar),
                    IslemSayisi = g.Count()
                })
                .ToListAsync();

            // FIX: Prioritize Personel.AdSoyad over Automation Name
            var personels = await _context.Personeller
                .AsNoTracking()
                .Where(p => p.IstasyonId == vardiya.IstasyonId)
                .ToListAsync();

            foreach (var item in personelOzetler)
            {
                Personel? match = null;
                if (!string.IsNullOrEmpty(item.PersonelKeyId))
                {
                    match = personels.FirstOrDefault(p => p.KeyId == item.PersonelKeyId);
                }
                if (match == null && !string.IsNullOrEmpty(item.PersonelAdi))
                {
                    match = personels.FirstOrDefault(p => string.Equals(p.OtomasyonAdi, item.PersonelAdi.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (match != null && !string.IsNullOrWhiteSpace(match.AdSoyad))
                {
                    item.GercekPersonelAdi = match.AdSoyad;
                    item.PersonelId = match.Id;
                }
            }

            // 3. Filo detayları (gruplu) - M-ODEM HARİÇ (Otomasyon/Mobil olarak sayılır)
            var filoDetaylari = await _context.FiloSatislar
                .AsNoTracking()
                .Where(f => f.VardiyaId == vardiyaId && f.FiloKodu != "M-ODEM" && f.FiloAdi != "M-ODEM" && f.FiloAdi != "MOBIL_ODEME")
                .GroupBy(f => (f.FiloAdi == null || f.FiloAdi == "") ? "OTOBIL" : f.FiloAdi)
                .Select(g => new FiloMutabakatDetayDto
                {
                    FiloAdi = g.Key,
                    Tutar = g.Sum(f => f.Tutar),
                    Litre = g.Sum(f => f.Litre),
                    IslemSayisi = g.Count()
                })
                .OrderBy(x => x.FiloAdi)
                .ToListAsync();

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
                    KrediKartiDetayList = p.KrediKartiDetaylari.Select(k => new PusulaKrediKartiDetayDto { BankaAdi = k.BankaAdi, Tutar = k.Tutar }).ToList(),
                    DigerOdemeler = p.DigerOdemeler.Select(d => new PusulaDigerOdemeDto { TurKodu = d.TurKodu, TurAdi = d.TurAdi, Tutar = d.Tutar, Silinemez = d.Silinemez }).ToList(),
                    Veresiyeler = p.Veresiyeler.Select(v => new PusulaVeresiyeDto { CariKartId = v.CariKartId, CariAd = v.CariKart.Ad, Plaka = v.Plaka, Litre = v.Litre, Tutar = v.Tutar, Aciklama = v.Aciklama }).ToList(),
                    Aciklama = p.Aciklama,
                    Toplam = 0
                })
                .ToListAsync();

            foreach (var item in pusulalar)
            {
                Personel? match = null;
                if (item.PersonelId.HasValue && item.PersonelId.Value > 0) match = personels.FirstOrDefault(p => p.Id == item.PersonelId.Value);
                if (match == null && !string.IsNullOrEmpty(item.PersonelAdi)) match = personels.FirstOrDefault(p => string.Equals(p.OtomasyonAdi, item.PersonelAdi.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null && !string.IsNullOrWhiteSpace(match.AdSoyad))
                {
                    item.GercekPersonelAdi = match.AdSoyad;
                    if (!item.PersonelId.HasValue || item.PersonelId.Value == 0) item.PersonelId = match.Id;
                }
            }

            foreach (var p in pusulalar)
            {
                p.Toplam = p.Nakit + p.KrediKarti + p.DigerOdemeler.Sum(d => d.Tutar) + p.Veresiyeler.Sum(v => v.Tutar);
            }

            // 6. Giderler
            var giderler = await _context.PompaGiderler
                .AsNoTracking()
                .Where(g => g.VardiyaId == vardiyaId)
                .Select(g => new GiderMutabakatDto { Id = g.Id, GiderTuru = g.GiderTuru, Tutar = g.Tutar, Aciklama = g.Aciklama })
                .ToListAsync();

            // 7. M-ODEM RECONCILIATION (From FiloSatislar)
            var mobileSales = await _context.FiloSatislar
                .AsNoTracking()
                .Where(f => f.VardiyaId == vardiyaId && (f.FiloKodu == "M-ODEM" || f.FiloAdi == "M-ODEM" || f.FiloAdi == "MOBIL_ODEME"))
                .ToListAsync();

            decimal totalMobile = mobileSales.Sum(m => m.Tutar);

            // 8. Genel Özet Hesaplama
            decimal otomasyonSatisToplam = await _context.OtomasyonSatislar.Where(s => s.VardiyaId == vardiyaId).SumAsync(s => s.ToplamTutar);
            
            var genelOzet = new GenelMutabakatOzetDto
            {
                ToplamOtomasyon = otomasyonSatisToplam + totalMobile,
                ToplamGider = giderler.Sum(g => g.Tutar),
                MarketToplam = vardiya.MarketToplam,
                ToplamNakit = pusulalar.Sum(p => p.Nakit),
                ToplamKrediKarti = pusulalar.Sum(p => p.KrediKarti),
                ToplamPusula = pusulalar.Sum(p => p.Toplam)
            };

            // M-ODEM'i pusula listesine sanal olarak ekle (UI'da görünmesi için)
            if (totalMobile > 0 && !pusulalar.Any(p => p.DigerOdemeler.Any(d => d.TurKodu == "MOBIL_ODEME")))
            {
                // En az bir pusula varsa ona ekle veya genel bir pusula gibi davran
                var firstPusula = pusulalar.FirstOrDefault();
                if (firstPusula != null)
                {
                    firstPusula.DigerOdemeler.Add(new PusulaDigerOdemeDto { TurKodu = "MOBIL_ODEME", TurAdi = "Mobil Ödeme (Sistem)", Tutar = totalMobile, Silinemez = true });
                    firstPusula.Toplam += totalMobile;
                    genelOzet.ToplamPusula += totalMobile;
                }
            }

            var toplamTahsilat = genelOzet.ToplamPusula + filoOzet.ToplamTutar + genelOzet.ToplamGider;
            
            genelOzet.Fark = toplamTahsilat - genelOzet.ToplamOtomasyon;

            return new MutabakatViewModel
            {
                Vardiya = new VardiyaSummaryDto { Id = vardiya.Id, IstasyonId = vardiya.IstasyonId, BaslangicTarihi = vardiya.BaslangicTarihi, BitisTarihi = vardiya.BitisTarihi, Durum = (int)vardiya.Durum, DosyaAdi = vardiya.DosyaAdi, GenelToplam = genelOzet.ToplamOtomasyon },
                PersonelOzetler = personelOzetler,
                FiloOzet = filoOzet,
                FiloDetaylari = filoDetaylari,
                Pusulalar = pusulalar,
                Giderler = giderler,
                GenelOzet = genelOzet
            };
        }

        public async Task ProcessVardiyaApproval(int vardiyaId, int onaylayanId)
        {
            // İdempotency Check: Already processed?
            bool alreadyProcessed = await _context.CariHareketler.AnyAsync(c => c.VardiyaId == vardiyaId);
            if (alreadyProcessed) 
            {
                _logger.LogInformation($"Vardiya {vardiyaId} finansal onayı daha önce işlenmiş, atlanıyor.");
                return;
            }

            // Vardiya bilgilerini çek (Tarih için)
            var vardiya = await _context.Vardiyalar.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vardiyaId);
            string vardiyaTarihi = vardiya?.BaslangicTarihi.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy");

            var veresiyeler = await _context.PusulaVeresiyeler
                .Include(pv => pv.CariKart)
                .Include(pv => pv.Pusula) // Pompacı adı için gerekli
                .Where(pv => pv.Pusula.VardiyaId == vardiyaId)
                .ToListAsync();

            if (!veresiyeler.Any()) return;

            foreach (var veresiye in veresiyeler)
            {
                var hareket = new CariHareket
                {
                    CariKartId = veresiye.CariKartId,
                    Tarih = DateTime.UtcNow,
                    IslemTipi = "SATIS",
                    Tutar = veresiye.Tutar,
                    Aciklama = $"Vardiya #{vardiyaId} ({vardiyaTarihi}) - {veresiye.Pusula.PersonelAdi} - Plaka: {veresiye.Plaka} - {veresiye.Litre:F2} L - {veresiye.Tutar:F2} TL" + (string.IsNullOrEmpty(veresiye.Aciklama) ? "" : $" - {veresiye.Aciklama}"),
                    VardiyaId = vardiyaId, // Important for idempotency
                    OlusturanId = onaylayanId,
                    OlusturmaTarihi = DateTime.UtcNow
                };

                _context.CariHareketler.Add(hareket);
                veresiye.CariKart.Bakiye += veresiye.Tutar;
                veresiye.CariKart.GuncellemeTarihi = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
