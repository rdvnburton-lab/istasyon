using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/vardiya/{vardiyaId}/gider")]
    [Authorize]
    public class PompaGiderController : BaseController
    {
        private readonly AppDbContext _context;

        public PompaGiderController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<bool> CheckVardiyaAccess(int vardiyaId)
        {
            if (IsAdmin) return true;

            var vardiya = await _context.Vardiyalar
                .Include(v => v.Istasyon).ThenInclude(i => i!.Firma)
                .FirstOrDefaultAsync(v => v.Id == vardiyaId);

            if (vardiya == null) return false;

            if (IsPatron)
            {
                return vardiya.Istasyon?.Firma?.PatronId == CurrentUserId;
            }

            return vardiya.IstasyonId == CurrentIstasyonId;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int vardiyaId)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var giderler = await _context.PompaGiderler
                .Where(g => g.VardiyaId == vardiyaId)
                .OrderByDescending(g => g.OlusturmaTarihi)
                .ToListAsync();

            return Ok(giderler);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int vardiyaId, [FromBody] PompaGider gider)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var vardiya = await _context.Vardiyalar.FindAsync(vardiyaId);
            if (vardiya == null) return NotFound(new { message = "Vardiya bulunamadı" });

            gider.VardiyaId = vardiyaId;
            gider.OlusturmaTarihi = DateTime.UtcNow;

            _context.PompaGiderler.Add(gider);
            await _context.SaveChangesAsync();

            return Ok(gider);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int vardiyaId, int id)
        {
            if (!await CheckVardiyaAccess(vardiyaId)) return Forbid();

            var gider = await _context.PompaGiderler
                .FirstOrDefaultAsync(g => g.Id == id && g.VardiyaId == vardiyaId);

            if (gider == null) return NotFound();

            _context.PompaGiderler.Remove(gider);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
