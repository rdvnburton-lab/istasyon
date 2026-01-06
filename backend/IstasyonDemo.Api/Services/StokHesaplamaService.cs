using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Services
{
    /// <summary>
    /// Yakıt stok hesaplama servisi - FIFO mantığı ile aylık stok takibi
    /// </summary>
    public class StokHesaplamaService
    {
        private readonly AppDbContext _context;
        private readonly IYakitService _yakitService;

        public StokHesaplamaService(AppDbContext context, IYakitService yakitService)
        {
            _context = context;
            _yakitService = yakitService;
        }

        /// <summary>
        /// Belirli bir yakıt türü için aylık stok özetini hesapla veya güncelle
        /// </summary>
        public async Task<AylikStokOzeti> HesaplaAylikStok(int yakitId, int yil, int ay)
        {
            var startDate = new DateTime(yil, ay, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            // Mevcut kaydı bul veya yeni oluştur
            var ozet = await _context.AylikStokOzetleri
                .FirstOrDefaultAsync(a => a.YakitId == yakitId && a.Yil == yil && a.Ay == ay);

            if (ozet == null)
            {
                ozet = new AylikStokOzeti
                {
                    YakitId = yakitId,
                    Yil = yil,
                    Ay = ay
                };
                _context.AylikStokOzetleri.Add(ozet);
            }

            // Kilitli ay güncellenmez
            if (ozet.Kilitli)
                return ozet;

            // 1. Devir stok - önceki aydan kalan
            ozet.DevirStok = await GetOncekiAyKalanStok(yakitId, yil, ay);

            // 2. Bu ay girişler (faturalar)
            ozet.AyGiris = await _context.TankGirisler
                .Where(t => t.YakitId == yakitId && t.Tarih >= startDate && t.Tarih < endDate)
                .SumAsync(t => t.Litre);

            // 3. Bu ay satışlar (onaylanan vardiyalar)
            var yakit = await _context.Yakitlar.FindAsync(yakitId);
            ozet.AySatis = await HesaplaAySatislari(yakit!, startDate, endDate);

            // 4. Kalan stok hesapla
            ozet.KalanStok = ozet.DevirStok + ozet.AyGiris - ozet.AySatis;
            ozet.HesaplamaZamani = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ozet;
        }

        /// <summary>
        /// Önceki aydan kalan stoku getir
        /// </summary>
        private async Task<decimal> GetOncekiAyKalanStok(int yakitId, int yil, int ay)
        {
            int oncekiAy = ay - 1;
            int oncekiYil = yil;
            
            if (oncekiAy < 1)
            {
                oncekiAy = 12;
                oncekiYil = yil - 1;
            }

            var oncekiOzet = await _context.AylikStokOzetleri
                .FirstOrDefaultAsync(a => a.YakitId == yakitId && a.Yil == oncekiYil && a.Ay == oncekiAy);

            return oncekiOzet?.KalanStok ?? 0;
        }

        /// <summary>
        /// Onaylanan vardiyalardan satışları hesapla (keyword eşleştirme ile)
        /// </summary>
        private async Task<decimal> HesaplaAySatislari(Yakit yakit, DateTime startDate, DateTime endDate)
        {
            // Otomasyon Satışlarını çek
            var otomasyonSatislar = await _context.OtomasyonSatislar
                .Where(s => s.Vardiya != null && 
                            s.Vardiya.Durum == VardiyaDurum.ONAYLANDI &&
                            s.Vardiya.BaslangicTarihi >= startDate && 
                            s.Vardiya.BaslangicTarihi < endDate)
                .ToListAsync();

            // Filo Satışlarını çek
            var filoSatislar = await _context.FiloSatislar
                .Where(s => s.Vardiya != null && 
                            s.Vardiya.Durum == VardiyaDurum.ONAYLANDI &&
                            s.Vardiya.BaslangicTarihi >= startDate && 
                            s.Vardiya.BaslangicTarihi < endDate)
                .ToListAsync();

            decimal toplam = 0;

            // Otomasyon satışlarını topla
            foreach (var satis in otomasyonSatislar)
            {
                var identifiedYakit = await _yakitService.IdentifyYakitAsync(satis.YakitTuru);
                if (identifiedYakit != null && identifiedYakit.Id == yakit.Id)
                {
                    toplam += satis.Litre;
                }
            }

            // Filo satışlarını topla
            foreach (var satis in filoSatislar)
            {
                var identifiedYakit = await _yakitService.IdentifyYakitAsync(satis.YakitTuru);
                if (identifiedYakit != null && identifiedYakit.Id == yakit.Id)
                {
                    toplam += satis.Litre;
                }
            }

            return toplam;
        }

        /// <summary>
        /// Tüm yakıtlar için aylık stok hesapla
        /// </summary>
        public async Task<List<AylikStokOzeti>> HesaplaTumYakitlar(int yil, int ay)
        {
            var yakitlar = await _context.Yakitlar.ToListAsync();
            var sonuclar = new List<AylikStokOzeti>();

            foreach (var yakit in yakitlar)
            {
                var ozet = await HesaplaAylikStok(yakit.Id, yil, ay);
                sonuclar.Add(ozet);
            }

            return sonuclar;
        }

        /// <summary>
        /// Ay kapatma - Stoku kilitle. Otomatik olarak ay sonunda veya istendiğinde çağrılır.
        /// </summary>
        public async Task AyiKapat(int yil, int ay)
        {
            // Önce tüm yakıtlar için hesapla
            await HesaplaTumYakitlar(yil, ay);

            // Sonra kilitle
            var ozetler = await _context.AylikStokOzetleri
                .Where(a => a.Yil == yil && a.Ay == ay)
                .ToListAsync();

            foreach (var ozet in ozetler)
            {
                ozet.Kilitli = true;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Fatura stok takip kaydı oluştur (yeni fatura girildiğinde çağrılır)
        /// </summary>
        public async Task FaturaStokKaydiOlustur(string faturaNo, int yakitId, DateTime faturaTarihi, decimal litre)
        {
            var mevcut = await _context.FaturaStokTakipleri
                .FirstOrDefaultAsync(f => f.FaturaNo == faturaNo && f.YakitId == yakitId);

            if (mevcut != null)
            {
                // Güncelle
                mevcut.GirenMiktar = litre;
                mevcut.KalanMiktar = litre; // Reset - satışlarla yeniden hesaplanacak
                mevcut.FaturaTarihi = faturaTarihi;
                mevcut.GuncellenmeTarihi = DateTime.UtcNow;
                mevcut.Tamamlandi = false;
            }
            else
            {
                // Yeni kayıt
                var kayit = new FaturaStokTakip
                {
                    FaturaNo = faturaNo,
                    YakitId = yakitId,
                    FaturaTarihi = faturaTarihi,
                    GirenMiktar = litre,
                    KalanMiktar = litre,
                    Tamamlandi = false
                };
                _context.FaturaStokTakipleri.Add(kayit);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// FIFO mantığı ile satış düş - en eski faturalardan başlayarak
        /// </summary>
        public async Task FIFOSatisDus(int yakitId, decimal satisLitre)
        {
            if (satisLitre <= 0) return;

            // Tamamlanmamış faturaları tarih sırasına göre getir
            var faturalar = await _context.FaturaStokTakipleri
                .Where(f => f.YakitId == yakitId && !f.Tamamlandi && f.KalanMiktar > 0)
                .OrderBy(f => f.FaturaTarihi)
                .ToListAsync();

            decimal kalanSatis = satisLitre;

            foreach (var fatura in faturalar)
            {
                if (kalanSatis <= 0) break;

                if (fatura.KalanMiktar >= kalanSatis)
                {
                    // Bu faturadan yeterli var
                    fatura.KalanMiktar -= kalanSatis;
                    kalanSatis = 0;
                }
                else
                {
                    // Bu fatura yetmez, tamamını düş ve sonrakine geç
                    kalanSatis -= fatura.KalanMiktar;
                    fatura.KalanMiktar = 0;
                }

                if (fatura.KalanMiktar == 0)
                {
                    fatura.Tamamlandi = true;
                }

                fatura.GuncellenmeTarihi = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Fatura stok durumunu getir
        /// </summary>
        public async Task<List<FaturaStokTakip>> GetFaturaStokDurumu(int? yakitId = null)
        {
            var query = _context.FaturaStokTakipleri
                .Include(f => f.Yakit)
                .OrderBy(f => f.FaturaTarihi)
                .AsQueryable();

            if (yakitId.HasValue)
            {
                query = query.Where(f => f.YakitId == yakitId.Value);
            }

            return await query.ToListAsync();
        }
    }
}
