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

            // 3. Aggregate Sales from Archive (JSON)
            // Sadece ONAYLI vardiyaların arşivlerini çek
            var arsivlerBefore = await _context.VardiyaRaporArsivleri
                .Where(a => a.Tarih < startDate)
                .Select(a => new { a.PersonelSatisDetayJson, a.FiloSatisDetayJson })
                .ToListAsync();

            var arsivlerThisMonth = await _context.VardiyaRaporArsivleri
                .Where(a => a.Tarih >= startDate && a.Tarih < nextMonth)
                .Select(a => new { a.PersonelSatisDetayJson, a.FiloSatisDetayJson })
                .ToListAsync();

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Helper to aggregate sales from JSON list
            List<(string YakitTuru, decimal Litre)> AggregateSales(IEnumerable<dynamic> arsivler)
            {
                var result = new List<(string YakitTuru, decimal Litre)>();
                
                foreach (var arsiv in arsivler)
                {
                    // Personel Satışları
                    if (!string.IsNullOrEmpty(arsiv.PersonelSatisDetayJson))
                    {
                        try
                        {
                            var personelDetay = System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDemo.Api.Services.PersonelSatisDetayDto>>(arsiv.PersonelSatisDetayJson, jsonOptions);
                            if (personelDetay != null)
                            {
                                foreach (var p in personelDetay)
                                {
                                    foreach (var s in p.Satislar)
                                    {
                                        result.Add((s.YakitTuru, s.Litre));
                                    }
                                }
                            }
                        }
                        catch { /* Ignore parse errors */ }
                    }

                    // Filo Satışları
                    if (!string.IsNullOrEmpty(arsiv.FiloSatisDetayJson))
                    {
                        try
                        {
                            var filoDetay = System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDemo.Api.Services.FiloSatisDetayDto>>(arsiv.FiloSatisDetayJson, jsonOptions);
                            if (filoDetay != null)
                            {
                                foreach (var f in filoDetay)
                                {
                                    result.Add((f.YakitTuru, f.Litre));
                                }
                            }
                        }
                        catch { /* Ignore parse errors */ }
                    }
                }
                return result;
            }

            var allSalesBefore = AggregateSales(arsivlerBefore);
            var allSalesThisMonth = AggregateSales(arsivlerThisMonth);

            var salesBeforeGrouped = allSalesBefore
                .GroupBy(x => x.YakitTuru)
                .Select(g => new { YakitTuru = g.Key, Total = g.Sum(x => x.Litre) })
                .ToList();

            var salesThisMonthGrouped = allSalesThisMonth
                .GroupBy(x => x.YakitTuru)
                .Select(g => new { YakitTuru = g.Key, Total = g.Sum(x => x.Litre) })
                .ToList();

            var ozetList = new List<IstasyonDemo.Api.Dtos.TankStokOzetDto>();

            foreach (var yakit in yakitlar)
            {
                // Match Inputs
                var inBefore = inputsBefore.FirstOrDefault(x => x.YakitId == yakit.Id)?.Total ?? 0;
                var inMonth = inputsThisMonth.FirstOrDefault(x => x.YakitId == yakit.Id)?.Total ?? 0;

                // Match Sales
                var keywords = (yakit.OtomasyonUrunAdi ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim().ToUpper())
                    .ToList();

                bool Matches(string saleYakitTuruStr) 
                {
                    if (string.IsNullOrEmpty(saleYakitTuruStr)) return false;
                    
                    string normalizedValue = saleYakitTuruStr;
                    if (int.TryParse(saleYakitTuruStr, out var intValue) && Enum.IsDefined(typeof(YakitTuru), intValue))
                    {
                        normalizedValue = ((YakitTuru)intValue).ToString();
                    }
                    
                    var u = normalizedValue.ToUpper();
                    return keywords.Any(k => u.Contains(k));
                }

                var outBefore = salesBeforeGrouped
                    .Where(x => Matches(x.YakitTuru))
                    .Sum(x => x.Total);
                
                var outMonth = salesThisMonthGrouped
                    .Where(x => Matches(x.YakitTuru))
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

            return Ok(new {
                Ozet = ozetList,
                Debug = new {
                    Aciklama = "Veriler artık VardiyaRaporArsivleri tablosundan (JSON) okunmaktadır."
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

            // 1. Arşivleri Çek (JSON)
            var query = _context.VardiyaRaporArsivleri
                .Where(a => a.Tarih >= startDate && a.Tarih < endDate);

            // İstasyon filtresi için Vardiya tablosuna join atmamız gerekebilir ama VardiyaRaporArsiv'de IstasyonId yok.
            // VardiyaId üzerinden Vardiya tablosuna join atalım.
            if (istasyonId.HasValue)
            {
                // Bu join performansı biraz etkileyebilir ama gerekli.
                // Alternatif: VardiyaRaporArsiv tablosuna IstasyonId eklemek.
                // Şimdilik Vardiya üzerinden gidelim.
                var vardiyaIds = await _context.Vardiyalar
                    .Where(v => v.IstasyonId == istasyonId.Value)
                    .Select(v => v.Id)
                    .ToListAsync();
                
                query = query.Where(a => vardiyaIds.Contains(a.VardiyaId));
            }

            var arsivler = await query
                .Select(a => new { 
                    a.VardiyaId, 
                    a.Tarih, 
                    a.TankEnvanterJson, 
                    a.PersonelSatisDetayJson, 
                    a.FiloSatisDetayJson 
                })
                .ToListAsync();

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 2. Verileri Parse Et
            var tankEnvanterleri = new List<VardiyaTankEnvanteri>();
            var tumSatislar = new List<(int VardiyaId, string YakitTuru, decimal Litre)>();

            foreach (var arsiv in arsivler)
            {
                // Tank Envanteri
                if (!string.IsNullOrEmpty(arsiv.TankEnvanterJson))
                {
                    try
                    {
                        var tanklar = System.Text.Json.JsonSerializer.Deserialize<List<VardiyaTankEnvanteri>>(arsiv.TankEnvanterJson, jsonOptions);
                        if (tanklar != null)
                        {
                            foreach (var t in tanklar)
                            {
                                t.VardiyaId = arsiv.VardiyaId; // İlişkiyi kur
                                // Tarih bilgisi tank nesnesinde olmayabilir, arşivden alalım
                                t.KayitTarihi = arsiv.Tarih;
                                tankEnvanterleri.Add(t);
                            }
                        }
                    }
                    catch { /* Ignore */ }
                }

                // Personel Satışları
                if (!string.IsNullOrEmpty(arsiv.PersonelSatisDetayJson))
                {
                    try
                    {
                        var personelDetay = System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDemo.Api.Services.PersonelSatisDetayDto>>(arsiv.PersonelSatisDetayJson, jsonOptions);
                        if (personelDetay != null)
                        {
                            foreach (var p in personelDetay)
                            {
                                foreach (var s in p.Satislar)
                                {
                                    tumSatislar.Add((arsiv.VardiyaId, s.YakitTuru, s.Litre));
                                }
                            }
                        }
                    }
                    catch { /* Ignore */ }
                }

                // Filo Satışları
                if (!string.IsNullOrEmpty(arsiv.FiloSatisDetayJson))
                {
                    try
                    {
                        var filoDetay = System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDemo.Api.Services.FiloSatisDetayDto>>(arsiv.FiloSatisDetayJson, jsonOptions);
                        if (filoDetay != null)
                        {
                            foreach (var f in filoDetay)
                            {
                                tumSatislar.Add((arsiv.VardiyaId, f.YakitTuru, f.Litre));
                            }
                        }
                    }
                    catch { /* Ignore */ }
                }
            }

            // 3. XML Kaynaklı (Tank Envanteri) Özetle
            var xmlOzetRaw = tankEnvanterleri
                .GroupBy(t => t.YakitTipi)
                .ToList();

            var xmlOzet = new List<dynamic>();
            foreach (var g in xmlOzetRaw)
            {
                var yakit = await _yakitService.IdentifyYakitAsync(g.Key ?? "");
                var yakitAdi = yakit?.Ad ?? g.Key ?? "Bilinmeyen";

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

                xmlOzet.Add(new
                {
                    YakitTipi = yakitAdi,
                    YakitId = yakit?.Id,
                    Tanimli = yakit != null,
                    Renk = yakit?.Renk ?? "#666",
                    ToplamSevkiyat = toplamSevkiyat,
                    SonStok = toplamSonStok,
                    IlkStok = toplamIlkStok,
                    KayitSayisi = g.Select(t => t.VardiyaId).Distinct().Count()
                });
            }

            // 4. XML Kaynaklı Özetini Tamamla (Satış ve Fark Hesapla)
            // Gün hesabı - Gerçek veri bulunan gün sayısını hesapla
            // Arşivlerdeki distinct tarih sayısı gerçek çalışma günlerini verir
            int daysElapsed = arsivler
                .Select(a => a.Tarih.Date)
                .Distinct()
                .Count();
            
            // Eğer veri yoksa veya 0 ise, minimum 1 gün kabul et
            if (daysElapsed == 0) daysElapsed = 1;

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

                decimal beklenenSatis = item.IlkStok + item.ToplamSevkiyat - item.SonStok;
                decimal fark = beklenenSatis - toplamSatis;

                decimal gunlukOrtalama = daysElapsed > 0 ? toplamSatis / daysElapsed : 0;
                decimal tahminiGun = gunlukOrtalama > 0 ? item.SonStok / gunlukOrtalama : 0;

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
                    item.KayitSayisi,
                    TahminiGun = (int)Math.Floor(tahminiGun)
                });
            }

            // 5. Manuel Kaynaklı (TankGiris) - LPG
            var tumYakitlar = await _yakitService.GetAllYakitlarAsync();
            var lpgYakitlar = await _context.Yakitlar
                .Where(y => y.Ad.ToUpper().Contains("LPG") || y.Ad.ToUpper().Contains("OTOGAZ"))
                .Select(y => y.Id)
                .ToListAsync();

            var lpgGirisler = await _context.TankGirisler
                .Where(t => t.Tarih >= startDate && t.Tarih < endDate)
                .Where(t => lpgYakitlar.Contains(t.YakitId))
                .SumAsync(t => t.Litre);

            var lpgYakitEntity = tumYakitlar.FirstOrDefault(y => y.Ad.ToUpper().Contains("LPG") || y.Ad.ToUpper().Contains("OTOGAZ"));
            
            var lpgSatisGruplari = new Dictionary<int, decimal>();
            if (lpgYakitEntity != null)
            {
                foreach (var s in tumSatislar)
                {
                    var identified = await _yakitService.IdentifyYakitAsync(s.YakitTuru);
                    if (identified != null && identified.Id == lpgYakitEntity.Id)
                    {
                        if (!lpgSatisGruplari.ContainsKey(s.VardiyaId)) lpgSatisGruplari[s.VardiyaId] = 0;
                        lpgSatisGruplari[s.VardiyaId] += s.Litre;
                    }
                }
            }
            var lpgSatislar = lpgSatisGruplari.Values.Sum();

            // LPG Devir ve Son Stok Hesabı
            decimal lpgDevir = 0;
            if (lpgYakitEntity != null)
            {
                var prevMonthDate = startDate.AddMonths(-1);
                // İstasyon filtresi varsa stok özetinde de dikkate almak gerekebilir ama AylikStokOzetleri istasyon bazlı değil şu an (Global kabul ediliyor veya geliştirilmeli)
                // Basitlik için direkt çekiyoruz.
                var prevStock = await _context.AylikStokOzetleri
                    .FirstOrDefaultAsync(x => x.Yil == prevMonthDate.Year && x.Ay == prevMonthDate.Month && x.YakitId == lpgYakitEntity.Id);
                lpgDevir = prevStock?.KalanStok ?? 0;
            }
            decimal lpgSonStok = lpgDevir + lpgGirisler - lpgSatislar;
            decimal lpgGunlukOrtalama = daysElapsed > 0 ? lpgSatislar / daysElapsed : 0;
            decimal lpgTahminiGun = lpgGunlukOrtalama > 0 ? lpgSonStok / lpgGunlukOrtalama : 0;

            var manuelOzet = new List<object>();
            if (lpgYakitlar.Any() || lpgSatislar > 0)
            {
                manuelOzet.Add(new
                {
                    YakitTipi = "LPG",
                    Renk = lpgYakitEntity?.Renk ?? "#3b82f6",
                    ToplamGiris = lpgGirisler,
                    ToplamSatis = lpgSatislar,
                    SonStok = lpgSonStok,
                    TahminiGun = (int)Math.Floor(lpgTahminiGun),
                    Kaynak = "FATURA"
                });
            }

            // 6. Vardiya Hareketleri
            var vardiyaHareketleriRaw = tankEnvanterleri
                .GroupBy(t => t.VardiyaId)
                .OrderByDescending(g => g.First().KayitTarihi)
                .ToList();

            var vardiyaHareketleri = new List<dynamic>();
            foreach (var g in vardiyaHareketleriRaw)
            {
                var vardiyaId = g.Key;
                var tanklar = new List<dynamic>();

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
                            if (t == g.First(x => x.YakitTipi == t.YakitTipi))
                                sistemSatisi = yakitBazliSatislar[yakit.Id];
                        }
                    }

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

                // LPG Ekle
                if (lpgSatisGruplari.ContainsKey(vardiyaId) && !tanklar.Any(x => ((string)x.YakitTipi).ToUpper().Contains("LPG")))
                {
                    tanklar.Add(new
                    {
                        TankNo = 99,
                        TankAdi = "LPG Tank",
                        YakitTipi = "LPG",
                        Renk = lpgYakitEntity?.Renk ?? "#3b82f6",
                        BaslangicStok = 0,
                        BitisStok = 0,
                        SevkiyatMiktar = 0,
                        SatilanMiktar = lpgSatisGruplari[vardiyaId],
                        FarkMiktar = 0
                    });
                }

                vardiyaHareketleri.Add(new
                {
                    VardiyaId = vardiyaId,
                    Tarih = g.First().KayitTarihi,
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
