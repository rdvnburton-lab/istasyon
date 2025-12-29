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
    public class FirmaController : BaseController
    {
        private readonly AppDbContext _context;

        public FirmaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FirmaDto>>> GetFirmalar()
        {
            IQueryable<Firma> query = _context.Firmalar;

            if (IsAdmin)
            {
                // Admin sees all
            }
            else if (IsPatron)
            {
                query = query.Where(f => f.PatronId == CurrentUserId);
            }
            else
            {
                // Regular users see the firma of their station
                var user = await _context.Users.Include(u => u.Istasyon).FirstOrDefaultAsync(u => u.Id == CurrentUserId);
                if (user?.Istasyon != null)
                {
                    query = query.Where(f => f.Id == user.Istasyon.FirmaId);
                }
                else
                {
                    return Ok(new List<FirmaDto>());
                }
            }

            var firmalar = await query.Select(f => new FirmaDto
            {
                Id = f.Id,
                Ad = f.Ad,
                Adres = f.Adres,
                Aktif = f.Aktif,
                PatronId = f.PatronId
            }).ToListAsync();

            return Ok(firmalar);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<FirmaDto>> CreateFirma(CreateFirmaDto request)
        {
            var firma = new Firma
            {
                Ad = request.Ad,
                Adres = request.Adres,
                PatronId = request.PatronId,
                Aktif = true
            };

            _context.Firmalar.Add(firma);
            await _context.SaveChangesAsync();

            return Ok(new FirmaDto
            {
                Id = firma.Id,
                Ad = firma.Ad,
                Adres = firma.Adres,
                Aktif = firma.Aktif,
                PatronId = firma.PatronId
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateFirma(int id, UpdateFirmaDto request)
        {
            var firma = await _context.Firmalar.FindAsync(id);

            if (firma == null)
            {
                return NotFound();
            }

            firma.Ad = request.Ad;
            firma.Adres = request.Adres;
            firma.Aktif = request.Aktif;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteFirma(int id)
        {
            var firma = await _context.Firmalar.FindAsync(id);

            if (firma == null)
            {
                return NotFound();
            }

            _context.Firmalar.Remove(firma);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
