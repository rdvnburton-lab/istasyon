using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MarketVardiyaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MarketVardiyaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MarketVardiyaDto>>> GetMarketVardiyalar()
        {
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(currentUserId);
            
            if (user == null) return Unauthorized();

            IQueryable<MarketVardiya> query = _context.MarketVardiyalar
                .Include(m => m.Istasyon)
                .Include(m => m.Sorumlu);

            if (user.Role != "admin")
            {
                if (user.Role == "patron")
                {
                    query = query.Where(m => m.Istasyon!.PatronId == currentUserId);
                }
                else if (user.Role == "vardiya_sorumlusu")
                {
                    // Vardiya sorumlusu should not see market shifts
                    return Ok(new List<MarketVardiyaDto>());
                }
                else
                {
                    query = query.Where(m => m.IstasyonId == user.IstasyonId);
                }
            }

            var result = await query
                .OrderByDescending(m => m.Tarih)
                .Select(m => new MarketVardiyaDto
                {
                    Id = m.Id,
                    IstasyonId = m.IstasyonId,
                    IstasyonAdi = m.Istasyon!.Ad,
                    SorumluId = m.SorumluId,
                    SorumluAdi = m.Sorumlu!.Username,
                    Tarih = m.Tarih,
                    Durum = m.Durum,
                    ToplamSatisTutari = m.ToplamSatisTutari,
                    ToplamTeslimatTutari = m.ToplamTeslimatTutari,
                    ToplamFark = m.ToplamFark,
                    ZRaporuTutari = m.ZRaporuTutari,
                    ZRaporuNo = m.ZRaporuNo,
                    OlusturmaTarihi = m.OlusturmaTarihi
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<MarketVardiya>> CreateMarketVardiya(CreateMarketVardiyaDto dto)
        {
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(currentUserId);
            
            if (user == null || !user.IstasyonId.HasValue) 
                return BadRequest("Kullanıcı istasyon bilgisi bulunamadı.");

            // Aynı tarihte başka vardiya var mı kontrol et
            var tarih = dto.Tarih.Date;
            var mevcut = await _context.MarketVardiyalar
                .AnyAsync(m => m.IstasyonId == user.IstasyonId.Value && m.Tarih.Date == tarih && m.Durum != VardiyaDurum.SILINDI);

            if (mevcut)
                return BadRequest($"{tarih:dd.MM.yyyy} tarihi için zaten bir market mutabakatı mevcut.");

            var marketVardiya = new MarketVardiya
            {
                IstasyonId = user.IstasyonId.Value,
                SorumluId = currentUserId,
                Tarih = dto.Tarih,
                ZRaporuTutari = dto.ZRaporuTutari,
                ZRaporuNo = dto.ZRaporuNo,
                Durum = VardiyaDurum.ACIK,
                OlusturmaTarihi = DateTime.UtcNow
            };

            // Z Raporları
            foreach (var z in dto.ZRaporlari)
            {
                marketVardiya.ZRaporlari.Add(new MarketZRaporu
                {
                    Tarih = dto.Tarih,
                    GenelToplam = z.GenelToplam,
                    Kdv0 = z.Kdv0,
                    Kdv1 = z.Kdv1,
                    Kdv10 = z.Kdv10,
                    Kdv20 = z.Kdv20,
                    KdvToplam = z.KdvToplam,
                    KdvHaricToplam = z.KdvHaricToplam
                });
            }

            // Tahsilatlar
            foreach (var t in dto.Tahsilatlar)
            {
                marketVardiya.Tahsilatlar.Add(new MarketTahsilat
                {
                    PersonelId = t.PersonelId,
                    Nakit = t.Nakit,
                    KrediKarti = t.KrediKarti,
                    ParoPuan = t.ParoPuan,
                    Toplam = t.Toplam,
                    Aciklama = t.Aciklama
                });
            }

            // Giderler
            foreach (var g in dto.Giderler)
            {
                marketVardiya.Giderler.Add(new MarketGider
                {
                    GiderTuru = g.GiderTuru,
                    Tutar = g.Tutar,
                    Aciklama = g.Aciklama,
                    BelgeTarihi = g.BelgeTarihi
                });
            }

            // Gelirler
            foreach (var g in dto.Gelirler)
            {
                marketVardiya.Gelirler.Add(new MarketGelir
                {
                    GelirTuru = g.GelirTuru,
                    Tutar = g.Tutar,
                    Aciklama = g.Aciklama,
                    BelgeTarihi = g.BelgeTarihi
                });
            }

            // Hesaplamalar
            marketVardiya.ToplamSatisTutari = marketVardiya.ZRaporlari.Sum(z => z.GenelToplam);
            marketVardiya.ToplamTeslimatTutari = marketVardiya.Tahsilatlar.Sum(t => t.Toplam) + marketVardiya.Gelirler.Sum(g => g.Tutar) - marketVardiya.Giderler.Sum(g => g.Tutar);
            marketVardiya.ToplamFark = marketVardiya.ToplamTeslimatTutari - marketVardiya.ToplamSatisTutari;

            _context.MarketVardiyalar.Add(marketVardiya);
            await _context.SaveChangesAsync();

            return Ok(new { id = marketVardiya.Id, message = "Market vardiyası başarıyla kaydedildi." });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetMarketVardiyaDetay(int id)
        {
            var vardiya = await _context.MarketVardiyalar
                .Include(m => m.ZRaporlari)
                .Include(m => m.Tahsilatlar).ThenInclude(t => t.Personel)
                .Include(m => m.Giderler)
                .Include(m => m.Gelirler)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (vardiya == null) return NotFound();

            return Ok(vardiya);
        }

        [HttpPost("{id}/z-raporu")]
        public async Task<ActionResult> SaveZRaporu(int id, MarketZRaporuDto dto)
        {
            var vardiya = await _context.MarketVardiyalar.Include(m => m.ZRaporlari).FirstOrDefaultAsync(m => m.Id == id);
            if (vardiya == null) return NotFound();

            // Mevcut Z raporlarını temizle (genelde bir tane olur ama liste olarak tutuyoruz)
            _context.MarketZRaporlari.RemoveRange(vardiya.ZRaporlari);

            var zRaporu = new MarketZRaporu
            {
                MarketVardiyaId = id,
                Tarih = vardiya.Tarih,
                GenelToplam = dto.GenelToplam,
                Kdv0 = dto.Kdv0,
                Kdv1 = dto.Kdv1,
                Kdv10 = dto.Kdv10,
                Kdv20 = dto.Kdv20,
                KdvToplam = dto.KdvToplam,
                KdvHaricToplam = dto.KdvHaricToplam
            };

            _context.MarketZRaporlari.Add(zRaporu);
            
            // Toplamları güncelle
            await _context.SaveChangesAsync(); // Önce kaydet ki Sum doğru çalışsın veya manuel hesapla
            vardiya.ToplamSatisTutari = dto.GenelToplam;
            vardiya.ToplamFark = vardiya.ToplamTeslimatTutari - vardiya.ToplamSatisTutari;
            
            await _context.SaveChangesAsync();

            return Ok(zRaporu);
        }

        [HttpPost("{id}/tahsilat")]
        public async Task<ActionResult> SaveTahsilat(int id, MarketTahsilatDto dto)
        {
            var vardiya = await _context.MarketVardiyalar.Include(m => m.Tahsilatlar).FirstOrDefaultAsync(m => m.Id == id);
            if (vardiya == null) return NotFound();

            // Aynı personel için mükerrer kaydı önle veya güncelle
            var mevcut = vardiya.Tahsilatlar.FirstOrDefault(t => t.PersonelId == dto.PersonelId);
            if (mevcut != null)
            {
                mevcut.Nakit = dto.Nakit;
                mevcut.KrediKarti = dto.KrediKarti;
                mevcut.ParoPuan = dto.ParoPuan;
                mevcut.SistemSatisTutari = dto.SistemSatisTutari;
                mevcut.Toplam = dto.Toplam;
                mevcut.Aciklama = dto.Aciklama;
            }
            else
            {
                var tahsilat = new MarketTahsilat
                {
                    MarketVardiyaId = id,
                    PersonelId = dto.PersonelId,
                    Nakit = dto.Nakit,
                    KrediKarti = dto.KrediKarti,
                    ParoPuan = dto.ParoPuan,
                    SistemSatisTutari = dto.SistemSatisTutari,
                    Toplam = dto.Toplam,
                    Aciklama = dto.Aciklama
                };
                _context.MarketTahsilatlar.Add(tahsilat);
            }

            await _context.SaveChangesAsync();
            
            // Toplamları güncelle
            vardiya.ToplamTeslimatTutari = await CalculateTotalTeslimat(id);
            vardiya.ToplamFark = vardiya.ToplamTeslimatTutari - vardiya.ToplamSatisTutari;
            
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("{id}/gider")]
        public async Task<ActionResult> AddGider(int id, MarketGiderDto dto)
        {
            var vardiya = await _context.MarketVardiyalar.FindAsync(id);
            if (vardiya == null) return NotFound();

            var gider = new MarketGider
            {
                MarketVardiyaId = id,
                GiderTuru = dto.GiderTuru,
                Tutar = dto.Tutar,
                Aciklama = dto.Aciklama,
                BelgeTarihi = dto.BelgeTarihi
            };

            _context.MarketGiderler.Add(gider);
            await _context.SaveChangesAsync();

            vardiya.ToplamTeslimatTutari = await CalculateTotalTeslimat(id);
            vardiya.ToplamFark = vardiya.ToplamTeslimatTutari - vardiya.ToplamSatisTutari;
            
            await _context.SaveChangesAsync();

            return Ok(gider);
        }

        [HttpDelete("gider/{giderId}")]
        public async Task<ActionResult> DeleteGider(int giderId)
        {
            var gider = await _context.MarketGiderler.FindAsync(giderId);
            if (gider == null) return NotFound();

            var vardiyaId = gider.MarketVardiyaId;
            _context.MarketGiderler.Remove(gider);
            await _context.SaveChangesAsync();

            var vardiya = await _context.MarketVardiyalar.FindAsync(vardiyaId);
            if (vardiya != null)
            {
                vardiya.ToplamTeslimatTutari = await CalculateTotalTeslimat(vardiyaId);
                vardiya.ToplamFark = vardiya.ToplamTeslimatTutari - vardiya.ToplamSatisTutari;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost("{id}/gelir")]
        public async Task<ActionResult> AddGelir(int id, MarketGelirDto dto)
        {
            var vardiya = await _context.MarketVardiyalar.FindAsync(id);
            if (vardiya == null) return NotFound();

            var gelir = new MarketGelir
            {
                MarketVardiyaId = id,
                GelirTuru = dto.GelirTuru,
                Tutar = dto.Tutar,
                Aciklama = dto.Aciklama,
                BelgeTarihi = dto.BelgeTarihi
            };

            _context.MarketGelirler.Add(gelir);
            await _context.SaveChangesAsync();

            vardiya.ToplamTeslimatTutari = await CalculateTotalTeslimat(id);
            vardiya.ToplamFark = vardiya.ToplamTeslimatTutari - vardiya.ToplamSatisTutari;
            
            await _context.SaveChangesAsync();

            return Ok(gelir);
        }

        [HttpDelete("gelir/{gelirId}")]
        public async Task<ActionResult> DeleteGelir(int gelirId)
        {
            var gelir = await _context.MarketGelirler.FindAsync(gelirId);
            if (gelir == null) return NotFound();

            var vardiyaId = gelir.MarketVardiyaId;
            _context.MarketGelirler.Remove(gelir);
            await _context.SaveChangesAsync();

            var vardiya = await _context.MarketVardiyalar.FindAsync(vardiyaId);
            if (vardiya != null)
            {
                vardiya.ToplamTeslimatTutari = await CalculateTotalTeslimat(vardiyaId);
                vardiya.ToplamFark = vardiya.ToplamTeslimatTutari - vardiya.ToplamSatisTutari;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        private async Task<decimal> CalculateTotalTeslimat(int vardiyaId)
        {
            var tahsilat = await _context.MarketTahsilatlar.Where(t => t.MarketVardiyaId == vardiyaId).SumAsync(t => t.Toplam);
            var gelir = await _context.MarketGelirler.Where(g => g.MarketVardiyaId == vardiyaId).SumAsync(g => g.Tutar);
            var gider = await _context.MarketGiderler.Where(g => g.MarketVardiyaId == vardiyaId).SumAsync(g => g.Tutar);
            
            return tahsilat + gelir - gider;
        }

        [HttpGet("rapor")]
        public async Task<IActionResult> GetMarketRaporu([FromQuery] DateTimeOffset baslangic, [FromQuery] DateTimeOffset bitis)
        {
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null) return Unauthorized();

            var start = baslangic.UtcDateTime;
            var end = bitis.UtcDateTime;

            var query = _context.MarketVardiyalar
                .Include(m => m.Istasyon)
                .Where(m => m.Tarih >= start && m.Tarih <= end && m.Durum != VardiyaDurum.SILINDI);

            if (user.Role != "admin")
            {
                if (user.Role == "patron")
                {
                    query = query.Where(m => m.Istasyon!.PatronId == currentUserId);
                }
                else
                {
                    query = query.Where(m => m.IstasyonId == user.IstasyonId);
                }
            }

            var vardiyalar = await query
                .OrderByDescending(m => m.Tarih)
                .Select(m => new
                {
                    m.Id,
                    Tarih = m.Tarih,
                    m.ToplamSatisTutari,
                    m.ToplamTeslimatTutari,
                    m.ToplamFark,
                    Durum = m.Durum.ToString()
                })
                .ToListAsync();

            var ozet = new
            {
                ToplamVardiya = vardiyalar.Count,
                ToplamSatis = vardiyalar.Sum(v => v.ToplamSatisTutari),
                ToplamTeslimat = vardiyalar.Sum(v => v.ToplamTeslimatTutari),
                ToplamFark = vardiyalar.Sum(v => v.ToplamFark)
            };

            return Ok(new { Ozet = ozet, Vardiyalar = vardiyalar });
        }

        [HttpPost("{id}/onaya-gonder")]
        public async Task<ActionResult> OnayaGonder(int id)
        {
            var vardiya = await _context.MarketVardiyalar.FindAsync(id);
            if (vardiya == null) return NotFound();

            vardiya.Durum = VardiyaDurum.ONAY_BEKLIYOR;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Market mutabakatı onaya gönderildi." });
        }

        [HttpPost("{id}/onayla")]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult> Onayla(int id)
        {
            var vardiya = await _context.MarketVardiyalar.FindAsync(id);
            if (vardiya == null) return NotFound();

            vardiya.Durum = VardiyaDurum.ONAYLANDI;
            vardiya.OnaylayanId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            vardiya.OnayTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Vardiya onaylandı." });
        }

        [HttpPost("{id}/reddet")]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult> Reddet(int id, [FromBody] string neden)
        {
            var vardiya = await _context.MarketVardiyalar.FindAsync(id);
            if (vardiya == null) return NotFound();

            vardiya.Durum = VardiyaDurum.REDDEDILDI;
            vardiya.RedNedeni = neden;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Vardiya reddedildi." });
        }
    }
}

