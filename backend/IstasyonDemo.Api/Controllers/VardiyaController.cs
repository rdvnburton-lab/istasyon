using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VardiyaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VardiyaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateVardiyaDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Vardiya oluştur
                var vardiya = new Vardiya
                {
                    IstasyonId = dto.IstasyonId,
                    BaslangicTarihi = dto.BaslangicTarihi.ToUniversalTime(), // Postgres için UTC
                    BitisTarihi = dto.BitisTarihi?.ToUniversalTime(),
                    DosyaAdi = dto.DosyaAdi,
                    DosyaIcerik = !string.IsNullOrEmpty(dto.DosyaIcerik) ? Convert.FromBase64String(dto.DosyaIcerik) : null,
                    Durum = VardiyaDurum.ACIK,
                    OlusturmaTarihi = DateTime.UtcNow,
                    
                    // Toplamları hesapla
                    PompaToplam = dto.OtomasyonSatislar.Sum(s => s.ToplamTutar) + dto.FiloSatislar.Sum(s => s.Tutar),
                    MarketToplam = 0, // Şimdilik 0
                    GenelToplam = dto.OtomasyonSatislar.Sum(s => s.ToplamTutar) + dto.FiloSatislar.Sum(s => s.Tutar)
                };

                _context.Vardiyalar.Add(vardiya);
                await _context.SaveChangesAsync();

                // 2. Otomasyon Satışlarını Ekle
                foreach (var satisDto in dto.OtomasyonSatislar)
                {
                    // Personel kontrolü (KeyId veya İsimden bulmaya çalış)
                    var personel = await _context.Personeller
                        .FirstOrDefaultAsync(p => 
                            (satisDto.PersonelKeyId != null && p.KeyId == satisDto.PersonelKeyId) || 
                            (p.AdSoyad == satisDto.PersonelAdi) || 
                            (p.OtomasyonAdi == satisDto.PersonelAdi));

                    // Personel bulunamadıysa yeni oluştur
                    if (personel == null && !string.IsNullOrWhiteSpace(satisDto.PersonelAdi))
                    {
                        personel = new Personel
                        {
                            OtomasyonAdi = satisDto.PersonelAdi.Trim(),
                            AdSoyad = satisDto.PersonelAdi.Trim(), // İlk başta aynı, sonra düzenlenebilir
                            KeyId = satisDto.PersonelKeyId,
                            Rol = PersonelRol.POMPACI, // Varsayılan rol
                            Aktif = true
                        };
                        _context.Personeller.Add(personel);
                        await _context.SaveChangesAsync(); // ID oluşması için kaydet
                    }

                    var satis = new OtomasyonSatis
                    {
                        VardiyaId = vardiya.Id,
                        PersonelId = personel?.Id,
                        PersonelAdi = satisDto.PersonelAdi,
                        PersonelKeyId = satisDto.PersonelKeyId,
                        PompaNo = satisDto.PompaNo,
                        YakitTuru = satisDto.YakitTuru,
                        Litre = satisDto.Litre,
                        BirimFiyat = satisDto.BirimFiyat,
                        ToplamTutar = satisDto.ToplamTutar,
                        SatisTarihi = satisDto.SatisTarihi.ToUniversalTime(),
                        FisNo = satisDto.FisNo,
                        Plaka = satisDto.Plaka
                    };
                    _context.OtomasyonSatislar.Add(satis);
                }

                // 3. Filo Satışlarını Ekle
                foreach (var filoDto in dto.FiloSatislar)
                {
                    var filo = new FiloSatis
                    {
                        VardiyaId = vardiya.Id,
                        Tarih = filoDto.Tarih.ToUniversalTime(),
                        FiloKodu = filoDto.FiloKodu,
                        Plaka = filoDto.Plaka,
                        YakitTuru = filoDto.YakitTuru,
                        Litre = filoDto.Litre,
                        Tutar = filoDto.Tutar,
                        PompaNo = filoDto.PompaNo,
                        FisNo = filoDto.FisNo
                    };
                    _context.FiloSatislar.Add(filo);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetById), new { id = vardiya.Id }, vardiya);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // OPTIMIZED: Removed Include() to prevent loading all sales data
            // Now returns only summary counts for faster response
            var vardiyalar = await _context.Vardiyalar
                .AsNoTracking()
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
                    OtomasyonSatisSayisi = _context.OtomasyonSatislar.Count(s => s.VardiyaId == v.Id),
                    FiloSatisSayisi = _context.FiloSatislar.Count(f => f.VardiyaId == v.Id),
                    PusulaSayisi = _context.Pusulalar.Count(p => p.VardiyaId == v.Id)
                })
                .ToListAsync();

            return Ok(vardiyalar);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var vardiya = await _context.Vardiyalar.FindAsync(id);
            if (vardiya == null)
            {
                return NotFound();
            }

            _context.Vardiyalar.Remove(vardiya);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost("{id}/onaya-gonder")]
        public async Task<IActionResult> OnayaGonder(int id)
        {
            var vardiya = await _context.Vardiyalar.FindAsync(id);
            if (vardiya == null) return NotFound();

            if (vardiya.Durum != VardiyaDurum.ACIK && vardiya.Durum != VardiyaDurum.REDDEDILDI)
            {
                return BadRequest("Sadece AÇIK veya REDDEDİLMİŞ vardiyalar onaya gönderilebilir.");
            }

            vardiya.Durum = VardiyaDurum.ONAY_BEKLIYOR;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Vardiya onaya gönderildi." });
        }

        [HttpGet("onay-bekleyenler")]
        public async Task<IActionResult> GetOnayBekleyenler()
        {
            var vardiyalar = await _context.Vardiyalar
                .Where(v => v.Durum == VardiyaDurum.ONAY_BEKLIYOR)
                .OrderByDescending(v => v.BaslangicTarihi)
                .ToListAsync();

            return Ok(vardiyalar);
        }

        [HttpPost("{id}/onayla")]
        public async Task<IActionResult> Onayla(int id, [FromBody] OnayDto dto)
        {
            var vardiya = await _context.Vardiyalar.FindAsync(id);
            if (vardiya == null) return NotFound();

            if (vardiya.Durum != VardiyaDurum.ONAY_BEKLIYOR)
            {
                return BadRequest("Sadece ONAY BEKLEYEN vardiyalar onaylanabilir.");
            }

            vardiya.Durum = VardiyaDurum.ONAYLANDI;
            vardiya.OnaylayanId = dto.OnaylayanId;
            vardiya.OnaylayanAdi = dto.OnaylayanAdi;
            vardiya.OnayTarihi = DateTime.UtcNow;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Vardiya onaylandı." });
        }

        [HttpPost("{id}/reddet")]
        public async Task<IActionResult> Reddet(int id, [FromBody] RedDto dto)
        {
            var vardiya = await _context.Vardiyalar.FindAsync(id);
            if (vardiya == null) return NotFound();

            if (vardiya.Durum != VardiyaDurum.ONAY_BEKLIYOR)
            {
                return BadRequest("Sadece ONAY BEKLEYEN vardiyalar reddedilebilir.");
            }

            vardiya.Durum = VardiyaDurum.REDDEDILDI;
            vardiya.RedNedeni = dto.RedNedeni;
            vardiya.OnaylayanId = dto.OnaylayanId;
            vardiya.OnaylayanAdi = dto.OnaylayanAdi;
            vardiya.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Vardiya reddedildi." });
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

            // 5. Genel özet hesapla
            var toplamNakit = pusulalar.Sum(p => p.Nakit);
            var toplamKrediKarti = pusulalar.Sum(p => p.KrediKarti);
            var toplamParoPuan = pusulalar.Sum(p => p.ParoPuan);
            var toplamMobilOdeme = pusulalar.Sum(p => p.MobilOdeme);
            var pusulaToplami = toplamNakit + toplamKrediKarti + toplamParoPuan + toplamMobilOdeme;
            var toplamFark = pusulaToplami - vardiya.PompaToplam;

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
    }
}
