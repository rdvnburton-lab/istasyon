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
    }
}
