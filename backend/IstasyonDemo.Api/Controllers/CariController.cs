using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CariController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CariController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("list/{istasyonId}")]
        public async Task<IActionResult> GetCariKartlar(int istasyonId)
        {
            var list = await _context.CariKartlar
                .Where(c => c.IstasyonId == istasyonId && c.Aktif)
                .OrderBy(c => c.Ad)
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCariDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                                    .SelectMany(x => x.Errors)
                                    .Select(x => x.ErrorMessage));
                Console.WriteLine($"CreateCari Validation Error: {errors}");
                return BadRequest(errors);
            }

            try 
            {
                var cari = new CariKart
                {
                    IstasyonId = dto.IstasyonId,
                    Ad = dto.Ad,
                    VergiDairesi = dto.VergiDairesi,
                    TckN_VKN = dto.TckN_VKN,
                    Telefon = dto.Telefon,
                    Email = dto.Email,
                    Adres = dto.Adres,
                    Limit = dto.Limit,
                    Kod = dto.Kod,
                    Aktif = true,
                    OlusturmaTarihi = DateTime.UtcNow,
                    Bakiye = 0
                };
                
                _context.CariKartlar.Add(cari);
                await _context.SaveChangesAsync();
                
                return Ok(cari);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateCari Error: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCariDto dto)
        {
            var existing = await _context.CariKartlar.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Ad = dto.Ad;
            existing.VergiDairesi = dto.VergiDairesi;
            existing.TckN_VKN = dto.TckN_VKN;
            existing.Telefon = dto.Telefon;
            existing.Email = dto.Email;
            existing.Adres = dto.Adres;
            existing.Limit = dto.Limit;
            existing.Kod = dto.Kod;
            existing.Aktif = dto.Aktif;
            existing.GuncellemeTarihi = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // ... Existing methods ...

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.CariKartlar.FindAsync(id);
            if (existing == null) return NotFound();

            // Soft delete
            existing.Aktif = false;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{id}/hareketler")]
        public async Task<IActionResult> GetHareketler(int id)
        {
            var list = await _context.CariHareketler
                .Where(h => h.CariKartId == id)
                .OrderByDescending(h => h.Tarih)
                .ThenByDescending(h => h.Id)
                .Take(100) // Son 100 hareket
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("{id}/tahsilat")]
        public async Task<IActionResult> AddTahsilat(int id, [FromBody] TahsilatModel model)
        {
            var cari = await _context.CariKartlar.FindAsync(id);
            if (cari == null) return NotFound();

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var hareket = new CariHareket
                {
                    CariKartId = id,
                    Tarih = DateTime.UtcNow,
                    IslemTipi = "TAHSILAT",
                    Tutar = model.Tutar,
                    Aciklama = model.Aciklama,
                    OlusturanId = 1, // TODO: User ID from claims
                    OlusturmaTarihi = DateTime.UtcNow,
                };

                _context.CariHareketler.Add(hareket);
                
                // Bakiyeyi düş (Tahsilat alacağı artırır, borcu azaltır. Biz bakiyeyi borç olarak tutuyoruz.)
                // Bakiye = Borç - Alacak. Tahsilat = Alacak. Bakiye azalır.
                cari.Bakiye -= model.Tutar;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(hareket);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("hareket/{id}")]
        public async Task<IActionResult> UpdateHareket(int id, [FromBody] TahsilatModel model)
        {
            var hareket = await _context.CariHareketler.FindAsync(id);
            if (hareket == null) return NotFound();

            var cari = await _context.CariKartlar.FindAsync(hareket.CariKartId);
            if (cari == null) return BadRequest("Cari kart bulunamadı");

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Eski tutarı geri al
                if (hareket.IslemTipi == "TAHSILAT")
                {
                    cari.Bakiye += hareket.Tutar; // Borcu geri artır
                }
                // Diğer tipler için de gerekirse logic eklenmeli ama şu an sadece tahsilat düzenletiyoruz

                hareket.Tutar = model.Tutar;
                hareket.Aciklama = model.Aciklama;
                
                // Yeni tutarı işle
                if (hareket.IslemTipi == "TAHSILAT")
                {
                    cari.Bakiye -= model.Tutar; // Borcu tekrar düş
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(hareket);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpDelete("hareket/{id}")]
        public async Task<IActionResult> DeleteHareket(int id)
        {
            var hareket = await _context.CariHareketler.FindAsync(id);
            if (hareket == null) return NotFound();

            var cari = await _context.CariKartlar.FindAsync(hareket.CariKartId);
            
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                if (cari != null)
                {
                    // Bakiyeyi düzelt
                    if (hareket.IslemTipi == "TAHSILAT")
                    {
                        cari.Bakiye += hareket.Tutar; // Tahsilat iptal ise borç artar
                    }
                    else if (hareket.IslemTipi == "SATIS")
                    {
                        cari.Bakiye -= hareket.Tutar; // Satış iptal ise borç azalır
                    }
                }

                _context.CariHareketler.Remove(hareket);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public class TahsilatModel
    {
        public decimal Tutar { get; set; }
        public string? Aciklama { get; set; }
    }

    public class CreateCariDto
    {
        public int IstasyonId { get; set; }
        [Required]
        public string Ad { get; set; } = string.Empty;
        public string? VergiDairesi { get; set; }
        public string? TckN_VKN { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Adres { get; set; }
        public decimal Limit { get; set; }
        public string? Kod { get; set; }
    }

    public class UpdateCariDto
    {
        [Required]
        public string Ad { get; set; } = string.Empty;
        public string? VergiDairesi { get; set; }
        public string? TckN_VKN { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Adres { get; set; }
        public decimal Limit { get; set; }
        public string? Kod { get; set; }
        public bool Aktif { get; set; }
    }
}
