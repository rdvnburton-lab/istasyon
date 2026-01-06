using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/reports/vardiya")]
    [Authorize]
    public class VardiyaReportController : BaseController
    {
        private readonly AppDbContext _context;

        public VardiyaReportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("genel")]
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
            else if (CurrentUserRole == "market sorumlusu")
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

        [HttpGet("fark")]
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
            else if (CurrentUserRole == "market sorumlusu")
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

            var satislar = await _context.OtomasyonSatislar
                .Where(s => s.PersonelId == personelId && s.Vardiya!.BaslangicTarihi >= start && s.Vardiya!.BaslangicTarihi <= end && s.Vardiya!.Durum != VardiyaDurum.SILINDI)
                .Select(s => new { s.VardiyaId, s.Vardiya!.BaslangicTarihi, s.ToplamTutar, s.Litre, YakitTuru = s.Yakit != null ? s.Yakit.Ad : s.YakitTuru })
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

        [HttpGet("karsilastirma/{vardiyaId}")]
        public async Task<IActionResult> GetKarsilastirma(int vardiyaId)
        {
            var vardiyaOzet = await _context.Vardiyalar
                .Where(v => v.Id == vardiyaId)
                .Select(v => new {
                    v.Id,
                    v.BaslangicTarihi,
                    v.PompaToplam,
                    PusulaOzet = v.Pusulalar.Select(p => new { p.Nakit, p.KrediKarti, Toplam = p.Nakit + p.KrediKarti + p.DigerOdemeler.Sum(d => d.Tutar) }).ToList(),
                    FiloToplam = v.FiloSatislar.Sum(f => f.Tutar),
                    PompaOzetleri = v.OtomasyonSatislar
                        .GroupBy(s => new { s.PompaNo, YakitAdi = s.Yakit != null ? s.Yakit.Ad : s.YakitTuru })
                        .Select(g => new {
                            g.Key.PompaNo,
                            YakitTuru = g.Key.YakitAdi,
                            Litre = g.Sum(s => s.Litre),
                            ToplamTutar = g.Sum(s => s.ToplamTutar),
                            IslemSayisi = g.Count()
                        }).ToList(),
                    FiloOzetleri = v.FiloSatislar
                        .GroupBy(f => new { f.PompaNo, YakitAdi = f.Yakit != null ? f.Yakit.Ad : f.YakitTuru })
                        .Select(g => new {
                            g.Key.PompaNo,
                            YakitTuru = g.Key.YakitAdi,
                            Litre = g.Sum(f => f.Litre),
                            ToplamTutar = g.Sum(f => f.Tutar),
                            IslemSayisi = g.Count()
                        }).ToList(),
                    OdemeTuruOzet = v.OtomasyonSatislar
                        .GroupBy(s => s.OdemeTuru)
                        .Select(g => new { OdemeTuru = g.Key, Toplam = g.Sum(s => s.ToplamTutar) })
                        .ToList(),
                    PompaEndeksleri = v.PompaEndeksleri.ToList()
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

            // Pompacı Satışı (Otomasyon Toplam vs Pusula Toplam)
            var pompacisatisSistem = vardiyaOzet.PompaOzetleri.Sum(p => p.ToplamTutar); // Otomasyon Satışları Toplamı
            var pompacisatisTahsilat = vardiyaOzet.PusulaOzet.Sum(p => p.Toplam); // Pusula (Nakit + KK) Toplamı

            var detaylar = new List<KarsilastirmaDetayDto>
            {
                new KarsilastirmaDetayDto { 
                    OdemeYontemi = "POMPACI_SATISI", 
                    SistemTutar = pompacisatisSistem,
                    TahsilatTutar = pompacisatisTahsilat,
                    Fark = pompacisatisTahsilat - pompacisatisSistem
                },
                new KarsilastirmaDetayDto { 
                    OdemeYontemi = "FILO", 
                    SistemTutar = vardiyaOzet.FiloToplam, 
                    TahsilatTutar = vardiyaOzet.FiloToplam,
                    Fark = 0
                }
            };

            // Otomasyon ve Filo Satışlarını Birleştir
            var birlesikSatislar = vardiyaOzet.PompaOzetleri
                .Select(p => new { p.PompaNo, p.YakitTuru, p.Litre, p.ToplamTutar, p.IslemSayisi })
                .Concat(vardiyaOzet.FiloOzetleri.Select(f => new { f.PompaNo, f.YakitTuru, f.Litre, f.ToplamTutar, f.IslemSayisi }))
                .GroupBy(x => new { x.PompaNo, x.YakitTuru })
                .Select(g => new {
                    g.Key.PompaNo,
                    g.Key.YakitTuru,
                    Litre = g.Sum(x => x.Litre),
                    ToplamTutar = g.Sum(x => x.ToplamTutar),
                    IslemSayisi = g.Sum(x => x.IslemSayisi)
                })
                .ToList();

            var pompaSatislari = birlesikSatislar
                .Select(p => {
                    var matchingEndeksler = vardiyaOzet.PompaEndeksleri
                        .Where(e => e.PompaNo == p.PompaNo && e.YakitTuru == p.YakitTuru)
                        .ToList();
                    
                    return new PompaSatisOzetDto
                    {
                        PompaNo = p.PompaNo,
                        YakitTuru = p.YakitTuru,
                        Litre = p.Litre,
                        ToplamTutar = p.ToplamTutar,
                        IslemSayisi = p.IslemSayisi,
                        BaslangicEndeks = matchingEndeksler.Sum(e => e.BaslangicEndeks),
                        BitisEndeks = matchingEndeksler.Sum(e => e.BitisEndeks)
                    };
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
    }
}
