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
        private readonly IYakitService _yakitService;

        public StokController(AppDbContext context, AutoMapper.IMapper mapper, StokHesaplamaService stokHesaplama, IYakitService yakitService)
        {
            _context = context;
            _mapper = mapper;
            _stokHesaplama = stokHesaplama;
            _yakitService = yakitService;
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
        /// Karma stok özeti - XML (Motorin/Benzin) + Manuel (LPG)
        /// </summary>
        [HttpGet("karma-ozet")]
        public async Task<IActionResult> GetKarmaOzet([FromQuery] int? istasyonId, [FromQuery] int yil, [FromQuery] int ay)
        {
            var startDate = DateTime.SpecifyKind(new DateTime(yil, ay, 1), DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            // 1. XML Kaynaklı (VardiyaTankEnvanterleri) - Motorin, Benzin
            // Tüm yakıtları çekip hangilerinin XML kaynaklı olduğunu (TurpakUrunKodu olanlar) bulalım
            var tumYakitlar = await _yakitService.GetAllYakitlarAsync();
            var xmlYakitAdlari = tumYakitlar
                .Where(y => !string.IsNullOrEmpty(y.TurpakUrunKodu) && (y.Ad.ToUpper().Contains("MOTORIN") || y.Ad.ToUpper().Contains("BENZIN")))
                .Select(y => y.Ad)
                .ToList();

            var xmlQuery = _context.VardiyaTankEnvanterleri
                .Include(t => t.Vardiya)
                .Where(t => t.KayitTarihi >= startDate && t.KayitTarihi < endDate);

            if (istasyonId.HasValue)
            {
                xmlQuery = xmlQuery.Where(t => t.Vardiya != null && t.Vardiya.IstasyonId == istasyonId.Value);
            }

            var xmlVerileri = await xmlQuery.ToListAsync();

            // Yakıt tipine göre grupla ve özetle
            var xmlOzetRaw = xmlVerileri
                .GroupBy(t => t.YakitTipi)
                .ToList();

            var xmlOzet = new List<dynamic>();
            foreach (var g in xmlOzetRaw)
            {
                var yakit = await _yakitService.IdentifyYakitAsync(g.Key);
                var yakitAdi = yakit?.Ad ?? g.Key ?? "Bilinmeyen";

                // Her tankın bu dönemdeki İLK ve SON kaydını bulalım
                var tankBazliGruplar = g.GroupBy(t => t.TankNo).ToList();
                
                decimal toplamIlkStok = 0;
                decimal toplamSonStok = 0;
                decimal toplamSevkiyat = 0;

                foreach (var tankGrubu in tankBazliGruplar)
                {
                    toplamIlkStok += tankGrubu.OrderBy(t => t.KayitTarihi).FirstOrDefault()?.BaslangicStok ?? 0;
                    toplamSonStok += tankGrubu.OrderByDescending(t => t.KayitTarihi).FirstOrDefault()?.BitisStok ?? 0;
                    toplamSevkiyat += tankGrubu.Sum(t => t.SevkiyatMiktar);
                }

                // Bu yakıt tipi için gerçek satışları (Otomasyon + Filo) bulalım
                // Not: tumSatislar henüz bu noktada tanımlı değil, aşağıdan yukarı taşıyacağız veya burada hesaplayacağız.
                // En iyisi tumSatislar'ı en başa çekmek.
                
                xmlOzet.Add(new
                {
                    YakitTipi = yakitAdi,
                    YakitId = yakit?.Id,
                    Tanimli = yakit != null, // Flag to indicate if it's in the database
                    Renk = yakit?.Renk ?? "#666",
                    ToplamSevkiyat = toplamSevkiyat,
                    SonStok = toplamSonStok,
                    IlkStok = toplamIlkStok,
                    KayitSayisi = g.Select(t => t.VardiyaId).Distinct().Count()
                });
            }

            // 2. Satışlar (Otomasyon + Filo) - Tüm vardiyalar için
            var allVardiyaIds = xmlVerileri.Select(v => v.VardiyaId).Distinct().ToList();
            
            var otomasyonSatislar = await _context.OtomasyonSatislar
                .Where(s => allVardiyaIds.Contains(s.VardiyaId))
                .ToListAsync();
            
            var filoSatislar = await _context.FiloSatislar
                .Where(s => allVardiyaIds.Contains(s.VardiyaId))
                .ToListAsync();

            // Tüm satışları birleştirelim (yakıt tanıma için)
            var tumSatislar = otomasyonSatislar.Select(s => new { s.VardiyaId, s.YakitTuru, s.Litre })
                .Concat(filoSatislar.Select(s => new { s.VardiyaId, s.YakitTuru, s.Litre }))
                .ToList();

            // 3. XML Kaynaklı Özetini Tamamla (Satış ve Fark Hesapla)
            var xmlOzetFinal = new List<object>();
            foreach (var item in xmlOzet)
            {
                decimal toplamSatis = 0;
                foreach (var s in tumSatislar)
                {
                    var identified = await _yakitService.IdentifyYakitAsync(s.YakitTuru);
                    if (identified != null && identified.Id == item.YakitId)
                    {
                        toplamSatis += s.Litre;
                    }
                }

                // Fark = (İlk Stok + Sevkiyat - Son Stok) - Satışlar
                decimal beklenenSatis = item.IlkStok + item.ToplamSevkiyat - item.SonStok;
                decimal fark = beklenenSatis - toplamSatis;

                xmlOzetFinal.Add(new
                {
                    item.YakitTipi,
                    item.Renk,
                    item.Tanimli,
                    item.ToplamSevkiyat,
                    ToplamSatis = toplamSatis,
                    item.SonStok,
                    item.IlkStok,
                    ToplamFark = fark,
                    item.KayitSayisi
                });
            }

            // 4. Manuel Kaynaklı (TankGiris) - LPG
            var lpgYakitlar = await _context.Yakitlar
                .Where(y => y.Ad.ToUpper().Contains("LPG") || y.Ad.ToUpper().Contains("OTOGAZ"))
                .Select(y => y.Id)
                .ToListAsync();

            var lpgGirisler = await _context.TankGirisler
                .Where(t => t.Tarih >= startDate && t.Tarih < endDate)
                .Where(t => lpgYakitlar.Contains(t.YakitId))
                .SumAsync(t => t.Litre);

            var lpgYakitEntity = tumYakitlar.FirstOrDefault(y => y.Ad.ToUpper().Contains("LPG") || y.Ad.ToUpper().Contains("OTOGAZ"));
            
            var lpgSalesPerVardiya = new List<dynamic>();
            if (lpgYakitEntity != null)
            {
                var lpgSatisGruplari = new Dictionary<int, decimal>();
                foreach (var s in tumSatislar)
                {
                    var identified = await _yakitService.IdentifyYakitAsync(s.YakitTuru);
                    if (identified != null && identified.Id == lpgYakitEntity.Id)
                    {
                        if (!lpgSatisGruplari.ContainsKey(s.VardiyaId)) lpgSatisGruplari[s.VardiyaId] = 0;
                        lpgSatisGruplari[s.VardiyaId] += s.Litre;
                    }
                }
                lpgSalesPerVardiya = lpgSatisGruplari.Select(x => new { VardiyaId = x.Key, Litre = x.Value }).Cast<dynamic>().ToList();
            }

            var lpgSatislar = lpgSalesPerVardiya.Sum(x => (decimal)x.Litre);

            var manuelOzet = new List<object>();
            if (lpgYakitlar.Any() || lpgSatislar > 0)
            {
                manuelOzet.Add(new
                {
                    YakitTipi = "LPG",
                    Renk = lpgYakitEntity?.Renk ?? "#3b82f6",
                    ToplamGiris = lpgGirisler,
                    ToplamSatis = lpgSatislar,
                    Kaynak = "FATURA"
                });
            }

            var vardiyaHareketleriRaw = xmlVerileri
                .GroupBy(t => t.VardiyaId)
                .OrderByDescending(g => g.First().Vardiya?.BaslangicTarihi)
                .ToList();

            var vardiyaHareketleri = new List<dynamic>();
            foreach (var g in vardiyaHareketleriRaw)
            {
                var vardiyaId = g.Key;
                var tanklar = new List<dynamic>();

                // Bu vardiyadaki tüm satışları (Otomasyon + Filo) önceden gruplayalım
                var vardiyaSatislari = tumSatislar.Where(s => s.VardiyaId == vardiyaId).ToList();
                var yakitBazliSatislar = new Dictionary<int, decimal>();
                
                foreach (var s in vardiyaSatislari)
                {
                    var identified = await _yakitService.IdentifyYakitAsync(s.YakitTuru);
                    if (identified != null)
                    {
                        if (!yakitBazliSatislar.ContainsKey(identified.Id)) yakitBazliSatislar[identified.Id] = 0;
                        yakitBazliSatislar[identified.Id] += s.Litre;
                    }
                }

                foreach (var t in g)
                {
                    var yakit = await _yakitService.IdentifyYakitAsync(t.YakitTipi);
                    
                    // Eğer bu yakıt tipinde birden fazla tank varsa, satışları tanklara nasıl böleceğiz?
                    // Şimdilik basitleştirmek için: Eğer yakıt tipi için toplam satış varsa ve bu tank o yakıt tipindeyse,
                    // fiziksel delta (t.SatilanMiktar) yerine sistem satışını göstermek kafa karıştırabilir.
                    // En iyisi: SatılanMiktar olarak sistem satışını (Otomasyon+Filo) gösterelim.
                    // Eğer birden fazla tank varsa, sistem satışını tankların fiziksel deltalarına göre oranlayalım.
                    
                    decimal sistemSatisi = 0;
                    if (yakit != null && yakitBazliSatislar.ContainsKey(yakit.Id))
                    {
                        var toplamFizikselDelta = g.Where(x => x.YakitTipi == t.YakitTipi).Sum(x => x.SatilanMiktar);
                        if (toplamFizikselDelta > 0)
                        {
                            sistemSatisi = (t.SatilanMiktar / toplamFizikselDelta) * yakitBazliSatislar[yakit.Id];
                        }
                        else
                        {
                            // Fiziksel delta yoksa ama satış varsa (garip durum), ilk tanka yazalım
                            if (t == g.First(x => x.YakitTipi == t.YakitTipi))
                                sistemSatisi = yakitBazliSatislar[yakit.Id];
                        }
                    }

                    // Fark = (Başlangıç + Sevkiyat - Bitiş) - Sistem Satışı
                    decimal fizikselEksilme = t.BaslangicStok + t.SevkiyatMiktar - t.BitisStok;
                    decimal fark = fizikselEksilme - sistemSatisi;

                    tanklar.Add(new
                    {
                        t.TankNo,
                        t.TankAdi,
                        t.YakitTipi,
                        Renk = yakit?.Renk ?? "#666",
                        t.BaslangicStok,
                        t.BitisStok,
                        t.SevkiyatMiktar,
                        SatilanMiktar = sistemSatisi,
                        FarkMiktar = fark
                    });
                }

                // LPG Ekle (Eğer bu vardiyada LPG satışı varsa ve tanklarda yoksa)
                var lpgSatis = lpgSalesPerVardiya.FirstOrDefault(x => x.VardiyaId == vardiyaId);
                if (lpgSatis != null && !tanklar.Any(x => ((string)x.YakitTipi).ToUpper().Contains("LPG")))
                {
                    var lpgYakit = tumYakitlar.FirstOrDefault(y => y.Ad.ToUpper().Contains("LPG") || y.Ad.ToUpper().Contains("OTOGAZ"));
                    tanklar.Add(new
                    {
                        TankNo = 99, // Sanal tank no
                        TankAdi = "LPG Tank",
                        YakitTipi = "LPG",
                        Renk = lpgYakit?.Renk ?? "#3b82f6",
                        BaslangicStok = 0, // Manuel takip edildiği için 0
                        BitisStok = 0,
                        SevkiyatMiktar = 0,
                        SatilanMiktar = lpgSatis.Litre,
                        FarkMiktar = 0
                    });
                }

                vardiyaHareketleri.Add(new
                {
                    VardiyaId = vardiyaId,
                    Tarih = g.First().Vardiya?.BaslangicTarihi,
                    Tanklar = tanklar
                });
            }

            return Ok(new
            {
                XmlKaynakli = xmlOzetFinal,
                ManuelKaynakli = manuelOzet,
                VardiyaHareketleri = vardiyaHareketleri,
                Donem = new { Yil = yil, Ay = ay },
                Ozet = new
                {
                    ToplamMotorinStok = xmlOzetFinal.Cast<dynamic>().FirstOrDefault(x => ((string)x.YakitTipi).ToUpper().Contains("MOTORIN"))?.SonStok ?? 0,
                    ToplamBenzinStok = xmlOzetFinal.Cast<dynamic>().FirstOrDefault(x => ((string)x.YakitTipi).ToUpper().Contains("BENZIN") || ((string)x.YakitTipi).ToUpper().Contains("KURŞUNSUZ"))?.SonStok ?? 0,
                    MotorinSevkiyat = xmlOzetFinal.Cast<dynamic>().FirstOrDefault(x => ((string)x.YakitTipi).ToUpper().Contains("MOTORIN"))?.ToplamSevkiyat ?? 0,
                    BenzinSevkiyat = xmlOzetFinal.Cast<dynamic>().FirstOrDefault(x => ((string)x.YakitTipi).ToUpper().Contains("BENZIN") || ((string)x.YakitTipi).ToUpper().Contains("KURŞUNSUZ"))?.ToplamSevkiyat ?? 0,
                    MotorinSatis = xmlOzetFinal.Cast<dynamic>().FirstOrDefault(x => ((string)x.YakitTipi).ToUpper().Contains("MOTORIN"))?.ToplamSatis ?? 0,
                    BenzinSatis = xmlOzetFinal.Cast<dynamic>().FirstOrDefault(x => ((string)x.YakitTipi).ToUpper().Contains("BENZIN") || ((string)x.YakitTipi).ToUpper().Contains("KURŞUNSUZ"))?.ToplamSatis ?? 0
                }
            });
        }

        private string NormalizeYakitTipi(string yakitTipi)
        {
            if (string.IsNullOrEmpty(yakitTipi)) return "DİĞER";
            var yakit = _yakitService.IdentifyYakitAsync(yakitTipi).GetAwaiter().GetResult();
            return yakit?.Ad ?? yakitTipi.ToUpper();
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
