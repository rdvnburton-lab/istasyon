using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
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

        public StokController(AppDbContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
            return Ok(new { message = "Fatura girişi başarıyla kaydedildi." });
        }

        [HttpDelete("giris/{id}")]
        public async Task<IActionResult> DeleteGiris(Guid id)
        {
            var giris = await _context.TankGirisler.FindAsync(id);
            if (giris == null) return NotFound();

            _context.TankGirisler.Remove(giris);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("fatura/{faturaNo}")]
        public async Task<IActionResult> DeleteFatura(string faturaNo)
        {
            var items = await _context.TankGirisler.Where(x => x.FaturaNo == faturaNo).ToListAsync();
            if (!items.Any()) return NotFound("Fatura bulunamadı.");

            _context.TankGirisler.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Fatura ve içeriği silindi." });
        }

        [HttpPut("fatura-giris/{oldFaturaNo}")]
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

            // Fetch Fuel Types (Yakit Definitions)
            var yakitlar = await _context.Yakitlar.OrderBy(y => y.Sira).ToListAsync();
            
            // --- INPUTS (TankGiris) ---
            var allInputs = await _context.TankGirisler.Where(t => t.Tarih < nextMonth).ToListAsync();

            // --- SALES (OtomasyonSatis) ---
            // OtomasyonSatis table has 'YakitTuru' (Enum). We convert it to string to match with 'OtomasyonUrunAdi' keywords.
            var allSales = await _context.OtomasyonSatislar
                .Include(s => s.Vardiya)
                .Where(s => s.Vardiya != null && s.Vardiya.BaslangicTarihi < nextMonth)
                .Select(s => new { s.Vardiya.BaslangicTarihi, YakitTuruStr = s.YakitTuru.ToString(), s.Litre })
                .ToListAsync();

            var ozetList = new List<object>();

            foreach (var yakit in yakitlar)
            {
                // Inputs for this specific Fuel Type
                var inputsBefore = allInputs.Where(x => x.YakitId == yakit.Id && x.Tarih < startDate).Sum(x => x.Litre);
                var inputsThisMonth = allInputs.Where(x => x.YakitId == yakit.Id && x.Tarih >= startDate && x.Tarih < nextMonth).Sum(x => x.Litre);

                // Sales Matching Logic
                // yakit.OtomasyonUrunAdi example: "MOTORIN,DIZEL"
                // OtomasyonSatis.YakitTuru.ToString() example: "MOTORIN", "EURO_DIESEL"
                var keywords = (yakit.OtomasyonUrunAdi ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(k => k.Trim().ToUpper()).ToList();

                // Helper func to check if sale matches this fuel type
                bool Matches(string saleYakitStr) 
                {
                    if (string.IsNullOrEmpty(saleYakitStr)) return false;
                    var u = saleYakitStr.ToUpper();
                    // Match if Enum String contains any keyword (e.g. EURO_DIESEL contains DIESEL)
                    return keywords.Any(k => u.Contains(k));
                }

                var salesBefore = allSales.Where(x => x.BaslangicTarihi < startDate && Matches(x.YakitTuruStr)).Sum(x => x.Litre);
                var salesThisMonth = allSales.Where(x => x.BaslangicTarihi >= startDate && x.BaslangicTarihi < nextMonth && Matches(x.YakitTuruStr)).Sum(x => x.Litre);

                var devir = inputsBefore - salesBefore;

                ozetList.Add(new IstasyonDemo.Api.Dtos.TankStokOzetDto
                {
                    YakitId = yakit.Id,
                    YakitTuru = yakit.Ad,
                    Renk = yakit.Renk, // Pass color to UI
                    GecenAyDevir = devir,
                    BuAyGiris = inputsThisMonth,
                    BuAySatis = salesThisMonth,
                    KalanStok = devir + inputsThisMonth - salesThisMonth
                });
            }

            return Ok(ozetList);
        }
    }
}
