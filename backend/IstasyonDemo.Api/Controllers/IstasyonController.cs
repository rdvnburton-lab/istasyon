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
    public class IstasyonController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IstasyonController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IstasyonDto>>> GetIstasyonlar()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Istasyon> query = _context.Istasyonlar;

            if (userRole == "admin")
            {
                // Admin sees all
            }
            else if (userRole == "patron")
            {
                // Patron sees stations they own OR stations where they are assigned (if any logic exists for that)
                // Assuming PatronId is linked to User.Id
                query = query.Where(i => i.PatronId == userId || (i.ParentIstasyon != null && i.ParentIstasyon.PatronId == userId));
            }
            else
            {
                // Regular users (Vardiya Sorumlusu, Pompaci) see their assigned station
                var user = await _context.Users.FindAsync(userId);
                if (user?.IstasyonId != null)
                {
                    query = query.Where(i => i.Id == user.IstasyonId);
                }
                else
                {
                    return Ok(new List<IstasyonDto>());
                }
            }

            var istasyonlar = await query.Select(i => new IstasyonDto
            {
                Id = i.Id,
                Ad = i.Ad,
                Adres = i.Adres,
                Aktif = i.Aktif,
                ParentIstasyonId = i.ParentIstasyonId,
                PatronId = i.PatronId,
                SorumluId = i.SorumluId
            }).ToListAsync();

            return Ok(istasyonlar);
        }

        [HttpPost]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult<IstasyonDto>> CreateIstasyon(CreateIstasyonDto request)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // If Patron, ensure they are creating a sub-station or they are setting themselves as owner?
            // For simplicity, if Patron creates a station, they are the owner.
            
            var istasyon = new Istasyon
            {
                Ad = request.Ad,
                Adres = request.Adres,
                ParentIstasyonId = request.ParentIstasyonId,
                Aktif = true,
                SorumluId = request.SorumluId
            };

            if (userRole == "patron")
            {
                if (!request.ParentIstasyonId.HasValue)
                {
                    return BadRequest("Patronlar ana istasyon oluşturamaz, sadece mevcut istasyonlarına alt istasyon ekleyebilir.");
                }

                istasyon.PatronId = userId;
                
                // If ParentIstasyonId is provided, ensure Patron owns the parent
                if (request.ParentIstasyonId.HasValue)
                {
                    var parent = await _context.Istasyonlar.FindAsync(request.ParentIstasyonId.Value);
                    if (parent == null || parent.PatronId != userId)
                    {
                        return BadRequest("Geçersiz üst istasyon. Bu istasyonun sahibi değilsiniz.");
                    }
                }
            }
            else if (userRole == "admin")
            {
                // Admin can assign a patron
                if (request.PatronId.HasValue)
                {
                    istasyon.PatronId = request.PatronId;
                }
            }

            _context.Istasyonlar.Add(istasyon);
            await _context.SaveChangesAsync();

            return Ok(new IstasyonDto
            {
                Id = istasyon.Id,
                Ad = istasyon.Ad,
                Adres = istasyon.Adres,
                Aktif = istasyon.Aktif,
                ParentIstasyonId = istasyon.ParentIstasyonId,
                PatronId = istasyon.PatronId,
                SorumluId = istasyon.SorumluId
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> UpdateIstasyon(int id, UpdateIstasyonDto request)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var istasyon = await _context.Istasyonlar.FindAsync(id);

            if (istasyon == null)
            {
                return NotFound();
            }

            if (userRole == "patron" && istasyon.PatronId != userId)
            {
                return Forbid();
            }

            istasyon.Ad = request.Ad;
            istasyon.Adres = request.Adres;
            istasyon.Aktif = request.Aktif;
            istasyon.SorumluId = request.SorumluId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> DeleteIstasyon(int id)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var istasyon = await _context.Istasyonlar.FindAsync(id);

            if (istasyon == null)
            {
                return NotFound();
            }

            if (userRole == "patron" && istasyon.PatronId != userId)
            {
                return Forbid();
            }

            _context.Istasyonlar.Remove(istasyon);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
