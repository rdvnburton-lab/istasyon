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

            IQueryable<Istasyon> query = _context.Istasyonlar
                .Include(i => i.IstasyonSorumlu).ThenInclude(s => s!.Role)
                .Include(i => i.VardiyaSorumlu).ThenInclude(s => s!.Role)
                .Include(i => i.MarketSorumlu).ThenInclude(s => s!.Role);

            if (userRole == "admin")
            {
                // Admin sees all
            }
            else if (userRole == "patron")
            {
                // Patron sees stations belonging to their firms
                query = query.Where(i => i.Firma.PatronId == userId);
            }
            else
            {
                // Regular users see their assigned station
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
                FirmaId = i.FirmaId,
                
                // 3 ayrı sorumlu ID
                IstasyonSorumluId = i.IstasyonSorumluId,
                VardiyaSorumluId = i.VardiyaSorumluId,
                MarketSorumluId = i.MarketSorumluId,
                
                ApiKey = userRole == "admin" ? i.ApiKey : null,
                
                // Her sorumlu sadece kendi kolonunda görünsün
                IstasyonSorumlusu = i.IstasyonSorumlu != null 
                    ? (i.IstasyonSorumlu.AdSoyad ?? i.IstasyonSorumlu.Username) 
                    : null,
                    
                VardiyaSorumlusu = i.VardiyaSorumlu != null 
                    ? (i.VardiyaSorumlu.AdSoyad ?? i.VardiyaSorumlu.Username)
                    : null,
                    
                MarketSorumlusu = i.MarketSorumlu != null 
                    ? (i.MarketSorumlu.AdSoyad ?? i.MarketSorumlu.Username)
                    : null
            }).ToListAsync();

            return Ok(istasyonlar);
        }

        [HttpPost]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult<IstasyonDto>> CreateIstasyon(CreateIstasyonDto request)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Ensure the firma exists and the user has access to it
            var firma = await _context.Firmalar.FindAsync(request.FirmaId);
            if (firma == null)
            {
                return BadRequest("Geçersiz firma.");
            }

            if (userRole == "patron" && firma.PatronId != userId)
            {
                return Forbid();
            }

            // Sorumluların başka istasyonda olup olmadığını kontrol et
            var sorumluHatalari = new List<string>();
            
            if (request.IstasyonSorumluId.HasValue)
            {
                var mevcutIstasyon = await _context.Istasyonlar
                    .AnyAsync(i => i.IstasyonSorumluId == request.IstasyonSorumluId || 
                                   i.VardiyaSorumluId == request.IstasyonSorumluId || 
                                   i.MarketSorumluId == request.IstasyonSorumluId);
                if (mevcutIstasyon)
                    sorumluHatalari.Add("İstasyon sorumlusu başka bir istasyonda zaten görevli.");
            }
            
            if (request.VardiyaSorumluId.HasValue)
            {
                var mevcutIstasyon = await _context.Istasyonlar
                    .AnyAsync(i => i.IstasyonSorumluId == request.VardiyaSorumluId || 
                                   i.VardiyaSorumluId == request.VardiyaSorumluId || 
                                   i.MarketSorumluId == request.VardiyaSorumluId);
                if (mevcutIstasyon)
                    sorumluHatalari.Add("Vardiya sorumlusu başka bir istasyonda zaten görevli.");
            }
            
            if (request.MarketSorumluId.HasValue)
            {
                var mevcutIstasyon = await _context.Istasyonlar
                    .AnyAsync(i => i.IstasyonSorumluId == request.MarketSorumluId || 
                                   i.VardiyaSorumluId == request.MarketSorumluId || 
                                   i.MarketSorumluId == request.MarketSorumluId);
                if (mevcutIstasyon)
                    sorumluHatalari.Add("Market sorumlusu başka bir istasyonda zaten görevli.");
            }
            
            if (sorumluHatalari.Any())
            {
                return BadRequest(string.Join(" ", sorumluHatalari));
            }

            var istasyon = new Istasyon
            {
                Ad = request.Ad,
                Adres = request.Adres,
                FirmaId = request.FirmaId,
                Aktif = true,
                IstasyonSorumluId = request.IstasyonSorumluId,
                VardiyaSorumluId = request.VardiyaSorumluId,
                MarketSorumluId = request.MarketSorumluId,
                ApiKey = userRole == "admin" ? request.ApiKey : null
            };

            _context.Istasyonlar.Add(istasyon);
            await _context.SaveChangesAsync();

            return Ok(new IstasyonDto
            {
                Id = istasyon.Id,
                Ad = istasyon.Ad,
                Adres = istasyon.Adres,
                Aktif = istasyon.Aktif,
                FirmaId = istasyon.FirmaId,
                IstasyonSorumluId = istasyon.IstasyonSorumluId,
                VardiyaSorumluId = istasyon.VardiyaSorumluId,
                MarketSorumluId = istasyon.MarketSorumluId,
                ApiKey = userRole == "admin" ? istasyon.ApiKey : null
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> UpdateIstasyon(int id, UpdateIstasyonDto request)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == id);

            if (istasyon == null)
            {
                return NotFound();
            }

            if (userRole == "patron" && istasyon.Firma.PatronId != userId)
            {
                return Forbid();
            }

            // Sorumluların başka istasyonda olup olmadığını kontrol et (kendi istasyonu hariç)
            var sorumluHatalari = new List<string>();
            
            if (request.IstasyonSorumluId.HasValue)
            {
                var mevcutIstasyon = await _context.Istasyonlar
                    .AnyAsync(i => i.Id != id && 
                                   (i.IstasyonSorumluId == request.IstasyonSorumluId || 
                                    i.VardiyaSorumluId == request.IstasyonSorumluId || 
                                    i.MarketSorumluId == request.IstasyonSorumluId));
                if (mevcutIstasyon)
                    sorumluHatalari.Add("İstasyon sorumlusu başka bir istasyonda zaten görevli.");
            }
            
            if (request.VardiyaSorumluId.HasValue)
            {
                var mevcutIstasyon = await _context.Istasyonlar
                    .AnyAsync(i => i.Id != id && 
                                   (i.IstasyonSorumluId == request.VardiyaSorumluId || 
                                    i.VardiyaSorumluId == request.VardiyaSorumluId || 
                                    i.MarketSorumluId == request.VardiyaSorumluId));
                if (mevcutIstasyon)
                    sorumluHatalari.Add("Vardiya sorumlusu başka bir istasyonda zaten görevli.");
            }
            
            if (request.MarketSorumluId.HasValue)
            {
                var mevcutIstasyon = await _context.Istasyonlar
                    .AnyAsync(i => i.Id != id && 
                                   (i.IstasyonSorumluId == request.MarketSorumluId || 
                                    i.VardiyaSorumluId == request.MarketSorumluId || 
                                    i.MarketSorumluId == request.MarketSorumluId));
                if (mevcutIstasyon)
                    sorumluHatalari.Add("Market sorumlusu başka bir istasyonda zaten görevli.");
            }
            
            if (sorumluHatalari.Any())
            {
                return BadRequest(string.Join(" ", sorumluHatalari));
            }

            istasyon.Ad = request.Ad;
            istasyon.Adres = request.Adres;
            istasyon.Aktif = request.Aktif;
            istasyon.IstasyonSorumluId = request.IstasyonSorumluId;
            istasyon.VardiyaSorumluId = request.VardiyaSorumluId;
            istasyon.MarketSorumluId = request.MarketSorumluId;
            if (userRole == "admin")
            {
                istasyon.ApiKey = request.ApiKey;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> DeleteIstasyon(int id)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == id);

            if (istasyon == null)
            {
                return NotFound();
            }

            if (userRole == "patron" && istasyon.Firma.PatronId != userId)
            {
                return Forbid();
            }

            _context.Istasyonlar.Remove(istasyon);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
