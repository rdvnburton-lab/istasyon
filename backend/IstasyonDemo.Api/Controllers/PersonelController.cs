using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var personeller = await _context.Personeller
                .OrderBy(p => p.OtomasyonAdi)
                .ToListAsync();

            return Ok(personeller);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var personel = await _context.Personeller.FindAsync(id);
            
            if (personel == null)
                return NotFound();

            return Ok(personel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Personel personel)
        {
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

            var existing = await _context.Personeller.FindAsync(id);
            if (existing == null)
                return NotFound();

            // KeyId benzersizlik kontrolü
            if (!string.IsNullOrEmpty(personel.KeyId))
            {
                var duplicate = await _context.Personeller
                    .FirstOrDefaultAsync(p => p.KeyId == personel.KeyId && p.Id != id);
                
                if (duplicate != null)
                    return BadRequest(new { message = "Bu KeyId zaten kullanılıyor." });
            }

            existing.OtomasyonAdi = personel.OtomasyonAdi;
            existing.AdSoyad = personel.AdSoyad;
            existing.KeyId = personel.KeyId;
            existing.Rol = personel.Rol;
            existing.Aktif = personel.Aktif;

            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var personel = await _context.Personeller.FindAsync(id);
            
            if (personel == null)
                return NotFound();

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
            var personel = await _context.Personeller.FindAsync(id);
            
            if (personel == null)
                return NotFound();

            personel.Aktif = !personel.Aktif;
            await _context.SaveChangesAsync();

            return Ok(personel);
        }
    }
}
