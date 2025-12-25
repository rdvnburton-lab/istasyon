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
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<User> query = _context.Users
                .Include(u => u.Istasyon)
                .Include(u => u.Role);

            if (currentUserRole == "patron")
            {
                // Patron can only see users in their stations
                // Find stations owned by this patron
                var patronStationIds = await _context.Istasyonlar
                    .Where(i => i.PatronId == currentUserId)
                    .Select(i => i.Id)
                    .ToListAsync();

                query = query.Where(u => u.IstasyonId.HasValue && patronStationIds.Contains(u.IstasyonId.Value));
            }
            else if (currentUserRole != "admin")
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
                Telefon = u.Telefon
            }).ToListAsync();

            return Ok(users);
        }

        [HttpGet("by-role/{role}")]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByRole(string role)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Istasyon)
                .Where(u => u.Role != null && u.Role.Ad == role);

            if (userRole == "patron")
            {
                // Patron için ek filtreleme gerekirse buraya eklenebilir
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
                    Telefon = u.Telefon
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto request)
        {
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (currentUserRole == "patron")
            {
                // Check if the user belongs to one of the patron's stations
                if (!user.IstasyonId.HasValue)
                {
                    return Forbid("Bu kullanıcıyı düzenleme yetkiniz yok.");
                }

                var istasyon = await _context.Istasyonlar.FindAsync(user.IstasyonId.Value);
                if (istasyon == null || istasyon.PatronId != currentUserId)
                {
                    return Forbid("Bu kullanıcıyı düzenleme yetkiniz yok.");
                }
            }
            else if (currentUserRole != "admin")
            {
                return Forbid();
            }

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return BadRequest("Geçersiz rol.");
            }

            if (currentUserRole == "patron")
            {
                // Patron cannot change role to admin or patron
                if (role.Ad == "admin" || role.Ad == "patron")
                {
                    return BadRequest("Bu rolü atayamazsınız.");
                }
            }

            user.Username = request.Username;
            user.RoleId = role.Id;
            user.AdSoyad = request.AdSoyad;
            user.Telefon = request.Telefon;
            
            if (request.IstasyonId.HasValue)
            {
                 if (currentUserRole == "patron")
                 {
                      var targetIstasyon = await _context.Istasyonlar.FindAsync(request.IstasyonId.Value);
                      if (targetIstasyon == null || targetIstasyon.PatronId != currentUserId)
                      {
                           return BadRequest("Kullanıcıyı sahip olmadığınız bir istasyona atayamazsınız.");
                      }
                 }
                 user.IstasyonId = request.IstasyonId;
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
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (currentUserRole == "patron")
            {
                if (!user.IstasyonId.HasValue)
                {
                    return Forbid("Bu kullanıcıyı silme yetkiniz yok.");
                }

                var istasyon = await _context.Istasyonlar.FindAsync(user.IstasyonId.Value);
                if (istasyon == null || istasyon.PatronId != currentUserId)
                {
                    return Forbid("Bu kullanıcıyı silme yetkiniz yok.");
                }
            }
            else if (currentUserRole != "admin")
            {
                return Forbid();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
