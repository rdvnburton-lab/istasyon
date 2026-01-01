using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IstasyonDemo.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StokController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AutoMapper.IMapper _mapper;
        private readonly StokHesaplamaService _stokHesaplama;

        public StokController(AppDbContext context, AutoMapper.IMapper mapper, StokHesaplamaService stokHesaplama)
        {
            _context = context;
            _mapper = mapper;
            _stokHesaplama = stokHesaplama;
        }

        [HttpGet("girisler")]
        public async Task<IActionResult> GetGirisler(
            [FromQuery] int? month, 
            [FromQuery] int? year, 
            [FromQuery] DateTime? startDate, 
            [FromQuery] DateTime? endDate)
        {
            var query = _context.TankGirisler
                .Include(t => t.Yakit)
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(t => t.Tarih >= start && t.Tarih <= end);
            }
            else if (year.HasValue)
            {
                if (month.HasValue && month > 0)
                {
                    query = query.Where(t => t.Tarih.Month == month && t.Tarih.Year == year);
                }
                else
                {
                    query = query.Where(t => t.Tarih.Year == year);
                }
            }

            var flatList = await query
                .OrderByDescending(t => t.Tarih)
                .ToListAsync();

            // Group by FaturaNo and Date to simulate "Invoices"
            var grouped = flatList
                .GroupBy(x => new { x.FaturaNo, x.Tarih })
                .Select(g => new IstasyonDemo.Api.Dtos.StokGirisFisDto
                {
                    FaturaNo = g.Key.FaturaNo,
                    Tarih = g.Key.Tarih, 
                    Kaydeden = g.First().Kaydeden,
                    GelisYontemi = g.First().GelisYontemi,
                    Plaka = g.First().Plaka,
                    UrunGirisTarihi = g.First().UrunGirisTarihi,
                    ToplamTutar = g.Sum(x => x.ToplamTutar),
                    ToplamLitre = g.Sum(x => x.Litre),
                    Kalemler = _mapper.Map<List<IstasyonDemo.Api.Dtos.TankGirisDto>>(g.OrderByDescending(i => i.Litre).ToList())
                })
                .OrderByDescending(x => x.Tarih)
                .ToList();

            return Ok(grouped);
        }

        [HttpPost("giris")] // Keeping same endpoint name or changing to "fatura-giris"? Keeping 'giris' might break if FE sends array. Let's make new endpoint or update. The user asked for "fatura bazlı" change. I will replace the logic of this endpoint but maybe rename it to be clear.
        [HttpPost("fatura-giris")]
        public async Task<IActionResult> AddFaturaGiris([FromBody] IstasyonDemo.Api.Dtos.CreateFaturaGirisDto dto)
        {
            if (dto == null || dto.Kalemler == null || !dto.Kalemler.Any()) 
                return BadRequest("Fatura bilgileri veya kalemler eksik.");
            
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var kaydeden = User.Identity?.Name ?? dto.Kaydeden ?? "Sistem";
            var now = DateTime.UtcNow;

            foreach (var item in dto.Kalemler)
            {
                var entity = new TankGiris
                {
                    Id = Guid.NewGuid(),
                    Tarih = dto.Tarih, // All items share same date
                    FaturaNo = dto.FaturaNo, // All items share same invoice no
                    YakitId = item.YakitId,
                    Litre = item.Litre,
                    BirimFiyat = item.BirimFiyat,
                    ToplamTutar = item.Litre * item.BirimFiyat,
                    Kaydeden = kaydeden,
                    GelisYontemi = dto.GelisYontemi,
                    Plaka = dto.Plaka,
                    UrunGirisTarihi = dto.UrunGirisTarihi,
                    CreatedAt = now
                };
                
                _context.TankGirisler.Add(entity);
            }

            await _context.SaveChangesAsync();

            // FIFO stok takip kaydı oluştur - her yakıt türü için ayrı kayıt
            foreach (var item in dto.Kalemler)
            {
                await _stokHesaplama.FaturaStokKaydiOlustur(
                    dto.FaturaNo, 
                    item.YakitId, 
                    dto.Tarih, 
                    item.Litre
                );
            }

            return Ok(new { message = "Fatura girişi başarıyla kaydedildi." });
        }

        [HttpDelete("giris/{id}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> DeleteGiris(Guid id)
        {
            var giris = await _context.TankGirisler.FindAsync(id);
            if (giris == null) return NotFound();

            _context.TankGirisler.Remove(giris);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("fatura/{faturaNo}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> DeleteFatura(string faturaNo)
        {
            var items = await _context.TankGirisler.Where(x => x.FaturaNo == faturaNo).ToListAsync();
            if (!items.Any()) return NotFound("Fatura bulunamadı.");

            _context.TankGirisler.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Fatura ve içeriği silindi." });
        }

        [HttpPut("fatura-giris/{oldFaturaNo}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> UpdateFatura(string oldFaturaNo, [FromBody] IstasyonDemo.Api.Dtos.CreateFaturaGirisDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Find and Delete Old Items
                var oldItems = await _context.TankGirisler.Where(x => x.FaturaNo == oldFaturaNo).ToListAsync();
                if (oldItems.Any())
                {
                    _context.TankGirisler.RemoveRange(oldItems);
                }

                // 2. Create New Items
                var kaydeden = User.Identity?.Name ?? dto.Kaydeden ?? "Sistem";
                var now = DateTime.UtcNow;

                foreach (var item in dto.Kalemler)
                {
                    var entity = new TankGiris
                    {
                        Id = Guid.NewGuid(),
                        Tarih = dto.Tarih,
                        FaturaNo = dto.FaturaNo,
                        YakitId = item.YakitId,
                        Litre = item.Litre,
                        BirimFiyat = item.BirimFiyat,
                        ToplamTutar = item.Litre * item.BirimFiyat,
                        Kaydeden = kaydeden,
                        GelisYontemi = dto.GelisYontemi,
                        Plaka = dto.Plaka,
                        UrunGirisTarihi = dto.UrunGirisTarihi,
                        CreatedAt = now
                    };
                    _context.TankGirisler.Add(entity);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return Ok(new { message = "Fatura güncellendi." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Güncelleme sırasında hata oluştu: " + ex.Message);
            }
        }

        [HttpGet("ozet")]
        public async Task<IActionResult> GetOzet([FromQuery] int month, [FromQuery] int year)
        {
            var startDate = DateTime.SpecifyKind(new DateTime(year, month, 1), DateTimeKind.Utc);
            var nextMonth = startDate.AddMonths(1);

            // 1. Fetch Fuel Definitions
            var yakitlar = await _context.Yakitlar.OrderBy(y => y.Sira).ToListAsync();
            
            // 2. Aggregate Inputs (TankGiris) by YakitId
            // Using separate queries to ensure cleaner Translation to SQL for different date ranges
            var inputsBefore = await _context.TankGirisler
                .Where(t => t.Tarih < startDate)
                .GroupBy(t => t.YakitId)
                .Select(g => new { YakitId = g.Key, Total = g.Sum(t => t.Litre) })
                .ToListAsync();

            var inputsThisMonth = await _context.TankGirisler
                .Where(t => t.Tarih >= startDate && t.Tarih < nextMonth)
                .GroupBy(t => t.YakitId)
                .Select(g => new { YakitId = g.Key, Total = g.Sum(t => t.Litre) })
                .ToListAsync();

            // 3. Aggregate Sales (OtomasyonSatis) by YakitTuru
            // OtomasyonSatis relates to Vardiya for Date info
            var salesBefore = await _context.OtomasyonSatislar
                // Sadece ONAYLANAN vardiyaların satışlarını stoktan düş
                .Where(s => s.Vardiya != null && s.Vardiya.Durum == VardiyaDurum.ONAYLANDI && s.Vardiya.BaslangicTarihi < startDate)
                .GroupBy(s => s.YakitTuru)
                .Select(g => new { YakitTuru = g.Key, Total = g.Sum(s => s.Litre) })
                .ToListAsync();

            var salesThisMonth = await _context.OtomasyonSatislar
                // Sadece ONAYLANAN vardiyaların satışlarını stoktan düş
                .Where(s => s.Vardiya != null && s.Vardiya.Durum == VardiyaDurum.ONAYLANDI && s.Vardiya.BaslangicTarihi >= startDate && s.Vardiya.BaslangicTarihi < nextMonth)
                .GroupBy(s => s.YakitTuru)
                .Select(g => new { YakitTuru = g.Key, Total = g.Sum(s => s.Litre) })
                .ToListAsync();

            var ozetList = new List<IstasyonDemo.Api.Dtos.TankStokOzetDto>();

            foreach (var yakit in yakitlar)
            {
                // Match Inputs
                var inBefore = inputsBefore.FirstOrDefault(x => x.YakitId == yakit.Id)?.Total ?? 0;
                var inMonth = inputsThisMonth.FirstOrDefault(x => x.YakitId == yakit.Id)?.Total ?? 0;

                // Match Sales
                // Parse keywords from Yakit definition (e.g. "MOTORIN,DIZEL")
                var keywords = (yakit.OtomasyonUrunAdi ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim().ToUpper())
                    .ToList();

                // Helper to check if an aggregated sale group belongs to this fuel type
                bool Matches(string saleYakitTuruStr) 
                {
                    if (string.IsNullOrEmpty(saleYakitTuruStr)) return false;
                    
                    // Convert numeric values to enum names for matching
                    string normalizedValue = saleYakitTuruStr;
                    if (int.TryParse(saleYakitTuruStr, out var intValue) && Enum.IsDefined(typeof(YakitTuru), intValue))
                    {
                        normalizedValue = ((YakitTuru)intValue).ToString();
                    }
                    
                    var u = normalizedValue.ToUpper();
                    return keywords.Any(k => u.Contains(k));
                }

                var outBefore = salesBefore
                    .Where(x => Matches(x.YakitTuru.ToString()))
                    .Sum(x => x.Total);
                
                var outMonth = salesThisMonth
                    .Where(x => Matches(x.YakitTuru.ToString()))
                    .Sum(x => x.Total);

                var devir = inBefore - outBefore;
                var kalan = devir + inMonth - outMonth;

                ozetList.Add(new IstasyonDemo.Api.Dtos.TankStokOzetDto
                {
                    YakitId = yakit.Id,
                    YakitTuru = yakit.Ad,
                    Renk = yakit.Renk,
                    GecenAyDevir = devir,
                    BuAyGiris = inMonth,
                    BuAySatis = outMonth,
                    KalanStok = kalan
                });
            }

            // Helper function to convert YakitTuru value to readable Turkish name from Yakitlar table
            string GetYakitTuruAdi(string yakitTuruValue)
            {
                if (string.IsNullOrEmpty(yakitTuruValue)) return yakitTuruValue;
                
                // First convert numeric to enum name if needed
                string normalizedValue = yakitTuruValue;
                if (int.TryParse(yakitTuruValue, out var intValue) && Enum.IsDefined(typeof(YakitTuru), intValue))
                {
                    normalizedValue = ((YakitTuru)intValue).ToString();
                }
                
                // Now find matching Yakit from the database using keyword matching
                var upperValue = normalizedValue.ToUpper();
                foreach (var yakit in yakitlar)
                {
                    var keywords = (yakit.OtomasyonUrunAdi ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => k.Trim().ToUpper());
                    if (keywords.Any(k => upperValue.Contains(k)))
                    {
                        return yakit.Ad; // Return Turkish name from database
                    }
                }
                
                // Return normalized value if no match found
                return normalizedValue;
            }

            // Debug: Include raw data for troubleshooting (Turkish labels)
            return Ok(new {
                Ozet = ozetList,
                Debug = new {
                    BuAyOnaylananSatislar = salesThisMonth.Select(s => new { YakitTuru = GetYakitTuruAdi(s.YakitTuru), ToplamLitre = s.Total }),
                    GecmisOnaylananSatislar = salesBefore.Select(s => new { YakitTuru = GetYakitTuruAdi(s.YakitTuru), ToplamLitre = s.Total }),
                    YakitEslestirmeAnahtarlari = yakitlar.Select(y => new { YakitAdi = y.Ad, OtomasyonAnahtarlari = y.OtomasyonUrunAdi }),
                    OnaylananVardiyaSayisi = _context.Vardiyalar.Count(v => v.Durum == VardiyaDurum.ONAYLANDI),
                    Aciklama = "Bu ay satış verisi yoksa, seçilen dönemde onaylanmış vardiya bulunmuyor olabilir. OtomasyonAnahtarlari değerleri, vardiya dosyasındaki YakitTuru değerleri ile eşleşmelidir."
                }
            });
        }

        /// <summary>
        /// Aylık stok raporu - hesaplanmış verileri döndürür
        /// </summary>
        [HttpGet("aylik-rapor")]
        public async Task<IActionResult> GetAylikRapor([FromQuery] int yil, [FromQuery] int ay)
        {
            // Hesapla veya mevcut veriyi getir
            var ozetler = await _stokHesaplama.HesaplaTumYakitlar(yil, ay);
            
            return Ok(ozetler.Select(o => new {
                o.Id,
                o.YakitId,
                YakitAdi = o.Yakit?.Ad ?? "?",
                Renk = o.Yakit?.Renk ?? "#666",
                o.Yil,
                o.Ay,
                o.DevirStok,
                o.AyGiris,
                o.AySatis,
                o.KalanStok,
                o.HesaplamaZamani,
                o.Kilitli
            }));
        }

        /// <summary>
        /// Fatura bazında stok durumu - FIFO takibi
        /// </summary>
        [HttpGet("fatura-stok-durumu")]
        public async Task<IActionResult> GetFaturaStokDurumu([FromQuery] int? yakitId = null)
        {
            var faturalar = await _stokHesaplama.GetFaturaStokDurumu(yakitId);
            
            return Ok(faturalar.Select(f => new {
                f.Id,
                f.FaturaNo,
                f.YakitId,
                YakitAdi = f.Yakit?.Ad ?? "?",
                f.FaturaTarihi,
                f.GirenMiktar,
                f.KalanMiktar,
                TuketilenMiktar = f.GirenMiktar - f.KalanMiktar,
                TuketimYuzdesi = f.GirenMiktar > 0 ? Math.Round((f.GirenMiktar - f.KalanMiktar) / f.GirenMiktar * 100, 1) : 0,
                f.Tamamlandi
            }));
        }

        /// <summary>
        /// Stoku yeniden hesapla (düzeltme için)
        /// </summary>
        [HttpPost("yeniden-hesapla")]
        public async Task<IActionResult> YenidenHesapla([FromQuery] int yil, [FromQuery] int ay)
        {
            // Kilidi kaldır ve yeniden hesapla
            var mevcutlar = await _context.AylikStokOzetleri
                .Where(a => a.Yil == yil && a.Ay == ay)
                .ToListAsync();
            
            foreach (var m in mevcutlar)
            {
                m.Kilitli = false;
            }
            await _context.SaveChangesAsync();

            var sonuc = await _stokHesaplama.HesaplaTumYakitlar(yil, ay);
            return Ok(new { message = "Stok başarıyla yeniden hesaplandı.", sonuc });
        }

        /// <summary>
        /// Ay kapatma işlemi
        /// </summary>
        [HttpPost("ay-kapat")]
        public async Task<IActionResult> AyKapat([FromQuery] int yil, [FromQuery] int ay)
        {
            await _stokHesaplama.AyiKapat(yil, ay);
            return Ok(new { message = $"{ay}/{yil} ayı başarıyla kapatıldı." });
        }
    }
}
