using IstasyonDemo.Api.Attributes;
using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermissionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{roleName}")]
        public async Task<IActionResult> GetPermissions(string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Ad == roleName);
            if (role == null)
            {
                return NotFound("Rol bulunamadı.");
            }

            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.ResourceKey)
                .ToListAsync();

            return Ok(permissions);
        }

        [HttpPost("{roleName}")]
        [HasPermission("YONETIM_YETKI")] // Sadece yetki yönetimi yetkisi olanlar değiştirebilir
        public async Task<IActionResult> UpdatePermissions(string roleName, [FromBody] List<string> resourceKeys)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Ad == roleName);
            if (role == null)
            {
                return NotFound("Rol bulunamadı.");
            }

            if (role.Ad.ToLower() == "admin")
            {
                return BadRequest("Admin yetkileri değiştirilemez.");
            }

            // Mevcut yetkileri sil
            var existingPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(existingPermissions);

            // Yeni yetkileri ekle
            var newPermissions = resourceKeys.Select(key => new RolePermission
            {
                RoleId = role.Id,
                ResourceKey = key
            });

            await _context.RolePermissions.AddRangeAsync(newPermissions);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Yetkiler güncellendi." });
        }
        
        // Tüm rollerin yetkilerini toplu getirmek için (Frontend matrisi için)
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var allPermissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .ToListAsync();

            var grouped = allPermissions
                .GroupBy(rp => rp.Role?.Ad.ToLower() ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Select(rp => rp.ResourceKey).ToList());

            return Ok(grouped);
        }
    }
}
