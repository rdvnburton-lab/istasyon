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
    public class UserController : BaseController
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            IQueryable<User> query = _context.Users
                .Include(u => u.Istasyon)
                .Include(u => u.Role);

            if (IsPatron)
            {
                // Patron can only see users in their stations
                // Find stations owned by this patron
                var patronStationIds = await _context.Istasyonlar
                    .Include(i => i.Firma)
                    .Where(i => i.Firma != null && i.Firma.PatronId == CurrentUserId)
                    .Select(i => i.Id)
                    .ToListAsync();

                query = query.Where(u => u.IstasyonId.HasValue && patronStationIds.Contains(u.IstasyonId.Value));
            }
            else if (!IsAdmin)
            {
                // Regular users can't see list of users
                return Forbid();
            }

            var users = await query.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role != null ? u.Role.Ad : "",
                RoleId = u.RoleId,
                IstasyonId = u.IstasyonId,
                IstasyonAdi = u.Istasyon != null ? u.Istasyon.Ad : null,
                AdSoyad = u.AdSoyad,
                Telefon = u.Telefon,
                FotografData = u.FotografData
            }).ToListAsync();

            return Ok(users);
        }

        [HttpGet("by-role/{role}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByRole(string role)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Istasyon)
                .Where(u => u.Role != null && u.Role.Ad == role);

            if (IsPatron)
            {
                // Patron için ek filtreleme gerekirse buraya eklenebilir
                // Örneğin sadece kendi istasyonlarındaki o role sahip kullanıcıları görsün
                 var patronStationIds = await _context.Istasyonlar
                    .Include(i => i.Firma)
                    .Where(i => i.Firma != null && i.Firma.PatronId == CurrentUserId)
                    .Select(i => i.Id)
                    .ToListAsync();

                query = query.Where(u => u.IstasyonId.HasValue && patronStationIds.Contains(u.IstasyonId.Value));
            }

            var users = await query
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role != null ? u.Role.Ad : "",
                    RoleId = u.RoleId,
                    IstasyonId = u.IstasyonId,
                    IstasyonAdi = u.Istasyon != null ? u.Istasyon.Ad : null,
                    AdSoyad = u.AdSoyad,
                    Telefon = u.Telefon,
                    FotografData = u.FotografData
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (IsPatron)
            {
                // Check if the user belongs to one of the patron's stations
                if (!user.IstasyonId.HasValue)
                {
                    return Forbid("Bu kullanıcıyı düzenleme yetkiniz yok.");
                }

                var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == user.IstasyonId.Value);
                if (istasyon == null || istasyon.Firma == null || istasyon.Firma.PatronId != CurrentUserId)
                {
                    return Forbid("Bu kullanıcıyı düzenleme yetkiniz yok.");
                }
            }
            else if (!IsAdmin)
            {
                return Forbid();
            }

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return BadRequest("Geçersiz rol.");
            }

            if (IsPatron)
            {
                // Patron cannot change role to admin or patron
                if (role.Ad == "admin" || role.Ad == "patron")
                {
                    return BadRequest("Bu rolü atayamazsınız.");
                }
            }

            // Capture old role name for later check
            var oldRoleName = user.Role?.Ad;
            if (oldRoleName == null)
            {
                var oldRole = await _context.Roles.FindAsync(user.RoleId);
                oldRoleName = oldRole?.Ad;
            }

            user.Username = request.Username;
            user.RoleId = role.Id;
            user.AdSoyad = request.AdSoyad;
            user.Telefon = request.Telefon;
            user.FotografData = request.FotografData;
            
            if (request.IstasyonId.HasValue)
            {
                 if (IsPatron)
                 {
                      var targetIstasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == request.IstasyonId.Value);
                      if (targetIstasyon == null || targetIstasyon.Firma == null || targetIstasyon.Firma.PatronId != CurrentUserId)
                      {
                           return BadRequest("Kullanıcıyı sahip olmadığınız bir istasyona atayamazsınız.");
                      }
                 }
                 user.IstasyonId = request.IstasyonId;
            }

            if (request.FirmaId.HasValue && IsAdmin)
            {
                // Clear previous firma ownership if any
                var previousFirmas = await _context.Firmalar.Where(f => f.PatronId == user.Id).ToListAsync();
                foreach (var f in previousFirmas)
                {
                    f.PatronId = null;
                }

                // Assign new firma
                var firma = await _context.Firmalar.FindAsync(request.FirmaId.Value);
                if (firma != null)
                {
                    firma.PatronId = user.Id;
                }
            }

            // Handle Station Responsibility Assignment on Update
            if (user.IstasyonId.HasValue)
            {
                var istasyon = await _context.Istasyonlar.FindAsync(user.IstasyonId.Value);
                if (istasyon != null)
                {
                    // If user is assigned to this station, update responsibility based on role
                    if (role.Ad == "istasyon sorumlusu") istasyon.IstasyonSorumluId = user.Id;
                    else if (role.Ad == "vardiya sorumlusu") istasyon.VardiyaSorumluId = user.Id;
                    else if (role.Ad == "market sorumlusu") istasyon.MarketSorumluId = user.Id;
                    
                    // If role changed AWAY from a responsible role (e.g. to Pasif), clear the responsibility
                    if (oldRoleName == "istasyon sorumlusu" && role.Ad != "istasyon sorumlusu" && istasyon.IstasyonSorumluId == user.Id)
                    {
                        istasyon.IstasyonSorumluId = null;
                    }
                    else if (oldRoleName == "vardiya sorumlusu" && role.Ad != "vardiya sorumlusu" && istasyon.VardiyaSorumluId == user.Id)
                    {
                        istasyon.VardiyaSorumluId = null;
                    }
                    else if (oldRoleName == "market sorumlusu" && role.Ad != "market sorumlusu" && istasyon.MarketSorumluId == user.Id)
                    {
                        istasyon.MarketSorumluId = null;
                    }
                }
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (IsPatron)
            {
                if (!user.IstasyonId.HasValue)
                {
                    return Forbid("Bu kullanıcıyı silme yetkiniz yok.");
                }

                var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == user.IstasyonId.Value);
                if (istasyon == null || istasyon.Firma == null || istasyon.Firma.PatronId != CurrentUserId)
                {
                    return Forbid("Bu kullanıcıyı silme yetkiniz yok.");
                }
            }
            else if (!IsAdmin)
            {
                return Forbid();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
