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
    [Route("api/reports/vardiya")]
    [Authorize]
    public class VardiyaReportController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly VardiyaArsivService _arsivService;

        public VardiyaReportController(AppDbContext context, VardiyaArsivService arsivService)
        {
            _context = context;
            _arsivService = arsivService;
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

            // Verileri ve arşivdeki JSON'ları çek
            var vardiyaVerileri = await query
                .OrderByDescending(v => v.BaslangicTarihi)
                .Select(v => new
                {
                    v.Id,
                    v.BaslangicTarihi,
                    v.DosyaAdi,
                    v.GenelToplam,
                    v.Durum,
                    PersonelSatisJson = v.RaporArsiv != null ? v.RaporArsiv.PersonelSatisDetayJson : null,
                    FiloSatisJson = v.RaporArsiv != null ? v.RaporArsiv.FiloSatisDetayJson : null
                })
                .ToListAsync();

            var vardiyalar = vardiyaVerileri.Select(v => new VardiyaRaporItemDto
            {
                Id = v.Id,
                Tarih = v.BaslangicTarihi,
                DosyaAdi = v.DosyaAdi ?? "",
                Tutar = v.GenelToplam,
                Durum = v.Durum.ToString()
            }).ToList();

            // Toplam Litreyi JSON'dan hesapla
            decimal toplamLitre = 0;
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var v in vardiyaVerileri)
            {
                if (!string.IsNullOrEmpty(v.PersonelSatisJson))
                {
                    try
                    {
                        var personelDetay = System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDemo.Api.Services.PersonelSatisDetayDto>>(v.PersonelSatisJson, jsonOptions);
                        if (personelDetay != null)
                        {
                            toplamLitre += personelDetay.Sum(p => p.Satislar.Sum(s => s.Litre));
                        }
                    }
                    catch { /* Ignore */ }
                }

                if (!string.IsNullOrEmpty(v.FiloSatisJson))
                {
                    try
                    {
                        var filoDetay = System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDemo.Api.Services.FiloSatisDetayDto>>(v.FiloSatisJson, jsonOptions);
                        if (filoDetay != null)
                        {
                            toplamLitre += filoDetay.Sum(f => f.Litre);
                        }
                    }
                    catch { /* Ignore */ }
                }
            }

            var ozet = new VardiyaRaporOzetDto
            {
                ToplamVardiya = vardiyalar.Count,
                ToplamTutar = vardiyalar.Sum(v => v.Tutar),
                ToplamLitre = toplamLitre,
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

            // Personel eşleşmesi için tüm personelleri çek
            var personeller = await _context.Personeller.Select(p => new { p.Id, p.KeyId, p.AdSoyad }).ToListAsync();

            var arsivler = await query
                .Where(v => v.BaslangicTarihi >= start && v.BaslangicTarihi <= end && v.Durum != VardiyaDurum.SILINDI)
                .OrderByDescending(v => v.BaslangicTarihi)
                .Select(v => new {
                    v.Id,
                    v.BaslangicTarihi,
                    v.DosyaAdi,
                    v.Durum,
                    PersonelSatisJson = v.RaporArsiv != null ? v.RaporArsiv.PersonelSatisDetayJson : null,
                    FiloSatisJson = v.RaporArsiv != null ? v.RaporArsiv.FiloSatisDetayJson : null,
                    Pusulalar = v.Pusulalar.Select(p => new { p.PersonelId, p.PersonelAdi, Toplam = p.Nakit + p.KrediKarti + p.DigerOdemeler.Sum(d => d.Tutar) }).ToList()
                })
                .ToListAsync();

            var raporItems = new List<FarkRaporItemDto>();
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var v in arsivler)
            {
                decimal otomasyonToplam = 0;
                decimal filoToplam = 0;
                var personelFarklari = new List<PersonelFarkDto>();

                // 1. Otomasyon Satışları (Personel Bazlı)
                if (!string.IsNullOrEmpty(v.PersonelSatisJson))
                {
                    try
                    {
                        var personelDetay = System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDemo.Api.Services.PersonelSatisDetayDto>>(v.PersonelSatisJson, jsonOptions);
                        if (personelDetay != null)
                        {
                            foreach (var p in personelDetay)
                            {
                                var pToplam = p.Satislar.Sum(s => s.Tutar);
                                otomasyonToplam += pToplam;

                                // Personel ID bul
                                var personel = personeller.FirstOrDefault(x => x.KeyId == p.PersonelKeyId);
                                var personelId = personel?.Id ?? 0;
                                var personelAdi = personel?.AdSoyad ?? p.PersonelAdi;

                                // Tahsilat bul
                                var pusula = v.Pusulalar.FirstOrDefault(x => x.PersonelId == personelId);
                                var tahsilat = pusula?.Toplam ?? 0;

                                personelFarklari.Add(new PersonelFarkDto
                                {
                                    PersonelKeyId = p.PersonelKeyId,
                                    PersonelAdi = personelAdi,
                                    Otomasyon = pToplam,
                                    Tahsilat = tahsilat,
                                    Fark = tahsilat - pToplam
                                });
                            }
                        }
                    }
                    catch { /* Ignore */ }
                }

                // 2. Filo Satışları
                if (!string.IsNullOrEmpty(v.FiloSatisJson))
                {
                    try
                    {
                        var filoDetay = System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDemo.Api.Services.FiloSatisDetayDto>>(v.FiloSatisJson, jsonOptions);
                        if (filoDetay != null)
                        {
                            filoToplam = filoDetay.Sum(f => f.Tutar);
                        }
                    }
                    catch { /* Ignore */ }
                }

                // Pusulası olup satışı olmayan personelleri de ekle
                foreach (var pusula in v.Pusulalar)
                {
                    // Personel ID üzerinden kontrol
                    var pKeyId = personeller.FirstOrDefault(x => x.Id == pusula.PersonelId)?.KeyId;
                    
                    // Eğer listede yoksa ekle
                    bool exists = false;
                    if (pKeyId != null) exists = personelFarklari.Any(p => p.PersonelKeyId == pKeyId);
                    else exists = personelFarklari.Any(p => p.PersonelAdi == pusula.PersonelAdi); // Fallback

                    if (!exists)
                    {
                        personelFarklari.Add(new PersonelFarkDto
                        {
                            PersonelKeyId = pKeyId ?? "",
                            PersonelAdi = pusula.PersonelAdi ?? "",
                            Otomasyon = 0,
                            Tahsilat = pusula.Toplam,
                            Fark = pusula.Toplam
                        });
                    }
                }

                // Filo Satışlarını Listeye Ekle
                if (filoToplam > 0)
                {
                    personelFarklari.Add(new PersonelFarkDto
                    {
                        PersonelAdi = "FİLO SATIŞLARI",
                        PersonelKeyId = "FILO",
                        Otomasyon = filoToplam,
                        Tahsilat = filoToplam, // Filo satışı sanal tahsilat sayılır, fark oluşturmaz
                        Fark = 0
                    });
                }

                // Genel Toplamlar
                // OtomasyonToplam = Personel Satışları + Filo Satışları
                // TahsilatToplam = Pusulalar + Filo Satışları
                
                var item = new FarkRaporItemDto
                {
                    VardiyaId = v.Id,
                    Tarih = v.BaslangicTarihi,
                    DosyaAdi = v.DosyaAdi ?? "",
                    OtomasyonToplam = otomasyonToplam + filoToplam,
                    TahsilatToplam = v.Pusulalar.Sum(p => p.Toplam) + filoToplam,
                    Durum = v.Durum.ToString(),
                    PersonelFarklari = personelFarklari
                };
                item.Fark = item.TahsilatToplam - item.OtomasyonToplam;
                
                raporItems.Add(item);
            }

            var ozet = new FarkRaporOzetDto
            {
                ToplamFark = raporItems.Sum(r => r.Fark),
                ToplamAcik = raporItems.Where(r => r.Fark < 0).Sum(r => r.Fark),
                ToplamFazla = raporItems.Where(r => r.Fark > 0).Sum(r => r.Fark),
                VardiyaSayisi = raporItems.Count,
                AcikVardiyaSayisi = raporItems.Count(r => r.Fark < -1), // -1 tolerans
                FazlaVardiyaSayisi = raporItems.Count(r => r.Fark > 1)
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

            // 1. Onaylı vardiyaların arşivlerini çek (JSON)
            var arsivler = await _context.VardiyaRaporArsivleri
                .Where(a => a.Tarih >= start && a.Tarih <= end)
                .Select(a => new { 
                    a.VardiyaId, 
                    a.Tarih, 
                    a.PersonelSatisDetayJson 
                })
                .ToListAsync();

            // 2. Pusulaları çek (Manuel tahsilat - Silinmeyen tablo)
            var pusulalar = await _context.Pusulalar
                .Where(p => p.PersonelId == personelId && p.Vardiya!.BaslangicTarihi >= start && p.Vardiya!.BaslangicTarihi <= end && p.Vardiya!.Durum == VardiyaDurum.ONAYLANDI)
                .Select(p => new { p.VardiyaId, p.Nakit, p.KrediKarti, p.Aciklama, p.Vardiya!.BaslangicTarihi })
                .ToListAsync();

            var hareketler = new List<PersonelHareketDto>();
            var yakitMap = new List<YakitDagilimiDto>();

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var arsiv in arsivler)
            {
                if (string.IsNullOrEmpty(arsiv.PersonelSatisDetayJson)) continue;

                try
                {
                    var detaylar = System.Text.Json.JsonSerializer.Deserialize<List<PersonelSatisDetayDto>>(arsiv.PersonelSatisDetayJson, jsonOptions);
                    var personelDetay = detaylar?.FirstOrDefault(d => d.PersonelKeyId == personel.KeyId);

                    decimal otomasyonSatis = 0;
                    decimal litre = 0;
                    int aracSayisi = 0;
                    if (personelDetay != null)
                    {
                        otomasyonSatis = personelDetay.Satislar.Sum(s => s.Tutar);
                        litre = personelDetay.Satislar.Sum(s => s.Litre);
                        aracSayisi = personelDetay.ToplamIslemSayisi; // 🆕 JSON'dan oku
                        
                        // Yakıt dağılımını topla
                        foreach (var satis in personelDetay.Satislar)
                        {
                            var existing = yakitMap.FirstOrDefault(y => y.Yakit == satis.YakitTuru);
                            if (existing != null)
                            {
                                existing.Litre += satis.Litre;
                                existing.Tutar += satis.Tutar;
                            }
                            else
                            {
                                yakitMap.Add(new YakitDagilimiDto { Yakit = satis.YakitTuru, Litre = satis.Litre, Tutar = satis.Tutar });
                            }
                        }
                    }

                    var pPusula = pusulalar.FirstOrDefault(p => p.VardiyaId == arsiv.VardiyaId);
                    var manuelTahsilat = pPusula != null ? (pPusula.Nakit + pPusula.KrediKarti) : 0;

                    // Eğer hem satış hem tahsilat yoksa listeye ekleme
                    if (otomasyonSatis == 0 && manuelTahsilat == 0) continue;

                    hareketler.Add(new PersonelHareketDto
                    {
                        Tarih = arsiv.Tarih,
                        VardiyaId = arsiv.VardiyaId,
                        OtomasyonSatis = otomasyonSatis,
                        ManuelTahsilat = manuelTahsilat,
                        Fark = manuelTahsilat - otomasyonSatis,
                        AracSayisi = 0, // JSON'da yoktu, eklenebilir
                        Litre = litre,
                        Aciklama = pPusula?.Aciklama
                    });
                }
                catch (Exception ex)
                {
                    // JSON parse hatası
                    Console.WriteLine($"JSON Parse Error Vardiya {arsiv.VardiyaId}: {ex.Message}");
                }
            }

            // Sadece pusulası olup satışı olmayanlar (zaten yukarıda kapsandı ama kontrol edelim)
            // Arşivde kaydı olmayan ama pusulası olan durumlar (eski veriler) için:
            var arsivVardiyaIds = arsivler.Select(a => a.VardiyaId).ToHashSet();
            foreach (var p in pusulalar.Where(p => !arsivVardiyaIds.Contains(p.VardiyaId)))
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
            // Vardiyayı kontrol et
            var vardiya = await _context.Vardiyalar
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vardiyaId);

            if (vardiya == null)
                return NotFound(new { message = "Vardiya bulunamadı." });

            // Sadece onaylanmış vardiyalar için rapor göster
            if (vardiya.Durum != VardiyaDurum.ONAYLANDI)
            {
                return BadRequest(new { message = "Bu vardiya henüz onaylanmadı, rapor mevcut değil." });
            }

            // Arşivden oku
            var arsivRaporu = await _arsivService.GetKarsilastirmaRaporuFromArsiv(vardiyaId);
            if (arsivRaporu != null)
            {
                return Ok(arsivRaporu);
            }

            // Arşiv yoksa - bu durum olmamalı (onaylı ama arşivlenmemiş)
            return StatusCode(500, new { message = "Rapor arşivi bulunamadı. Lütfen sistem yöneticisiyle iletişime geçin." });
        }
    }
}
