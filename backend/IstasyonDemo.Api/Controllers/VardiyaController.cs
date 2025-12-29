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
        public async Task<IActionResult> Create(CreateVardiyaDto dto)
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
                            f.FisNo
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
                            p.ParoPuan,
                            p.MobilOdeme,
                            p.KrediKartiDetay,
                            p.Aciklama,
                            p.Toplam
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
                        .Select(p => new { p.PersonelAdi, p.PersonelId, Toplam = p.Nakit + p.KrediKarti + p.ParoPuan + p.MobilOdeme })
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
                .Select(p => new { p.VardiyaId, p.Nakit, p.KrediKarti, p.ParoPuan, p.MobilOdeme, p.Aciklama, p.Vardiya!.BaslangicTarihi })
                .ToListAsync();

            var hareketler = satislar.GroupBy(s => new { s.VardiyaId, s.BaslangicTarihi })
                .Select(g => {
                    var pPusula = pusulalar.FirstOrDefault(p => p.VardiyaId == g.Key.VardiyaId);
                    var otomasyonSatis = g.Sum(s => s.ToplamTutar);
                    var manuelTahsilat = pPusula != null ? (pPusula.Nakit + pPusula.KrediKarti + pPusula.ParoPuan + pPusula.MobilOdeme) : 0;
                    
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
                var manuelTahsilat = p.Nakit + p.KrediKarti + p.ParoPuan + p.MobilOdeme;
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
                    PusulaOzet = v.Pusulalar.Select(p => new { p.Nakit, p.KrediKarti, p.ParoPuan, p.MobilOdeme, p.Toplam }).ToList(),
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
                    OdemeYontemi = "PARO_PUAN", 
                    SistemTutar = 0, 
                    TahsilatTutar = vardiyaOzet.PusulaOzet.Sum(p => p.ParoPuan),
                    Fark = vardiyaOzet.PusulaOzet.Sum(p => p.ParoPuan)
                },
                new KarsilastirmaDetayDto { 
                    OdemeYontemi = "MOBIL_ODEME", 
                    SistemTutar = 0, 
                    TahsilatTutar = vardiyaOzet.PusulaOzet.Sum(p => p.MobilOdeme),
                    Fark = vardiyaOzet.PusulaOzet.Sum(p => p.MobilOdeme)
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
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> GetOnayBekleyenler()
        {
            IQueryable<Vardiya> query = _context.Vardiyalar
                .Include(v => v.Istasyon)
                .Where(v => v.Durum == VardiyaDurum.ONAY_BEKLIYOR || v.Durum == VardiyaDurum.SILINME_ONAYI_BEKLIYOR);

            if (IsPatron)
            {
                query = query.Where(v => v.Istasyon != null && v.Istasyon.Firma != null && v.Istasyon.Firma.PatronId == CurrentUserId);
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
        [HttpGet("{id}/mutabakat")]
        public async Task<IActionResult> GetMutabakat(int id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"🚀 GetMutabakat başladı, ID: {id}");

            // 1. Vardiya temel bilgileri
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
                    v.DosyaAdi,
                    v.RedNedeni
                })
                .FirstOrDefaultAsync();

            if (vardiya == null)
            {
                return NotFound();
            }

            // Security Check for Mutabakat
            if (!IsAdmin)
            {
                if (IsPatron)
                {
                    var station = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == vardiya.IstasyonId);
                    if (station?.Firma?.PatronId != CurrentUserId) return Forbid();
                }
                else
                {
                    if (vardiya.IstasyonId != CurrentIstasyonId) return Forbid();
                }
            }

            Console.WriteLine($"⏱️ Vardiya sorgusu: {stopwatch.ElapsedMilliseconds}ms");

            // 2. Personel bazında GRUPLANMIŞ otomasyon satışları (DATABASE LEVEL GROUP BY)
            var personelOzetler = await _context.OtomasyonSatislar
                .AsNoTracking()
                .Where(s => s.VardiyaId == id)
                .GroupBy(s => new { s.PersonelAdi, s.PersonelId })
                .Select(g => new
                {
                    PersonelAdi = g.Key.PersonelAdi,
                    PersonelId = g.Key.PersonelId,
                    ToplamLitre = g.Sum(s => s.Litre),
                    ToplamTutar = g.Sum(s => s.ToplamTutar),
                    IslemSayisi = g.Count()
                })
                .ToListAsync();

            Console.WriteLine($"⏱️ Personel özetleri sorgusu: {stopwatch.ElapsedMilliseconds}ms");

            // 3. Filo satışları özeti
            var filoOzet = await _context.FiloSatislar
                .AsNoTracking()
                .Where(f => f.VardiyaId == id)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    ToplamTutar = g.Sum(f => f.Tutar),
                    ToplamLitre = g.Sum(f => f.Litre),
                    IslemSayisi = g.Count()
                })
                .FirstOrDefaultAsync();

            Console.WriteLine($"⏱️ Filo özeti sorgusu: {stopwatch.ElapsedMilliseconds}ms");

            // 4. Filo detayları (gruplu)
            var filoDetaylari = await _context.FiloSatislar
                .AsNoTracking()
                .Where(f => f.VardiyaId == id)
                .GroupBy(f => f.FiloKodu)
                .Select(g => new
                {
                    FiloKodu = g.Key,
                    Tutar = g.Sum(f => f.Tutar),
                    Litre = g.Sum(f => f.Litre)
                })
                .ToListAsync();

            Console.WriteLine($"⏱️ Filo detayları sorgusu: {stopwatch.ElapsedMilliseconds}ms");

            // 5. Pusulalar (zaten az kayıt)
            var pusulalar = await _context.Pusulalar
                .AsNoTracking()
                .Where(p => p.VardiyaId == id)
                .Select(p => new
                {
                    p.Id,
                    p.PersonelAdi,
                    p.PersonelId,
                    p.Nakit,
                    p.KrediKarti,
                    p.ParoPuan,
                    p.MobilOdeme,
                    p.KrediKartiDetay,
                    p.Aciklama,
                    p.Toplam
                })
                .ToListAsync();

            stopwatch.Stop();
            Console.WriteLine($"✅ GetMutabakat tamamlandı: {stopwatch.ElapsedMilliseconds}ms toplam");

            return Ok(new
            {
                Vardiya = vardiya,
                PersonelOzetler = personelOzetler,
                FiloOzet = filoOzet,
                FiloDetaylari = filoDetaylari,
                Pusulalar = pusulalar,
                _performanceMs = stopwatch.ElapsedMilliseconds
            });
        }

        /// <summary>
        /// OPTIMIZED endpoint for Onay Bekleyenler İncele functionality
        /// Returns pre-aggregated data with personnel analysis
        /// </summary>
        [HttpGet("{id}/onay-detay")]
        public async Task<IActionResult> GetOnayDetay(int id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"🔍 GetOnayDetay başladı, ID: {id}");

            // 1. Vardiya temel bilgileri
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
                    v.DosyaAdi,
                    v.RedNedeni,
                    v.OnaylayanAdi,
                    v.OnayTarihi
                })
                .FirstOrDefaultAsync();

            if (vardiya == null)
            {
                return NotFound();
            }

            // Security Check
            if (!IsAdmin)
            {
                if (IsPatron)
                {
                    var station = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == vardiya.IstasyonId);
                    if (station?.Firma?.PatronId != CurrentUserId) return Forbid();
                }
                else
                {
                    if (vardiya.IstasyonId != CurrentIstasyonId) return Forbid();
                }
            }

            Console.WriteLine($"⏱️ Vardiya sorgusu: {stopwatch.ElapsedMilliseconds}ms");

            // 2. Personel bazında GRUPLANMIŞ otomasyon satışları
            var personelOzetler = await _context.OtomasyonSatislar
                .AsNoTracking()
                .Where(s => s.VardiyaId == id)
                .GroupBy(s => new { s.PersonelAdi, s.PersonelId })
                .Select(g => new
                {
                    PersonelAdi = g.Key.PersonelAdi,
                    PersonelId = g.Key.PersonelId,
                    ToplamLitre = g.Sum(s => s.Litre),
                    OtomasyonToplam = g.Sum(s => s.ToplamTutar),
                    IslemSayisi = g.Count()
                })
                .ToListAsync();

            Console.WriteLine($"⏱️ Personel özetleri: {stopwatch.ElapsedMilliseconds}ms");

            // 3. Pusulalar (personel bazında gruplanmış)
            var pusulalar = await _context.Pusulalar
                .AsNoTracking()
                .Where(p => p.VardiyaId == id)
                .Select(p => new
                {
                    p.Id,
                    p.PersonelAdi,
                    p.PersonelId,
                    p.Nakit,
                    p.KrediKarti,
                    p.ParoPuan,
                    p.MobilOdeme,
                    p.KrediKartiDetay,
                    p.Aciklama,
                    p.Toplam
                })
                .ToListAsync();

            Console.WriteLine($"⏱️ Pusulalar: {stopwatch.ElapsedMilliseconds}ms");

            // 3.5. Filo Satışları özeti
            var filoOzet = await _context.FiloSatislar
                .AsNoTracking()
                .Where(f => f.VardiyaId == id)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    ToplamTutar = g.Sum(f => f.Tutar),
                    ToplamLitre = g.Sum(f => f.Litre),
                    IslemSayisi = g.Count()
                })
                .FirstOrDefaultAsync();

            var filoToplam = filoOzet?.ToplamTutar ?? 0;
            Console.WriteLine($"⏱️ Filo özeti: {stopwatch.ElapsedMilliseconds}ms, Toplam: {filoToplam}");

            // 4. Sunucu tarafında fark analizi hesapla
            var farkAnalizi = new List<object>();
            var processedPersonel = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Önce otomasyon personellerini ekle
            foreach (var personel in personelOzetler)
            {
                var pusula = pusulalar.FirstOrDefault(p => 
                    (p.PersonelId != null && p.PersonelId == personel.PersonelId) ||
                    (p.PersonelAdi != null && p.PersonelAdi.Trim().ToLower() == personel.PersonelAdi?.Trim().ToLower()));
                
                var pusulaToplam = pusula?.Toplam ?? 0;
                var fark = pusulaToplam - personel.OtomasyonToplam;

                farkAnalizi.Add(new
                {
                    PersonelId = personel.PersonelId,
                    PersonelAdi = personel.PersonelAdi,
                    OtomasyonToplam = personel.OtomasyonToplam,
                    PusulaToplam = pusulaToplam,
                    Fark = fark,
                    FarkDurum = Math.Abs(fark) < 1 ? "UYUMLU" : (fark < 0 ? "ACIK" : "FAZLA"),
                    PusulaDokum = pusula != null ? new
                    {
                        Nakit = pusula.Nakit,
                        KrediKarti = pusula.KrediKarti,
                        ParoPuan = pusula.ParoPuan,
                        MobilOdeme = pusula.MobilOdeme,
                        KrediKartiDetay = pusula.KrediKartiDetay
                    } : null
                });

                if (personel.PersonelAdi != null)
                    processedPersonel.Add(personel.PersonelAdi.Trim().ToLower());
            }

            // Pusulası olup otomasyonu olmayan personelleri ekle
            foreach (var pusula in pusulalar)
            {
                var key = pusula.PersonelAdi?.Trim().ToLower() ?? "";
                if (!string.IsNullOrEmpty(key) && !processedPersonel.Contains(key))
                {
                    farkAnalizi.Add(new
                    {
                        PersonelId = pusula.PersonelId,
                        PersonelAdi = pusula.PersonelAdi,
                        OtomasyonToplam = 0m,
                        PusulaToplam = pusula.Toplam,
                        Fark = pusula.Toplam,
                        FarkDurum = pusula.Toplam > 1 ? "FAZLA" : "UYUMLU",
                        PusulaDokum = new
                        {
                            Nakit = pusula.Nakit,
                            KrediKarti = pusula.KrediKarti,
                            ParoPuan = pusula.ParoPuan,
                            MobilOdeme = pusula.MobilOdeme,
                            KrediKartiDetay = pusula.KrediKartiDetay
                        }
                    });
                }
            }

            // 4.5. Filo Satışlarını fark analizine ekle (Tabloda görünmesi için)
            if (filoToplam > 0)
            {
                farkAnalizi.Add(new
                {
                    PersonelId = -1,
                    PersonelAdi = "FİLO SATIŞLARI",
                    OtomasyonToplam = filoToplam,
                    PusulaToplam = filoToplam, // Filo satışları otomatik mutabık sayılır
                    Fark = 0m,
                    FarkDurum = "UYUMLU",
                    PusulaDokum = new
                    {
                        Nakit = 0m,
                        KrediKarti = 0m,
                        ParoPuan = 0m,
                        MobilOdeme = 0m,
                        KrediKartiDetay = (string?)null
                    }
                });
            }

            // 5. Genel özet hesapla
            var toplamNakit = pusulalar.Sum(p => p.Nakit);
            var toplamKrediKarti = pusulalar.Sum(p => p.KrediKarti);
            var toplamParoPuan = pusulalar.Sum(p => p.ParoPuan);
            var toplamMobilOdeme = pusulalar.Sum(p => p.MobilOdeme);
            var pusulaToplami = toplamNakit + toplamKrediKarti + toplamParoPuan + toplamMobilOdeme;
            
            // Fark = (Pusula Toplamı + Filo Satışları) - Pompa Toplamı
            var toplamFark = (pusulaToplami + filoToplam) - vardiya.PompaToplam;
            Console.WriteLine($"📊 Fark Hesabı: ({pusulaToplami} + {filoToplam}) - {vardiya.PompaToplam} = {toplamFark}");

            var genelOzet = new
            {
                PompaToplam = vardiya.PompaToplam,
                MarketToplam = vardiya.MarketToplam,
                GenelToplam = vardiya.GenelToplam,
                ToplamNakit = toplamNakit,
                ToplamKrediKarti = toplamKrediKarti,
                ToplamParoPuan = toplamParoPuan,
                ToplamMobilOdeme = toplamMobilOdeme,
                PusulaToplam = pusulaToplami,
                FiloToplam = filoToplam,
                ToplamFark = toplamFark,
                DurumRenk = Math.Abs(toplamFark) < 10 ? "success" : (toplamFark < 0 ? "danger" : "warn")
            };

            // 6. Kredi kartı detayları - banka bazlı grupla
            var krediKartiDetaylari = new Dictionary<string, decimal>();
            foreach (var pusula in pusulalar)
            {
                if (!string.IsNullOrEmpty(pusula.KrediKartiDetay))
                {
                    try
                    {
                        Console.WriteLine($"📄 Parsing KrediKartiDetay: {pusula.KrediKartiDetay}");
                        
                        using var doc = System.Text.Json.JsonDocument.Parse(pusula.KrediKartiDetay);
                        var detaylar = doc.RootElement;
                        
                        if (detaylar.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var detay in detaylar.EnumerateArray())
                            {
                                var banka = detay.TryGetProperty("banka", out var bankaEl) ? bankaEl.GetString() ?? "Diğer" : "Diğer";
                                decimal tutar = 0;
                                if (detay.TryGetProperty("tutar", out var tutarEl))
                                {
                                    if (tutarEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                                        tutar = tutarEl.GetDecimal();
                                    else if (tutarEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                        decimal.TryParse(tutarEl.GetString(), out tutar);
                                }
                                
                                Console.WriteLine($"   → Banka: {banka}, Tutar: {tutar}");
                                
                                if (!krediKartiDetaylari.ContainsKey(banka))
                                    krediKartiDetaylari[banka] = 0;
                                krediKartiDetaylari[banka] += tutar;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ KrediKartiDetay parse hatası: {ex.Message}");
                    }
                }
            }
            
            Console.WriteLine($"💳 Toplam {krediKartiDetaylari.Count} banka detayı bulundu");

            // Detaysız kredi kartı tutarlarını ekle (banka detayı olmadan girilen)
            decimal detaysizKrediKarti = 0;
            foreach (var pusula in pusulalar)
            {
                if (pusula.KrediKarti > 0 && string.IsNullOrEmpty(pusula.KrediKartiDetay))
                {
                    detaysizKrediKarti += pusula.KrediKarti;
                }
            }
            if (detaysizKrediKarti > 0)
            {
                krediKartiDetaylari["Genel / Detaysız"] = detaysizKrediKarti;
            }

            // Paro Puan ve Mobil Ödeme'yi de ekle
            var toplamParoPuanDetay = pusulalar.Sum(p => p.ParoPuan);
            var toplamMobilOdemeDetay = pusulalar.Sum(p => p.MobilOdeme);
            
            if (toplamParoPuanDetay > 0)
                krediKartiDetaylari["Paro Puan"] = toplamParoPuanDetay;
            if (toplamMobilOdemeDetay > 0)
                krediKartiDetaylari["Mobil Ödeme"] = toplamMobilOdemeDetay;

            var bankaDetaylari = krediKartiDetaylari.Select(kvp => new { Banka = kvp.Key, Tutar = kvp.Value }).OrderByDescending(x => x.Tutar).ToList();

            stopwatch.Stop();
            Console.WriteLine($"✅ GetOnayDetay tamamlandı: {stopwatch.ElapsedMilliseconds}ms toplam");

            return Ok(new
            {
                Vardiya = vardiya,
                GenelOzet = genelOzet,
                FarkAnalizi = farkAnalizi,
                KrediKartiDetaylari = bankaDetaylari,
                PersonelSayisi = farkAnalizi.Count,
                _performanceMs = stopwatch.ElapsedMilliseconds
            });
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
