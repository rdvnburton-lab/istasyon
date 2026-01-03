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
                            Toplam = p.Nakit + p.KrediKarti + p.DigerOdemeler.Sum(d => d.Tutar)
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
            var vardiya = await _context.Vardiyalar.FindAsync(id);
            if (vardiya == null || vardiya.DosyaIcerik == null)
            {
                return NotFound("Dosya bulunamadı.");
            }

            return File(vardiya.DosyaIcerik, "text/plain", vardiya.DosyaAdi ?? "vardiya.txt");
        }

        [HttpGet("rapor")]
        public async Task<IActionResult> GetVardiyaRaporu([FromQuery] DateTimeOffset baslangic, [FromQuery] DateTimeOffset bitis)
        {
            var start = baslangic.UtcDateTime;
            var end = bitis.UtcDateTime;

            var query = _context.Vardiyalar.AsQueryable();

            if (IsAdmin) { }
            else if (IsPatron)
            {
                query = query.Where(v => v.Istasyon != null && v.Istasyon.Firma != null && v.Istasyon.Firma.PatronId == CurrentUserId);
            }
            else if (CurrentUserRole == "market_sorumlusu")
            {
                return Ok(new VardiyaRaporuDto { Ozet = new VardiyaRaporOzetDto(), Vardiyalar = new List<VardiyaRaporItemDto>() });
            }
            else
            {
                if (CurrentIstasyonId != null)
                {
                    query = query.Where(v => v.IstasyonId == CurrentIstasyonId);
                }
                else return Unauthorized();
            }

            query = query.Where(v => v.BaslangicTarihi >= start && v.BaslangicTarihi <= end && v.Durum != VardiyaDurum.SILINDI);

            var vardiyalar = await query
                .OrderByDescending(v => v.BaslangicTarihi)
                .Select(v => new VardiyaRaporItemDto
                {
                    Id = v.Id,
                    Tarih = v.BaslangicTarihi,
                    DosyaAdi = v.DosyaAdi ?? "",
                    Tutar = v.GenelToplam,
                    Durum = v.Durum.ToString()
                })
                .ToListAsync();

            var ozet = new VardiyaRaporOzetDto
            {
                ToplamVardiya = vardiyalar.Count,
                ToplamTutar = vardiyalar.Sum(v => v.Tutar),
                ToplamLitre = await query.SelectMany(v => v.OtomasyonSatislar).SumAsync(s => s.Litre) +
                              await query.SelectMany(v => v.FiloSatislar).SumAsync(s => s.Litre),
                ToplamIade = 0,
                ToplamGider = 0
            };

            return Ok(new VardiyaRaporuDto
            {
                Ozet = ozet,
                Vardiyalar = vardiyalar
            });
        }

        [HttpGet("fark-raporu")]
        public async Task<IActionResult> GetFarkRaporu([FromQuery] DateTimeOffset baslangic, [FromQuery] DateTimeOffset bitis)
        {
            var start = baslangic.UtcDateTime;
            var end = bitis.UtcDateTime;

            var query = _context.Vardiyalar.AsQueryable();

            if (IsAdmin) { }
            else if (IsPatron)
            {
                query = query.Where(v => v.Istasyon != null && v.Istasyon.Firma != null && v.Istasyon.Firma.PatronId == CurrentUserId);
            }
            else if (CurrentUserRole == "market_sorumlusu")
            {
                return Ok(new { Ozet = new { }, Vardiyalar = new List<object>() });
            }
            else
            {
                if (CurrentIstasyonId != null)
                {
                    query = query.Where(v => v.IstasyonId == CurrentIstasyonId);
                }
                else return Unauthorized();
            }

            var vardiyaOzetleri = await query
                .Where(v => v.BaslangicTarihi >= start && v.BaslangicTarihi <= end && v.Durum != VardiyaDurum.SILINDI)
                .Select(v => new {
                    v.Id,
                    v.BaslangicTarihi,
                    v.DosyaAdi,
                    v.PompaToplam,
                    v.Durum,
                    OtomasyonPersonelOzet = v.OtomasyonSatislar
                        .GroupBy(s => new { s.PersonelKeyId, s.PersonelAdi })
                        .Select(g => new { g.Key.PersonelKeyId, g.Key.PersonelAdi, Toplam = g.Sum(s => s.ToplamTutar) })
                        .ToList(),
                    PusulaOzet = v.Pusulalar
                        .Select(p => new { p.PersonelAdi, p.PersonelId, Toplam = p.Nakit + p.KrediKarti + p.DigerOdemeler.Sum(d => d.Tutar) })
                        .ToList(),
                    FiloToplam = v.FiloSatislar.Sum(f => f.Tutar)
                })
                .OrderByDescending(v => v.BaslangicTarihi)
                .ToListAsync();

            var raporItems = new List<FarkRaporItemDto>();

            foreach (var v in vardiyaOzetleri)
            {
                var item = new FarkRaporItemDto
                {
                    VardiyaId = v.Id,
                    Tarih = v.BaslangicTarihi,
                    DosyaAdi = v.DosyaAdi ?? "",
                    OtomasyonToplam = v.PompaToplam,
                    TahsilatToplam = v.PusulaOzet.Sum(p => p.Toplam) + v.FiloToplam,
                    Durum = v.Durum.ToString()
                };
                item.Fark = item.TahsilatToplam - item.OtomasyonToplam;

                // Personel bazlı farklar
                var personeller = v.OtomasyonPersonelOzet
                    .Select(g => new PersonelFarkDto
                    {
                        PersonelKeyId = g.PersonelKeyId,
                        PersonelAdi = g.PersonelAdi,
                        Otomasyon = g.Toplam
                    }).ToList();

                foreach (var p in personeller)
                {
                    var pusula = v.PusulaOzet.FirstOrDefault(ps => ps.PersonelAdi == p.PersonelAdi);
                    if (pusula != null)
                    {
                        p.Tahsilat = pusula.Toplam;
                    }
                    p.Fark = p.Tahsilat - p.Otomasyon;
                }
                
                if (v.FiloToplam > 0)
                {
                    personeller.Add(new PersonelFarkDto
                    {
                        PersonelAdi = "FİLO SATIŞLARI",
                        PersonelKeyId = "FILO",
                        Otomasyon = v.FiloToplam,
                        Tahsilat = v.FiloToplam,
                        Fark = 0
                    });
                }

                item.PersonelFarklari = personeller;
                raporItems.Add(item);
            }

            var ozet = new FarkRaporOzetDto
            {
                VardiyaSayisi = raporItems.Count,
                ToplamFark = raporItems.Sum(i => i.Fark),
                ToplamAcik = raporItems.Where(i => i.Fark < 0).Sum(i => Math.Abs(i.Fark)),
                ToplamFazla = raporItems.Where(i => i.Fark > 0).Sum(i => i.Fark),
                AcikVardiyaSayisi = raporItems.Count(i => i.Fark < -0.01m),
                FazlaVardiyaSayisi = raporItems.Count(i => i.Fark > 0.01m)
            };

            return Ok(new FarkRaporuDto
            {
                Ozet = ozet,
                Vardiyalar = raporItems
            });
        }

        [HttpGet("personel-karnesi/{personelId}")]
        public async Task<IActionResult> GetPersonelKarnesi(int personelId, [FromQuery] DateTimeOffset baslangic, [FromQuery] DateTimeOffset bitis)
        {
            var start = baslangic.UtcDateTime;
            var end = bitis.UtcDateTime;

            var personel = await _context.Personeller.FindAsync(personelId);
            if (personel == null) return NotFound();

            // Sadece bu personele ait satışları ve pusulaları çekiyoruz (silinmiş vardiyalar hariç)
            var satislar = await _context.OtomasyonSatislar
                .Where(s => s.PersonelId == personelId && s.Vardiya!.BaslangicTarihi >= start && s.Vardiya!.BaslangicTarihi <= end && s.Vardiya!.Durum != VardiyaDurum.SILINDI)
                .Select(s => new { s.VardiyaId, s.Vardiya!.BaslangicTarihi, s.ToplamTutar, s.Litre, s.YakitTuru })
                .ToListAsync();

            var pusulalar = await _context.Pusulalar
                .Where(p => p.PersonelId == personelId && p.Vardiya!.BaslangicTarihi >= start && p.Vardiya!.BaslangicTarihi <= end && p.Vardiya!.Durum != VardiyaDurum.SILINDI)
                .Select(p => new { p.VardiyaId, p.Nakit, p.KrediKarti, p.Aciklama, p.Vardiya!.BaslangicTarihi })
                .ToListAsync();

            var hareketler = satislar.GroupBy(s => new { s.VardiyaId, s.BaslangicTarihi })
                .Select(g => {
                    var pPusula = pusulalar.FirstOrDefault(p => p.VardiyaId == g.Key.VardiyaId);
                    var otomasyonSatis = g.Sum(s => s.ToplamTutar);
                    var manuelTahsilat = pPusula != null ? (pPusula.Nakit + pPusula.KrediKarti) : 0;
                    
                    return new PersonelHareketDto
                    {
                        Tarih = g.Key.BaslangicTarihi,
                        VardiyaId = g.Key.VardiyaId,
                        OtomasyonSatis = otomasyonSatis,
                        ManuelTahsilat = manuelTahsilat,
                        Fark = manuelTahsilat - otomasyonSatis,
                        AracSayisi = g.Count(),
                        Litre = g.Sum(s => s.Litre),
                        Aciklama = pPusula?.Aciklama
                    };
                }).ToList();

            // Pusulası olup satışı olmayan vardiyaları da ekleyelim
            var satisVardiyaIds = satislar.Select(s => s.VardiyaId).ToHashSet();
            foreach (var p in pusulalar.Where(p => !satisVardiyaIds.Contains(p.VardiyaId)))
            {
                var manuelTahsilat = p.Nakit + p.KrediKarti;
                hareketler.Add(new PersonelHareketDto
                {
                    Tarih = p.BaslangicTarihi,
                    VardiyaId = p.VardiyaId,
                    OtomasyonSatis = 0,
                    ManuelTahsilat = manuelTahsilat,
                    Fark = manuelTahsilat,
                    AracSayisi = 0,
                    Litre = 0,
                    Aciklama = p.Aciklama
                });
            }

            hareketler = hareketler.OrderByDescending(h => h.Tarih).ToList();

            var yakitMap = satislar.GroupBy(s => s.YakitTuru)
                .Select(g => new YakitDagilimiDto
                {
                    Yakit = g.Key.ToString(),
                    Litre = g.Sum(s => s.Litre),
                    Tutar = g.Sum(s => s.ToplamTutar)
                }).ToList();

            var toplamLitre = yakitMap.Sum(y => y.Litre);
            foreach (var y in yakitMap)
            {
                y.Oran = toplamLitre > 0 ? (y.Litre / toplamLitre) * 100 : 0;
            }

            var ozet = new PersonelKarneOzetDto
            {
                ToplamSatis = hareketler.Sum(h => h.OtomasyonSatis),
                ToplamTahsilat = hareketler.Sum(h => h.ManuelTahsilat),
                ToplamFark = hareketler.Sum(h => h.Fark),
                ToplamLitre = toplamLitre,
                AracSayisi = hareketler.Sum(h => h.AracSayisi),
                OrtalamaLitre = hareketler.Count > 0 ? toplamLitre / hareketler.Count : 0,
                OrtalamaTutar = hareketler.Count > 0 ? hareketler.Sum(h => h.OtomasyonSatis) / hareketler.Count : 0,
                YakitDagilimi = yakitMap.OrderByDescending(y => y.Litre).ToList()
            };

            return Ok(new PersonelKarnesiDto
            {
                Personel = new PersonelDto
                {
                    Id = personel.Id,
                    AdSoyad = personel.AdSoyad,
                    KeyId = personel.KeyId,
                    Rol = personel.Rol.ToString()
                },
                Hareketler = hareketler,
                Ozet = ozet
            });
        }

        [HttpGet("{id}/karsilastirma")]
        public async Task<IActionResult> GetKarsilastirma(int id)
        {
            var vardiyaOzet = await _context.Vardiyalar
                .Where(v => v.Id == id)
                .Select(v => new {
                    v.Id,
                    v.BaslangicTarihi,
                    v.PompaToplam,
                    PusulaOzet = v.Pusulalar.Select(p => new { p.Nakit, p.KrediKarti, p.Toplam }).ToList(),
                    FiloToplam = v.FiloSatislar.Sum(f => f.Tutar),
                    PompaOzetleri = v.OtomasyonSatislar
                        .GroupBy(s => new { s.PompaNo, s.YakitTuru })
                        .Select(g => new {
                            g.Key.PompaNo,
                            YakitTuru = g.Key.YakitTuru.ToString(),
                            Litre = g.Sum(s => s.Litre),
                            ToplamTutar = g.Sum(s => s.ToplamTutar),
                            IslemSayisi = g.Count()
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (vardiyaOzet == null) return NotFound();

            var sistemToplam = vardiyaOzet.PompaToplam;
            var tahsilatToplam = vardiyaOzet.PusulaOzet.Sum(p => p.Toplam) + vardiyaOzet.FiloToplam;
            var fark = tahsilatToplam - sistemToplam;
            var farkYuzde = sistemToplam > 0 ? (fark / sistemToplam) * 100 : 0;

            var durum = "UYUMLU";
            if (Math.Abs(fark) > 100) durum = "KRITIK_FARK";
            else if (Math.Abs(fark) > 1) durum = "FARK_VAR";

            var detaylar = new List<KarsilastirmaDetayDto>
            {
                new KarsilastirmaDetayDto { 
                    OdemeYontemi = "NAKIT", 
                    SistemTutar = 0,
                    TahsilatTutar = vardiyaOzet.PusulaOzet.Sum(p => p.Nakit),
                    Fark = vardiyaOzet.PusulaOzet.Sum(p => p.Nakit)
                },
                new KarsilastirmaDetayDto { 
                    OdemeYontemi = "KREDI_KARTI", 
                    SistemTutar = 0, 
                    TahsilatTutar = vardiyaOzet.PusulaOzet.Sum(p => p.KrediKarti),
                    Fark = vardiyaOzet.PusulaOzet.Sum(p => p.KrediKarti)
                },
                new KarsilastirmaDetayDto { 
                    OdemeYontemi = "FILO", 
                    SistemTutar = vardiyaOzet.FiloToplam, 
                    TahsilatTutar = vardiyaOzet.FiloToplam,
                    Fark = 0
                }
            };

            var pompaSatislari = vardiyaOzet.PompaOzetleri
                .Select(p => new PompaSatisOzetDto
                {
                    PompaNo = p.PompaNo,
                    YakitTuru = p.YakitTuru,
                    Litre = p.Litre,
                    ToplamTutar = p.ToplamTutar,
                    IslemSayisi = p.IslemSayisi
                })
                .OrderBy(p => p.PompaNo)
                .ToList();

            return Ok(new KarsilastirmaRaporuDto
            {
                VardiyaId = vardiyaOzet.Id,
                Tarih = vardiyaOzet.BaslangicTarihi,
                SistemToplam = sistemToplam,
                TahsilatToplam = tahsilatToplam,
                Fark = fark,
                FarkYuzde = farkYuzde,
                Durum = durum,
                Detaylar = detaylar,
                PompaSatislari = pompaSatislari
            });
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
            else if (CurrentUserRole == "market_sorumlusu")
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


        [HttpGet("onay-bekleyenler")]
        [Authorize(Roles = "admin,patron,vardiya sorumlusu,istasyon sorumlusu")]
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
                // Vardiya sorumlusu veya istasyon sorumlusu sadece kendi istasyonunu görsün
                query = query.Where(v => v.IstasyonId == CurrentIstasyonId.Value);
            }

            var list = await query.OrderByDescending(v => v.BaslangicTarihi).ToListAsync();
            return Ok(list);
        }

        [HttpPost("{id}/onaya-gonder")]
        [Authorize]
        public async Task<IActionResult> OnayaGonder(int id)
        {
            await _vardiyaService.OnayaGonderAsync(id, CurrentUserId, CurrentUserRole);
            return Ok(new { message = "Vardiya onaya gönderildi." });
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



        [HttpPost("{id}/silme-talebi")]
        [Authorize]
        public async Task<IActionResult> SilmeTalebi(int id, [FromBody] SilmeTalebiDto dto)
        {
            await _vardiyaService.SilmeTalebiOlusturAsync(id, dto, CurrentUserId, CurrentUserRole, User.Identity.Name);
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
        /// OPTIMIZED endpoint for Pompa Mutabakatı page
        /// Returns pre-aggregated data by personnel (GROUP BY at database level)
        /// </summary>
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
                 var v = await _context.Vardiyalar.Include(x => x.Istasyon).ThenInclude(x => x.Firma).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                 if (v == null) return NotFound();

                if (IsPatron)
                {
                    if (v.Istasyon?.Firma?.PatronId != CurrentUserId) return Forbid();
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

        /// <summary>
        /// OPTIMIZED endpoint for Onay Bekleyenler İncele functionality
        /// Returns pre-aggregated data with personnel analysis
        /// </summary>
        [HttpGet("{id}/onay-detay")]
        public async Task<IActionResult> GetOnayDetay(int id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"🔍 GetOnayDetay başladı (Centralized), ID: {id}");

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

                // ... (Logic continues unchanged) ... 

                var digerOdemelerToplam = digerOdemelerOzet.Sum(x => x.Toplam);
                var pusulaGenelToplam = data.GenelOzet.ToplamNakit + data.GenelOzet.ToplamKrediKarti + digerOdemelerToplam;
               
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
                            }).ToList()
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
                var krediKartiDetaylariList = new List<object>();
                foreach(var p in data.Pusulalar)
                {
                    if (!string.IsNullOrEmpty(p.KrediKartiDetay))
                    {
                        try {
                            var details = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(p.KrediKartiDetay);
                            if (details != null) krediKartiDetaylariList.AddRange(details);
                        } catch {}
                    }
                }
                
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

                 var finalBankaDetaylari = bankaDict.Select(kvp => new { Banka = kvp.Key, Tutar = kvp.Value })
                                            .OrderByDescending(x => x.Tutar).ToList();

                // Prepare dynamic access for mapping
                var vMapping = (dynamic)data.Vardiya;
                decimal pompaToplam = (decimal)vMapping.PompaToplam;
                decimal marketToplam = (decimal)vMapping.MarketToplam;
                decimal genelToplam = (decimal)vMapping.GenelToplam;
                // Wait, PompaToplam calculation for Fark might need the property too.

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
                    ToplamVeresiye = 0, 
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
