using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/approvals/vardiya")]
    [Authorize]
    public class VardiyaApprovalController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IVardiyaService _vardiyaService;
        private readonly VardiyaArsivService _arsivService;

        public VardiyaApprovalController(
            AppDbContext context, 
            IVardiyaService vardiyaService,
            VardiyaArsivService arsivService)
        {
            _context = context;
            _vardiyaService = vardiyaService;
            _arsivService = arsivService;
        }

        [HttpGet("onay-bekleyenler")]
        [Authorize(Roles = "admin,patron,vardiya sorumlusu,istasyon sorumlusu,market sorumlusu")]
        public async Task<IActionResult> GetOnayBekleyenler()
        {
            IQueryable<Vardiya> query = _context.Vardiyalar
                .Include(v => v.Istasyon)
                .Where(v => v.Durum == VardiyaDurum.ONAY_BEKLIYOR || v.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR);

            if (IsPatron)
            {
                query = query.Where(v => v.Istasyon != null && v.Istasyon.Firma != null && v.Istasyon.Firma.PatronId == CurrentUserId);
            }
            else if (CurrentIstasyonId.HasValue && !IsAdmin)
            {
                query = query.Where(v => v.IstasyonId == CurrentIstasyonId.Value);
            }

            var list = await query.OrderByDescending(v => v.BaslangicTarihi).ToListAsync();
            return Ok(list);
        }

        [HttpPost("{id}/onaya-gonder")]
        public async Task<IActionResult> OnayaGonder(int id)
        {
            await _vardiyaService.OnayaGonderAsync(id, CurrentUserId, CurrentUserRole);
            return Ok(new { message = "Vardiya onaya gönderildi." });
        }

        [HttpPost("{id}/silme-talebi")]
        public async Task<IActionResult> SilmeTalebi(int id, [FromBody] SilmeTalebiDto dto)
        {
            await _vardiyaService.SilmeTalebiOlusturAsync(id, dto, CurrentUserId, CurrentUserRole, User.Identity?.Name ?? "Unknown");
            return Ok(new { message = "Vardiya silme onayına gönderildi." });
        }

        [HttpPost("{id}/onayla")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> Onayla(int id, [FromBody] OnayDto dto)
        {
            await _vardiyaService.OnaylaAsync(id, dto, CurrentUserId, CurrentUserRole);
            return Ok(new { message = "Vardiya işlemi onaylandı." });
        }

        [HttpPost("{id}/reddet")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> Reddet(int id, [FromBody] RedDto dto)
        {
            await _vardiyaService.ReddetAsync(id, dto, CurrentUserId, CurrentUserRole);
            return Ok(new { message = "Vardiya işlemi reddedildi." });
        }

        /// <summary>
        /// Onaylanmış bir vardiyayı geri alır. Veriler silinmediği için XML'den geri yüklenir.
        /// Sadece admin kullanabilir.
        /// </summary>
        [HttpPost("{id}/onay-kaldir")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> OnayKaldir(int id)
        {
            try
            {
                var result = await _arsivService.OnayiKaldirVeGeriYukle(id, CurrentUserId, User.Identity?.Name ?? "Admin");
                
                if (result)
                {
                    // True döndüyse: Durum 'Onay Bekliyor' oldu, verileri geri yüklemeye çalış
                    await _vardiyaService.RestoreVardiyaDataAsync(id, CurrentUserId, CurrentUserRole);
                    return Ok(new { message = "Vardiya onayı kaldırıldı. Vardiya tekrar onay bekliyor durumuna alındı ve veriler geri yüklendi." });
                }
                else
                {
                    // False döndüyse: XML yok/Veri yok hatası ALINMADI, ama durum değişmedi.
                    // Demek ki "In-Place Fix" (Yerinde Düzeltme) yapıldı.
                    return Ok(new { message = "Yedek veri bulunamadığı için onay kaldırılamadı, ANCAK rapor toplamlarındaki hesaplama hatası arşiv üzerinde düzeltildi." });
                }
            }
            catch (InvalidOperationException ex)
            {
                 // Catch known business logic errors
                 return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Onay kaldırılırken beklenmeyen bir hata oluştu.", error = ex.Message });
            }
        }

        [HttpGet("{id}/onay-detay")]
        public async Task<IActionResult> GetOnayDetay(int id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"🔍 GetOnayDetay başladı (Moved), ID: {id}");

            try
            {
                // Use the Centralized Service Method
                // This ensures M-ODEM fallback and Paro Puan calculations are included
                var data = await _vardiyaService.CalculateVardiyaFinancials(id);

                // Security Check
                if (!IsAdmin)
                {
                    // Extract IstasyonId safely from dynamic object BEFORE usage in EF expression
                    var vDyn = (dynamic)data.Vardiya;
                    int vIstasyonId = (int)vDyn.IstasyonId;

                    if (IsPatron)
                    {
                        var station = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == vIstasyonId);
                        if (station?.Firma?.PatronId != CurrentUserId) return Forbid();
                    }
                    else
                    {
                        if (vIstasyonId != CurrentIstasyonId) return Forbid();
                    }
                }

                // --- MAP TO FRONTEND RESPONSE FORMAT ---
                
                // 1. Calculate Diger Odemeler Ozet (Grouped by Type)
                var digerOdemelerOzet = data.Pusulalar
                    .SelectMany(p => p.DigerOdemeler)
                    .GroupBy(d => new { d.TurKodu, d.TurAdi })
                    .Select(g => new
                    {
                        TurKodu = g.Key.TurKodu,
                        TurAdi = g.Key.TurAdi,
                        Toplam = g.Sum(d => d.Tutar)
                    })
                    .ToList();

                // Veresiyeleri de 'Diğer Ödemeler' listesine ekle (Kullanıcı talebi: Firma bazlı)
                var veresiyeOzet = data.Pusulalar
                    .SelectMany(p => p.Veresiyeler)
                    .GroupBy(v => v.CariAd)
                    .Select(g => new
                    {
                        TurKodu = "VERESIYE",
                        TurAdi = $"Cari / {g.Key}",
                        Toplam = g.Sum(v => v.Tutar)
                    }).ToList();

                foreach (var v in veresiyeOzet)
                {
                    digerOdemelerOzet.Add(v);
                }

                var digerOdemelerToplam = digerOdemelerOzet.Sum(x => x.Toplam);
               
                // 2. Prepare Fark Analizi
                var farkAnalizi = new List<object>();

                // Add Personel Analysis
                foreach (var personel in data.PersonelOzetler.Where(p => p.PersonelAdi != "FİLO SATIŞLARI"))
                {
                    var pusula = data.Pusulalar.FirstOrDefault(p => p.PersonelAdi == personel.PersonelAdi);
                    var pusulaTutar = pusula?.Toplam ?? 0;
                    var fark = pusulaTutar - personel.ToplamTutar;

                    farkAnalizi.Add(new
                    {
                        PersonelId = personel.PersonelId,
                        PersonelAdi = personel.PersonelAdi,
                        OtomasyonToplam = personel.ToplamTutar,
                        PusulaToplam = pusulaTutar,
                        Fark = fark,
                        FarkDurum = Math.Abs(fark) < 1 ? "UYUMLU" : (fark < 0 ? "ACIK" : "FAZLA"),
                        PusulaDokum = pusula != null ? new
                        {
                            Nakit = pusula.Nakit,
                            KrediKarti = pusula.KrediKarti,
                            KrediKartiDetay = pusula.KrediKartiDetay, 
                            DigerOdemeler = pusula.DigerOdemeler.Select(d => new { 
                                turKodu = d.TurKodu, 
                                turAdi = d.TurAdi, 
                                tutar = d.Tutar 
                            }).Concat(pusula.Veresiyeler.Select(v => new {
                                turKodu = "VERESIYE",
                                turAdi = $"Cari / {v.CariAd}",
                                tutar = v.Tutar
                            })).ToList()
                        } : null
                    });
                }

                // Add Filo if exists
                if (data.FiloOzet != null && data.FiloOzet.ToplamTutar > 0)
                {
                     farkAnalizi.Add(new
                    {
                        PersonelId = -1,
                        PersonelAdi = "FİLO SATIŞLARI",
                        OtomasyonToplam = data.FiloOzet.ToplamTutar,
                        PusulaToplam = data.FiloOzet.ToplamTutar,
                        Fark = 0m,
                        FarkDurum = "UYUMLU",
                        PusulaDokum = new
                        {
                            Nakit = 0m,
                            KrediKarti = 0m,
                            DigerOdemeler = new List<object>()
                        }
                    });
                }

                // 3. Flatten Credit Card Details for Summary
                var krediKartiDetaylariList = new List<object>(); // Assuming details logic is mostly for debugging or simple counts?
                // Re-implementation of the JSON parsing logic from original controller:
                var bankaDict = new Dictionary<string, decimal>();
                 foreach(var p in data.Pusulalar)
                 {
                      if (!string.IsNullOrEmpty(p.KrediKartiDetay)) {
                           try {
                                using var doc = System.Text.Json.JsonDocument.Parse(p.KrediKartiDetay);
                                foreach(var el in doc.RootElement.EnumerateArray()) {
                                    var banka = el.TryGetProperty("banka", out var b) ? b.GetString() ?? "Diğer" : "Diğer";
                                    var tutar = el.TryGetProperty("tutar", out var t) ? (t.ValueKind == System.Text.Json.JsonValueKind.Number ? t.GetDecimal() : 0) : 0;
                                    if(!bankaDict.ContainsKey(banka)) bankaDict[banka] = 0;
                                    bankaDict[banka] += tutar;
                                }
                           } catch {}
                      }
                      
                      // Add detaysiz if any
                      if (p.KrediKarti > 0 && string.IsNullOrEmpty(p.KrediKartiDetay)) {
                           if(!bankaDict.ContainsKey("Genel / Detaysız")) bankaDict["Genel / Detaysız"] = 0;
                           bankaDict["Genel / Detaysız"] += p.KrediKarti;
                      }
                 }
                 
                  foreach(var doz in digerOdemelerOzet) {
                      var key = doz.TurAdi ?? doz.TurKodu ?? "Diğer";
                      if(!bankaDict.ContainsKey(key)) bankaDict[key] = 0;
                      bankaDict[key] += doz.Toplam;
                  }

                  // Veresiyeleri de detay listesine ekle
                  foreach (var p in data.Pusulalar)
                  {
                      foreach (var v in p.Veresiyeler)
                      {
                          var key = $"Cari / {v.CariAd}";
                          if (!bankaDict.ContainsKey(key)) bankaDict[key] = 0;
                          bankaDict[key] += v.Tutar;
                      }
                  }

                  var finalBankaDetaylari = bankaDict.Select(kvp => new { Banka = kvp.Key, Tutar = kvp.Value })
                                             .OrderByDescending(x => x.Tutar).ToList();

                // Prepare dynamic access for mapping
                var vMapping = (dynamic)data.Vardiya;
                decimal pompaToplam = (decimal)vMapping.PompaToplam;
                decimal marketToplam = (decimal)vMapping.MarketToplam;
                decimal genelToplam = (decimal)vMapping.GenelToplam;

                // 4. Construct Final Response
                var genOzet = new
                {
                    PompaToplam = pompaToplam,
                    MarketToplam = marketToplam,
                    GenelToplam = genelToplam,
                    ToplamNakit = data.GenelOzet.ToplamNakit,
                    ToplamKrediKarti = data.GenelOzet.ToplamKrediKarti,
                    DigerOdemeler = digerOdemelerOzet,
                    FiloToplam = data.FiloOzet?.ToplamTutar ?? 0,
                    ToplamVeresiye = data.Pusulalar.Sum(p => p.Veresiyeler.Sum(v => v.Tutar)), 
                    ToplamFark = data.GenelOzet.Fark,
                    DurumRenk = Math.Abs(data.GenelOzet.Fark) < 10 ? "success" : (data.GenelOzet.Fark < 0 ? "danger" : "warn")
                };

                stopwatch.Stop();
                Console.WriteLine($"✅ GetOnayDetay tamamlandı: {stopwatch.ElapsedMilliseconds}ms");

                return Ok(new
                {
                    Vardiya = data.Vardiya,
                    GenelOzet = genOzet,
                    FarkAnalizi = farkAnalizi,
                    Pusulalar = data.Pusulalar,
                    KrediKartiDetaylari = finalBankaDetaylari,
                    PersonelSayisi = farkAnalizi.Count,
                    _performanceMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetOnayDetay HATA: {ex}");
                return StatusCode(500, new { message = "Veri çekilemedi", error = ex.Message });
            }
        }
    }
}
