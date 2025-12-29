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
    public class IstasyonController : BaseController
    {
        private readonly AppDbContext _context;

        public IstasyonController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IstasyonDto>>> GetIstasyonlar([FromQuery] int? firmaId = null)
        {
            IQueryable<Istasyon> query = _context.Istasyonlar
                .Include(i => i.Firma)
                .Include(i => i.IstasyonSorumlu).ThenInclude(s => s!.Role)
                .Include(i => i.VardiyaSorumlu).ThenInclude(s => s!.Role)
                .Include(i => i.MarketSorumlu).ThenInclude(s => s!.Role);

            if (IsAdmin)
            {
                // Admin sees all, optionally filtered by firmaId
                if (firmaId.HasValue)
                {
                    query = query.Where(i => i.FirmaId == firmaId.Value);
                }
            }
            else if (IsPatron)
            {
                // Patron sees stations belonging to their firms
                query = query.Where(i => i.Firma != null && i.Firma.PatronId == CurrentUserId);
                
                if (firmaId.HasValue)
                {
                    query = query.Where(i => i.FirmaId == firmaId.Value);
                }
            }
            else
            {
                // Regular users see their assigned station
                if (CurrentIstasyonId != null)
                {
                    query = query.Where(i => i.Id == CurrentIstasyonId);
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
                
                ApiKey = (IsAdmin || IsPatron) ? i.ApiKey : null,
                
                // Her sorumlu sadece kendi kolonunda görünsün (Pasif kullanıcılar görünmesin)
                IstasyonSorumlusu = (i.IstasyonSorumlu != null && i.IstasyonSorumlu.Role != null && i.IstasyonSorumlu.Role.Ad.ToLower() != "pasif")
                    ? (i.IstasyonSorumlu.AdSoyad ?? i.IstasyonSorumlu.Username) 
                    : null,
                    
                VardiyaSorumlusu = (i.VardiyaSorumlu != null && i.VardiyaSorumlu.Role != null && i.VardiyaSorumlu.Role.Ad.ToLower() != "pasif")
                    ? (i.VardiyaSorumlu.AdSoyad ?? i.VardiyaSorumlu.Username)
                    : null,
                    
                MarketSorumlusu = (i.MarketSorumlu != null && i.MarketSorumlu.Role != null && i.MarketSorumlu.Role.Ad.ToLower() != "pasif")
                    ? (i.MarketSorumlu.AdSoyad ?? i.MarketSorumlu.Username)
                    : null,
                    
                RegisteredDeviceId = i.RegisteredDeviceId,
                LastConnectionTime = i.LastConnectionTime
            }).ToListAsync();

            return Ok(istasyonlar);
        }

        [HttpPost]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult<IstasyonDto>> CreateIstasyon(CreateIstasyonDto request)
        {
            // Ensure the firma exists and the user has access to it
            var firma = await _context.Firmalar.FindAsync(request.FirmaId);
            if (firma == null)
            {
                return BadRequest("Geçersiz firma.");
            }

            if (IsPatron && firma.PatronId != CurrentUserId)
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
                ApiKey = (IsAdmin || IsPatron) ? request.ApiKey : null
            };

            _context.Istasyonlar.Add(istasyon);
            await _context.SaveChangesAsync();

            // Update Users with new IstasyonId
            bool userUpdated = false;
            if (istasyon.IstasyonSorumluId.HasValue)
            {
                var user = await _context.Users.FindAsync(istasyon.IstasyonSorumluId.Value);
                if (user != null) 
                {
                    user.IstasyonId = istasyon.Id;
                    userUpdated = true;
                }
            }
            if (istasyon.VardiyaSorumluId.HasValue)
            {
                var user = await _context.Users.FindAsync(istasyon.VardiyaSorumluId.Value);
                if (user != null) 
                {
                    user.IstasyonId = istasyon.Id;
                    userUpdated = true;
                }
            }
            if (istasyon.MarketSorumluId.HasValue)
            {
                var user = await _context.Users.FindAsync(istasyon.MarketSorumluId.Value);
                if (user != null) 
                {
                    user.IstasyonId = istasyon.Id;
                    userUpdated = true;
                }
            }
            
            if (userUpdated)
            {
                await _context.SaveChangesAsync();
            }

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
                ApiKey = (IsAdmin || IsPatron) ? istasyon.ApiKey : null
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> UpdateIstasyon(int id, UpdateIstasyonDto request)
        {
            var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == id);

            if (istasyon == null)
            {
                return NotFound();
            }

            if (IsPatron && (istasyon.Firma == null || istasyon.Firma.PatronId != CurrentUserId))
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

            var oldIstasyonSorumluId = istasyon.IstasyonSorumluId;
            var oldVardiyaSorumluId = istasyon.VardiyaSorumluId;
            var oldMarketSorumluId = istasyon.MarketSorumluId;

            istasyon.Ad = request.Ad;
            istasyon.Adres = request.Adres;
            istasyon.Aktif = request.Aktif;
            istasyon.IstasyonSorumluId = request.IstasyonSorumluId;
            istasyon.VardiyaSorumluId = request.VardiyaSorumluId;
            istasyon.MarketSorumluId = request.MarketSorumluId;
            if (IsAdmin)
            {
            istasyon.ApiKey = request.ApiKey;
            }

            // Handle User Updates - Sync IstasyonId
            // 1. Istasyon Sorumlusu
            if (oldIstasyonSorumluId.HasValue && oldIstasyonSorumluId != request.IstasyonSorumluId)
            {
                var oldUser = await _context.Users.FindAsync(oldIstasyonSorumluId.Value);
                if (oldUser != null && oldUser.IstasyonId == istasyon.Id)
                {
                    oldUser.IstasyonId = null;
                }
            }
            if (request.IstasyonSorumluId.HasValue && request.IstasyonSorumluId != oldIstasyonSorumluId)
            {
                var newUser = await _context.Users.FindAsync(request.IstasyonSorumluId.Value);
                if (newUser != null)
                {
                    newUser.IstasyonId = istasyon.Id;
                }
            }

            // 2. Vardiya Sorumlusu
            if (oldVardiyaSorumluId.HasValue && oldVardiyaSorumluId != request.VardiyaSorumluId)
            {
                var oldUser = await _context.Users.FindAsync(oldVardiyaSorumluId.Value);
                if (oldUser != null && oldUser.IstasyonId == istasyon.Id)
                {
                    oldUser.IstasyonId = null;
                }
            }
            if (request.VardiyaSorumluId.HasValue && request.VardiyaSorumluId != oldVardiyaSorumluId)
            {
                var newUser = await _context.Users.FindAsync(request.VardiyaSorumluId.Value);
                if (newUser != null)
                {
                    newUser.IstasyonId = istasyon.Id;
                }
            }

            // 3. Market Sorumlusu
            if (oldMarketSorumluId.HasValue && oldMarketSorumluId != request.MarketSorumluId)
            {
                var oldUser = await _context.Users.FindAsync(oldMarketSorumluId.Value);
                if (oldUser != null && oldUser.IstasyonId == istasyon.Id)
                {
                    oldUser.IstasyonId = null;
                }
            }
            if (request.MarketSorumluId.HasValue && request.MarketSorumluId != oldMarketSorumluId)
            {
                var newUser = await _context.Users.FindAsync(request.MarketSorumluId.Value);
                if (newUser != null)
                {
                    newUser.IstasyonId = istasyon.Id;
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> DeleteIstasyon(int id)
        {
            var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == id);

            if (istasyon == null)
            {
                return NotFound();
            }

            if (IsPatron && (istasyon.Firma == null || istasyon.Firma.PatronId != CurrentUserId))
            {
                return Forbid();
            }

            _context.Istasyonlar.Remove(istasyon);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost("{id}/unlock")]
        [Authorize(Roles = "admin,patron")]
        public async Task<IActionResult> UnlockStation(int id)
        {
            var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == id);
            
            if (istasyon == null) return NotFound();
            
            if (IsPatron && (istasyon.Firma == null || istasyon.Firma.PatronId != CurrentUserId)) return Forbid();

            // Clear the lock
            istasyon.RegisteredDeviceId = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "İstasyon kilidi kaldırıldı.", id = istasyon.Id });
        }
    }
}
