using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/vardiya/{vardiyaId}/[controller]")]
    [Authorize]
    public class PusulaController : BaseController
    {
        private readonly AppDbContext _context;

        public PusulaController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<bool> CheckVardiyaAccess(int vardiyaId)
        {
            if (IsAdmin) return true;

            var vardiya = await _context.Vardiyalar
                .Include(v => v.Istasyon).ThenInclude(i => i!.Firma)
                .FirstOrDefaultAsync(v => v.Id == vardiyaId);

            if (vardiya == null) return false;

            if (IsPatron)
            {
                return vardiya.Istasyon?.Firma?.PatronId == CurrentUserId;
            }

            return vardiya.IstasyonId == CurrentIstasyonId;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int vardiyaId)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var pusulalar = await _context.Pusulalar
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.KrediKartiDetaylari)
                .Include(p => p.DigerOdemeler)
                .Where(p => p.VardiyaId == vardiyaId)
                .OrderBy(p => p.PersonelAdi)
                .ToListAsync();

            return Ok(pusulalar);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int vardiyaId, int id)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var pusula = await _context.Pusulalar
                .AsSplitQuery()
                .Include(p => p.KrediKartiDetaylari)
                .Include(p => p.DigerOdemeler)
                .FirstOrDefaultAsync(p => p.Id == id && p.VardiyaId == vardiyaId);

            if (pusula == null)
                return NotFound();

            return Ok(pusula);
        }

        [HttpGet("personel/{personelAdi}")]
        public async Task<IActionResult> GetByPersonel(int vardiyaId, string personelAdi)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var pusula = await _context.Pusulalar
                .AsSplitQuery()
                .Include(p => p.KrediKartiDetaylari)
                .Include(p => p.DigerOdemeler)
                .FirstOrDefaultAsync(p => p.VardiyaId == vardiyaId && p.PersonelAdi == personelAdi);

            if (pusula == null)
                return NotFound();

            return Ok(pusula);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int vardiyaId, CreatePusulaDto dto)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var vardiya = await _context.Vardiyalar.FindAsync(vardiyaId);
            if (vardiya == null)
                return NotFound(new { message = "Vardiya bulunamadı" });

            // Aynı personel için pusula kontrolü
            var existing = await _context.Pusulalar
                .FirstOrDefaultAsync(p => p.VardiyaId == vardiyaId && p.PersonelAdi == dto.PersonelAdi);

            if (existing != null)
                return BadRequest(new { message = "Bu personel için zaten pusula girilmiş" });

            var pusula = new Pusula
            {
                VardiyaId = vardiyaId,
                PersonelAdi = dto.PersonelAdi,
                PersonelId = dto.PersonelId,
                Nakit = dto.Nakit,
                KrediKarti = dto.KrediKarti,

                KrediKartiDetay = dto.KrediKartiDetay,
                Aciklama = dto.Aciklama,
                PusulaTuru = dto.PusulaTuru ?? "TAHSILAT",
                OlusturmaTarihi = DateTime.UtcNow
            };

            _context.Pusulalar.Add(pusula);
            await _context.SaveChangesAsync();

            // Kredi Kartı Detaylarını İlişkisel Tabloya Kaydet
            if (dto.KrediKartiDetayList != null && dto.KrediKartiDetayList.Any())
            {
                foreach (var detay in dto.KrediKartiDetayList)
                {
                    _context.PusulaKrediKartiDetaylari.Add(new PusulaKrediKartiDetay
                    {
                        PusulaId = pusula.Id,
                        BankaAdi = detay.BankaAdi,
                        Tutar = detay.Tutar
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Diğer Ödeme Detaylarını Kaydet
            if (dto.DigerOdemeList != null && dto.DigerOdemeList.Any())
            {
                foreach (var detay in dto.DigerOdemeList)
                {
                    _context.PusulaDigerOdemeleri.Add(new PusulaDigerOdeme
                    {
                        PusulaId = pusula.Id,
                        TurKodu = detay.TurKodu,
                        TurAdi = detay.TurAdi,
                        Tutar = detay.Tutar,
                        Silinemez = detay.Silinemez
                    });
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetById), new { vardiyaId, id = pusula.Id }, pusula);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int vardiyaId, int id, UpdatePusulaDto dto)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var pusula = await _context.Pusulalar
                .FirstOrDefaultAsync(p => p.Id == id && p.VardiyaId == vardiyaId);

            if (pusula == null)
                return NotFound();

            pusula.Nakit = dto.Nakit;
            pusula.KrediKarti = dto.KrediKarti;

            pusula.KrediKartiDetay = dto.KrediKartiDetay;
            pusula.Aciklama = dto.Aciklama;
            pusula.PusulaTuru = dto.PusulaTuru ?? "TAHSILAT";
            pusula.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Kredi Kartı Detaylarını Güncelle (Sil ve Yeniden Ekle)
            if (dto.KrediKartiDetayList != null)
            {
                var existingDetails = await _context.PusulaKrediKartiDetaylari
                    .Where(pk => pk.PusulaId == id)
                    .ToListAsync();
                
                _context.PusulaKrediKartiDetaylari.RemoveRange(existingDetails);

                foreach (var detay in dto.KrediKartiDetayList)
                {
                    _context.PusulaKrediKartiDetaylari.Add(new PusulaKrediKartiDetay
                    {
                        PusulaId = pusula.Id,
                        BankaAdi = detay.BankaAdi,
                        Tutar = detay.Tutar
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Diğer Ödeme Detaylarını Güncelle
            if (dto.DigerOdemeList != null)
            {
                var existingOtherPayments = await _context.PusulaDigerOdemeleri
                    .Where(p => p.PusulaId == id)
                    .ToListAsync();

                // Güvenlik Kontrolü: Silinemez işaretli kayıtlar listeden çıkarılmış mı?
                var protectedItems = existingOtherPayments.Where(x => x.Silinemez).ToList();
                var incomingTurKodlari = dto.DigerOdemeList.Select(x => x.TurKodu).ToHashSet();

                foreach (var protectedItem in protectedItems)
                {
                    if (!incomingTurKodlari.Contains(protectedItem.TurKodu))
                    {
                        return BadRequest(new { message = $"Otomatik oluşturulan '{protectedItem.TurAdi}' kaydı silinemez!" });
                    }
                }

                _context.PusulaDigerOdemeleri.RemoveRange(existingOtherPayments);

                foreach (var detay in dto.DigerOdemeList)
                {
                    _context.PusulaDigerOdemeleri.Add(new PusulaDigerOdeme
                    {
                        PusulaId = pusula.Id,
                        TurKodu = detay.TurKodu,
                        TurAdi = detay.TurAdi,
                        Tutar = detay.Tutar,
                        Silinemez = detay.Silinemez
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(pusula);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int vardiyaId, int id)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var pusula = await _context.Pusulalar
                .FirstOrDefaultAsync(p => p.Id == id && p.VardiyaId == vardiyaId);

            if (pusula == null)
                return NotFound();

            _context.Pusulalar.Remove(pusula);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("ozet")]
        public async Task<IActionResult> GetOzet(int vardiyaId)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var vardiya = await _context.Vardiyalar.FindAsync(vardiyaId);
            if (vardiya == null)
                return NotFound(new { message = "Vardiya bulunamadı" });

            var pusulalar = await _context.Pusulalar
                .Where(p => p.VardiyaId == vardiyaId)
                .ToListAsync();

            var ozet = new
            {
                toplamPusula = pusulalar.Count,
                toplamNakit = pusulalar.Sum(p => p.Nakit),
                toplamKrediKarti = pusulalar.Sum(p => p.KrediKarti),
                genelToplam = pusulalar.Sum(p => p.Toplam)
            };

            return Ok(ozet);
        }
    }
}
