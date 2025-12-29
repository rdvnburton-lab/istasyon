using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PersonelController : BaseController
    {
        private readonly AppDbContext _context;

        public PersonelController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IQueryable<Personel> query = _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma);

            if (IsAdmin)
            {
                // Admin sees all
            }
            else if (IsPatron)
            {
                query = query.Where(p => p.Istasyon != null && p.Istasyon.Firma != null && p.Istasyon.Firma.PatronId == CurrentUserId);
            }
            else
            {
                // Vardiya Sorumlusu sees their station's personnel
                if (CurrentIstasyonId.HasValue)
                {
                    query = query.Where(p => p.IstasyonId == CurrentIstasyonId.Value);
                }
                else
                {
                    return Ok(new List<Personel>());
                }
            }

            var personeller = await query.OrderBy(p => p.OtomasyonAdi).ToListAsync();
            return Ok(personeller);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var personel = await _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(p => p.Id == id);
            
            if (personel == null)
                return NotFound();

            // Authorization Check
            if (IsPatron && (personel.Istasyon == null || personel.Istasyon.Firma == null || personel.Istasyon.Firma.PatronId != CurrentUserId)) return Forbid();
            if (!IsAdmin && !IsPatron)
            {
                if (CurrentIstasyonId != personel.IstasyonId) return Forbid();
            }

            return Ok(personel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Personel personel)
        {
            // Authorization
            if (IsPatron)
            {
                var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == personel.IstasyonId);
                if (istasyon == null || istasyon.Firma == null || istasyon.Firma.PatronId != CurrentUserId)
                {
                    return BadRequest("Geçersiz istasyon. Bu istasyonun sahibi değilsiniz.");
                }
            }
            else if (!IsAdmin)
            {
                // Vardiya Sorumlusu etc.
                if (!CurrentIstasyonId.HasValue)
                {
                    return Forbid("Bir istasyona bağlı değilsiniz.");
                }
                // Force IstasyonId
                personel.IstasyonId = CurrentIstasyonId.Value;
            }

            // KeyId benzersizlik kontrolü
            if (!string.IsNullOrEmpty(personel.KeyId))
            {
                var existing = await _context.Personeller
                    .FirstOrDefaultAsync(p => p.KeyId == personel.KeyId && p.Id != personel.Id);
                
                if (existing != null)
                    return BadRequest(new { message = "Bu KeyId zaten kullanılıyor." });
            }

            _context.Personeller.Add(personel);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = personel.Id }, personel);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Personel personel)
        {
            if (id != personel.Id)
                return BadRequest();

            var existing = await _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
                return NotFound();

            // Authorization Check
            if (IsPatron && (existing.Istasyon == null || existing.Istasyon.Firma == null || existing.Istasyon.Firma.PatronId != CurrentUserId)) return Forbid();
            if (!IsAdmin && !IsPatron)
            {
                // Vardiya Sorumlusu can update names
                if (CurrentIstasyonId != existing.IstasyonId) return Forbid();
            }

            // KeyId benzersizlik kontrolü
            if (!string.IsNullOrEmpty(personel.KeyId))
            {
                var duplicate = await _context.Personeller
                    .FirstOrDefaultAsync(p => p.KeyId == personel.KeyId && p.Id != id);
                
                if (duplicate != null)
                    return BadRequest(new { message = "Bu KeyId zaten kullanılıyor." });
            }

            // Update allowed fields
            existing.OtomasyonAdi = personel.OtomasyonAdi;
            existing.AdSoyad = personel.AdSoyad;
            existing.KeyId = personel.KeyId;
            existing.Rol = personel.Rol;
            existing.Aktif = personel.Aktif;
            existing.Telefon = personel.Telefon;
            // IstasyonId change logic if needed, usually not allowed for Vardiya Sorumlusu

            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> Delete(int id)
        {
            var personel = await _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(p => p.Id == id);
            
            if (personel == null)
                return NotFound();

            // Authorization Check
            if (IsPatron && (personel.Istasyon == null || personel.Istasyon.Firma == null || personel.Istasyon.Firma.PatronId != CurrentUserId)) return Forbid();

            // Satışları olan personeli silmeyi engelle
            var hasSales = await _context.OtomasyonSatislar
                .AnyAsync(s => s.PersonelId == id);

            if (hasSales)
                return BadRequest(new { message = "Bu personelin satış kayıtları bulunduğu için silinemez. Pasif yapabilirsiniz." });

            _context.Personeller.Remove(personel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/toggle-aktif")]
        public async Task<IActionResult> ToggleAktif(int id)
        {
            var personel = await _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(p => p.Id == id);
            
            if (personel == null)
                return NotFound();

            // Authorization Check
            if (IsPatron && (personel.Istasyon == null || personel.Istasyon.Firma == null || personel.Istasyon.Firma.PatronId != CurrentUserId)) return Forbid();
            if (!IsAdmin && !IsPatron)
            {
                if (CurrentIstasyonId != personel.IstasyonId) return Forbid();
            }

            personel.Aktif = !personel.Aktif;
            await _context.SaveChangesAsync();

            return Ok(personel);
        }
    }
}
