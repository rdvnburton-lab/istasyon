using System;
using System.Linq;
using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VardiyaController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IVardiyaService _vardiyaService;

        public VardiyaController(AppDbContext context, IVardiyaService vardiyaService)
        {
            _context = context;
            _vardiyaService = vardiyaService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateVardiyaDto dto)
        {
            Console.WriteLine($"Create Vardiya - User Claims: {string.Join(", ", User.Claims.Select(c => c.Type + "=" + c.Value))}");
            
            if (CurrentUserId == 0)
            {
                return BadRequest("Kullanıcı kimliği doğrulanamadı (Token hatası). Lütfen çıkış yapıp tekrar giriş yapın.");
            }

            var userIstasyonId = 0;

            if (!IsAdmin)
            {
                if (!CurrentIstasyonId.HasValue)
                {
                    return BadRequest("Kullanıcının istasyonu tanımlı değil.");
                }
                userIstasyonId = CurrentIstasyonId.Value;
                
                // Force IstasyonId to be user's station
                dto.IstasyonId = userIstasyonId;
            }
            else
            {
                // Admin can specify station, otherwise use default or error
                if (dto.IstasyonId == 0) return BadRequest("İstasyon ID zorunludur.");
            }

            try
            {
                var vardiya = await _vardiyaService.CreateVardiyaAsync(dto, CurrentUserId, CurrentUserRole, User.Identity.Name);
                return CreatedAtAction(nameof(GetById), new { id = vardiya.Id }, vardiya);
            }
            catch (Exception ex)
            {
                // Global Exception Middleware will handle logging, but we can still return specific status codes if needed
                // For now, let's just rethrow or return 500 as before (but cleaner)
                return StatusCode(500, new { message = "Vardiya kaydedilirken bir hata oluştu.", error = ex.Message });
            }
        }

        [HttpPost("upload-xml-zip")]
        [Authorize]
        [DisableRequestSizeLimit] 
        public async Task<IActionResult> UploadXmlZip()
        {
            try
            {
                if (!Request.HasFormContentType)
                {
                    return BadRequest(new { message = "Form verisi bekleniyor (multipart/form-data)." });
                }

                var form = await Request.ReadFormAsync();
                var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

                if (file == null || file.Length == 0)
                {
                     return BadRequest(new { message = "Dosya yüklenmedi." });
                }
                
                if (!Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Sadece .zip dosyaları kabul edilir." });

                using var stream = file.OpenReadStream();
                await _vardiyaService.ProcessXmlZipAsync(stream, file.FileName, CurrentUserId, CurrentUserRole, User.Identity.Name);
                return Ok(new { message = "Dosya başarıyla işlendi ve vardiya oluşturuldu." });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(400, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Dosya işlenirken hata oluştu.", error = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            Console.WriteLine($"🔍 GetById çağrıldı, ID: {id}");
            
            var vardiya = await _context.Vardiyalar
                .AsNoTracking()
                .Where(v => v.Id == id)
                .Select(v => new
                {
                    v.Id,
                    v.IstasyonId,
                    v.BaslangicTarihi,
                    v.BitisTarihi,
                    v.Durum,
                    v.PompaToplam,
                    v.MarketToplam,
                    v.GenelToplam,
                    v.OlusturmaTarihi,
                    v.GuncellemeTarihi,
                    v.DosyaAdi,
                    v.RedNedeni,
                    OtomasyonSatislar = _context.OtomasyonSatislar
                        .Where(s => s.VardiyaId == id)
                        .Select(s => new
                        {
                            s.Id,
                            s.PersonelAdi,
                            s.PersonelKeyId,
                            s.PersonelId,
                            s.PompaNo,
                            s.YakitTuru,
                            s.Litre,
                            s.BirimFiyat,
                            s.ToplamTutar,
                            s.SatisTarihi,
                            s.FisNo,
                            s.Plaka
                        }).ToList(),
                    FiloSatislar = _context.FiloSatislar
                        .Where(f => f.VardiyaId == id)
                        .Select(f => new
                        {
                            f.Id,
                            f.Tarih,
                            f.FiloKodu,
                            f.Plaka,
                            f.YakitTuru,
                            f.Litre,
                            f.Tutar,
                            f.PompaNo,
                            f.FisNo,
                            f.FiloAdi
                        }).ToList(),
                    Pusulalar = _context.Pusulalar
                        .Where(p => p.VardiyaId == id)
                        .Select(p => new
                        {
                            p.Id,
                            p.PersonelAdi,
                            p.PersonelId,
                            p.Nakit,
                            p.KrediKarti,
                            p.KrediKartiDetay,
                            p.Aciklama,
                            Veresiyeler = p.Veresiyeler.Select(v => new {
                                v.Id,
                                v.CariKartId,
                                v.Plaka,
                                v.Litre,
                                v.Tutar,
                                v.Aciklama,
                                CariKart = new { v.CariKart.Ad }
                            }).ToList(),
                            Toplam = p.Nakit + p.KrediKarti + p.DigerOdemeler.Sum(d => d.Tutar) + p.Veresiyeler.Sum(v => v.Tutar)
                        }).ToList(),
                    TankEnvanterleri = _context.VardiyaTankEnvanterleri
                        .Where(t => t.VardiyaId == id)
                        .OrderBy(t => t.TankNo)
                        .Select(t => new
                        {
                            t.TankNo,
                            t.TankAdi,
                            t.YakitTipi,
                            t.BaslangicStok,
                            t.BitisStok,
                            t.SatilanMiktar,
                            t.SevkiyatMiktar,
                            t.BeklenenTuketim,
                            t.FarkMiktar
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (vardiya == null)
            {
                Console.WriteLine($"❌ Vardiya bulunamadı, ID: {id}");
                return NotFound();
            }

            // Security Check
            if (!IsAdmin)
            {
                if (IsPatron)
                {
                    // Check if station belongs to patron's company
                    // This requires fetching station info which is not in the projection above easily unless we include it or check separately
                    // For performance, let's assume if they have the ID they might access, OR better check:
                    var station = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == vardiya.IstasyonId);
                    if (station?.Firma?.PatronId != CurrentUserId) return Forbid();
                }
                else
                {
                    if (vardiya.IstasyonId != CurrentIstasyonId) return Forbid();
                }
            }

            Console.WriteLine($"✅ Vardiya bulundu: {vardiya.DosyaAdi}, Satış: {vardiya.OtomasyonSatislar?.Count ?? 0}");
            return Ok(vardiya);
        }

        [HttpGet("{id}/dosya")]
        public async Task<IActionResult> DownloadDosya(int id)
        {
            Console.WriteLine($"[DownloadDosya] Request for ID: {id}");
            var vardiya = await _context.Vardiyalar.FindAsync(id);
            if (vardiya == null)
            {
                Console.WriteLine($"[DownloadDosya] Vardiya not found for ID: {id}");
                return NotFound("Vardiya bulunamadı.");
            }

            byte[]? dosyaIcerik = vardiya.DosyaIcerik;
            string dosyaAdi = vardiya.DosyaAdi ?? "vardiya.txt";
            Console.WriteLine($"[DownloadDosya] Initial check - Name: {dosyaAdi}, ContentLength: {dosyaIcerik?.Length ?? 0}");

            // If DosyaIcerik is empty, try to fetch from VardiyaXmlLog
            if (dosyaIcerik == null || dosyaIcerik.Length == 0)
            {
                Console.WriteLine($"[DownloadDosya] Content empty, checking XmlLog for VardiyaId: {id}");
                var xmlLog = await _context.VardiyaXmlLoglari.FirstOrDefaultAsync(x => x.VardiyaId == id);
                if (xmlLog != null)
                {
                    Console.WriteLine($"[DownloadDosya] XmlLog found. ZipLength: {xmlLog.ZipDosyasi?.Length ?? 0}");
                    if (xmlLog.ZipDosyasi != null && xmlLog.ZipDosyasi.Length > 0)
                    {
                        dosyaIcerik = xmlLog.ZipDosyasi;
                        if (!string.IsNullOrEmpty(xmlLog.DosyaAdi))
                        {
                            dosyaAdi = xmlLog.DosyaAdi;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[DownloadDosya] XmlLog NOT found for VardiyaId: {id}");
                }
            }

            if (dosyaIcerik == null || dosyaIcerik.Length == 0)
            {
                Console.WriteLine($"[DownloadDosya] Final content is empty. Returning NotFound.");
                return NotFound("Dosya içeriği bulunamadı.");
            }

            string contentType = "text/plain";

            if (dosyaAdi.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "application/zip";
            }
            else if (dosyaAdi.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "text/xml";
            }

            Console.WriteLine($"[DownloadDosya] Returning File. Length: {dosyaIcerik.Length}, Type: {contentType}, Name: {dosyaAdi}");
            return File(dosyaIcerik, contentType, dosyaAdi);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            IQueryable<Vardiya> query = _context.Vardiyalar.AsNoTracking();

            if (IsAdmin)
            {
                // Admin sees all
            }
            else if (IsPatron)
            {
                query = query.Where(v => v.Istasyon != null && v.Istasyon.Firma != null && v.Istasyon.Firma.PatronId == CurrentUserId);
            }
            else if (CurrentUserRole == "market sorumlusu")
            {
                // Market sorumlusu should not see pump shifts
                return Ok(new { Items = new List<object>(), Summary = new { ToplamCiro = 0, ToplamIslem = 0, BenzersizPersonelSayisi = 0 } });
            }
            else
            {
                if (CurrentIstasyonId != null)
                {
                    query = query.Where(v => v.IstasyonId == CurrentIstasyonId);
                }
                else
                {
                    return Ok(new { Items = new List<object>(), Summary = new { ToplamCiro = 0, ToplamIslem = 0, BenzersizPersonelSayisi = 0 } });
                }
            }

            // Silinmiş vardiyaları listeden çıkar
            query = query.Where(v => v.Durum != VardiyaDurum.SILINDI);

            // OPTIMIZED: Removed Include() to prevent loading all sales data
            // Now returns only summary counts for faster response
            var items = await query
                .OrderByDescending(v => v.BaslangicTarihi)
                .Select(v => new
                {
                    v.Id,
                    v.IstasyonId,
                    v.BaslangicTarihi,
                    v.BitisTarihi,
                    v.Durum,
                    v.PompaToplam,
                    v.MarketToplam,
                    v.GenelToplam,
                    v.OlusturmaTarihi,
                    v.GuncellemeTarihi,
                    v.DosyaAdi,
                    v.RedNedeni,
                    v.OnaylayanAdi,
                    v.OnayTarihi,
                    // Return counts instead of full objects
                    PersonelSayisi = _context.OtomasyonSatislar
                        .Where(s => s.VardiyaId == v.Id)
                        .Select(s => s.PersonelAdi)
                        .Distinct()
                        .Count(),
                    IslemSayisi = _context.OtomasyonSatislar.Count(s => s.VardiyaId == v.Id) + 
                                 _context.FiloSatislar.Count(f => f.VardiyaId == v.Id),
                    PusulaSayisi = _context.Pusulalar.Count(p => p.VardiyaId == v.Id)
                })
                .ToListAsync();

            // Global Summary
            var summary = new
            {
                ToplamCiro = await query.SumAsync(v => v.GenelToplam),
                ToplamIslem = await query.SelectMany(v => v.OtomasyonSatislar).CountAsync() + await query.SelectMany(v => v.FiloSatislar).CountAsync(),
                BenzersizPersonelSayisi = await query.SelectMany(v => v.OtomasyonSatislar).Select(s => s.PersonelAdi).Distinct().CountAsync()
            };

            return Ok(new { Items = items, Summary = summary });
        }


        [HttpGet("{id}/tank-envanter")]
        [Authorize]
        public async Task<IActionResult> GetTankEnvanter(int id)
        {
            var envanter = await _context.VardiyaTankEnvanterleri
                .Where(t => t.VardiyaId == id)
                .OrderBy(t => t.TankNo)
                .ToListAsync();
            
            return Ok(envanter);
        }

        /// <summary>
        /// OPTIMIZED endpoint for Pompa Mutabakatı page
        /// Returns pre-aggregated data by personnel (GROUP BY at database level)
        /// Includes centralized M-ODEM reconciliation logic
        /// </summary>
        [HttpGet("{id}/mutabakat")]
        public async Task<IActionResult> GetMutabakat(int id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"🚀 GetMutabakat başladı, ID: {id}");

            // Security Check for Mutabakat
            if (!IsAdmin)
            {
                 // Fetch minimal info to check ownership (optimize this later)
                 var v = await _context.Vardiyalar.Include(x => x.Istasyon).ThenInclude(x => x!.Firma).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                 if (v == null) return NotFound();

                if (IsPatron)
                {
                    if (v.Istasyon == null || v.Istasyon.Firma == null || v.Istasyon.Firma.PatronId != CurrentUserId) return Forbid();
                }
                else
                {
                    if (v.IstasyonId != CurrentIstasyonId) return Forbid();
                }
            }

            try
            {
                // CENTRALIZED LOGIC CALL
                var result = await _vardiyaService.CalculateVardiyaFinancials(id);
                
                stopwatch.Stop();
                Console.WriteLine($"✅ GetMutabakat tamamlandı: {stopwatch.ElapsedMilliseconds}ms toplam. Fark: {result.GenelOzet.Fark}");
                
                // Add performance metric
                result._performanceMs = stopwatch.ElapsedMilliseconds;

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }


        [HttpGet("loglar")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> GetVardiyaLoglari([FromQuery] int? vardiyaId, [FromQuery] int? limit = 100)
        {
            IQueryable<VardiyaLog> query = _context.VardiyaLoglari
                .Include(vl => vl.Vardiya)
                    .ThenInclude(v => v!.Istasyon).ThenInclude(i => i!.Firma);

            // Patron sadece kendi istasyonlarının loglarını görebilir
            if (IsPatron)
            {
                query = query.Where(vl => vl.Vardiya != null && vl.Vardiya.Istasyon != null && vl.Vardiya.Istasyon.Firma != null && vl.Vardiya.Istasyon.Firma.PatronId == CurrentUserId);
            }

            // Belirli bir vardiya için filtreleme
            if (vardiyaId.HasValue)
            {
                query = query.Where(vl => vl.VardiyaId == vardiyaId.Value);
            }

            var loglar = await query
                .OrderByDescending(vl => vl.IslemTarihi)
                .Take(limit ?? 100)
                .Select(vl => new
                {
                    vl.Id,
                    vl.VardiyaId,
                    VardiyaDosyaAdi = vl.Vardiya != null ? vl.Vardiya.DosyaAdi : "Bilinmeyen Vardiya",
                    IstasyonAdi = vl.Vardiya != null && vl.Vardiya.Istasyon != null ? vl.Vardiya.Istasyon.Ad : "Bilinmeyen İstasyon",
                    vl.Islem,
                    vl.Aciklama,
                    vl.KullaniciId,
                    vl.KullaniciAdi,
                    vl.KullaniciRol,
                    vl.IslemTarihi,
                    vl.EskiDurum,
                    vl.YeniDurum
                })
                .ToListAsync();

            return Ok(loglar);
        }
    }
}
