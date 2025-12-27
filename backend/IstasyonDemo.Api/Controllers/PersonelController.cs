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
    public class PersonelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PersonelController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Personel> query = _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma);

            if (userRole == "admin")
            {
                // Admin sees all
            }
            else if (userRole == "patron")
            {
                query = query.Where(p => p.Istasyon != null && p.Istasyon.Firma != null && p.Istasyon.Firma.PatronId == userId);
            }
            else
            {
                // Vardiya Sorumlusu sees their station's personnel
                var user = await _context.Users.FindAsync(userId);
                if (user?.IstasyonId != null)
                {
                    query = query.Where(p => p.IstasyonId == user.IstasyonId);
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
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var personel = await _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(p => p.Id == id);
            
            if (personel == null)
                return NotFound();

            // Authorization Check
            if (userRole == "patron" && (personel.Istasyon == null || personel.Istasyon.Firma == null || personel.Istasyon.Firma.PatronId != userId)) return Forbid();
            if (userRole != "admin" && userRole != "patron")
            {
                var user = await _context.Users.FindAsync(userId);
                if (user?.IstasyonId != personel.IstasyonId) return Forbid();
            }

            return Ok(personel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Personel personel)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Authorization
            // Authorization
            if (userRole == "patron")
            {
                var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == personel.IstasyonId);
                if (istasyon == null || istasyon.Firma == null || istasyon.Firma.PatronId != userId)
                {
                    return BadRequest("Geçersiz istasyon. Bu istasyonun sahibi değilsiniz.");
                }
            }
            else if (userRole != "admin")
            {
                // Vardiya Sorumlusu etc.
                var user = await _context.Users.FindAsync(userId);
                if (user?.IstasyonId == null)
                {
                    return Forbid("Bir istasyona bağlı değilsiniz.");
                }
                // Force IstasyonId
                personel.IstasyonId = user.IstasyonId.Value;
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

            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var existing = await _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
                return NotFound();

            // Authorization Check
            if (userRole == "patron" && (existing.Istasyon == null || existing.Istasyon.Firma == null || existing.Istasyon.Firma.PatronId != userId)) return Forbid();
            if (userRole != "admin" && userRole != "patron")
            {
                // Vardiya Sorumlusu can update names
                var user = await _context.Users.FindAsync(userId);
                if (user?.IstasyonId != existing.IstasyonId) return Forbid();
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
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var personel = await _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(p => p.Id == id);
            
            if (personel == null)
                return NotFound();

            // Authorization Check
            if (userRole == "patron" && (personel.Istasyon == null || personel.Istasyon.Firma == null || personel.Istasyon.Firma.PatronId != userId)) return Forbid();

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
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var personel = await _context.Personeller.Include(p => p.Istasyon).ThenInclude(i => i!.Firma).FirstOrDefaultAsync(p => p.Id == id);
            
            if (personel == null)
                return NotFound();

            // Authorization Check
            if (userRole == "patron" && (personel.Istasyon == null || personel.Istasyon.Firma == null || personel.Istasyon.Firma.PatronId != userId)) return Forbid();
            if (userRole != "admin" && userRole != "patron")
            {
                var user = await _context.Users.FindAsync(userId);
                if (user?.IstasyonId != personel.IstasyonId) return Forbid();
            }

            personel.Aktif = !personel.Aktif;
            await _context.SaveChangesAsync();

            return Ok(personel);
        }
    }
}
