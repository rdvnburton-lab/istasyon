using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models; // For Enums if needed
using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MarketVardiyaController : BaseController
    {
        private readonly IMarketVardiyaService _service;

        public MarketVardiyaController(IMarketVardiyaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MarketVardiyaDto>>> GetMarketVardiyalar()
        {
            var result = await _service.GetMarketVardiyalarAsync(CurrentUserId, CurrentUserRole, CurrentIstasyonId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<MarketVardiya>> CreateMarketVardiya(CreateMarketVardiyaDto dto)
        {
            try 
            {
                var vardiya = await _service.CreateMarketVardiyaAsync(dto, CurrentUserId, CurrentIstasyonId);
                return Ok(new { id = vardiya.Id, message = "Market vardiyası başarıyla kaydedildi." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetMarketVardiyaDetay(int id)
        {
            try
            {
                var vardiya = await _service.GetMarketVardiyaByIdAsync(id, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                if (vardiya == null) return NotFound();
                return Ok(vardiya);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("{id}/z-raporu")]
        public async Task<ActionResult> SaveZRaporu(int id, MarketZRaporuDto dto)
        {
            try {
                var result = await _service.AddZRaporuAsync(id, dto, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                return Ok(result);
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id}/tahsilat")]
        public async Task<ActionResult> SaveTahsilat(int id, MarketTahsilatDto dto)
        {
             try {
                await _service.AddTahsilatAsync(id, dto, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                return Ok();
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("tahsilat/{tahsilatId}")]
        public async Task<ActionResult> DeleteTahsilat(int tahsilatId)
        {
             try {
                await _service.DeleteTahsilatAsync(tahsilatId, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                return Ok();
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id}/gider")]
        public async Task<ActionResult> AddGider(int id, MarketGiderDto dto)
        {
             try {
                var result = await _service.AddGiderAsync(id, dto, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                return Ok(result);
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("gider/{giderId}")]
        public async Task<ActionResult> DeleteGider(int giderId)
        {
             try {
                await _service.DeleteGiderAsync(giderId, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                return Ok();
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id}/gelir")]
        public async Task<ActionResult> AddGelir(int id, MarketGelirDto dto)
        {
             try {
                var result = await _service.AddGelirAsync(id, dto, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                return Ok(result);
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("gelir/{gelirId}")]
        public async Task<ActionResult> DeleteGelir(int gelirId)
        {
             try {
                await _service.DeleteGelirAsync(gelirId, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                return Ok();
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("rapor")]
        public async Task<IActionResult> GetMarketRaporu([FromQuery] DateTimeOffset baslangic, [FromQuery] DateTimeOffset bitis)
        {
            var result = await _service.GetMarketRaporuAsync(baslangic, bitis, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
            return Ok(result);
        }

        [HttpPost("{id}/onaya-gonder")]
        public async Task<ActionResult> OnayaGonder(int id)
        {
             try {
                await _service.OnayaGonderAsync(id, CurrentUserId, CurrentUserRole, CurrentIstasyonId);
                return Ok(new { message = "Market mutabakatı onaya gönderildi." });
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id}/onayla")]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult> Onayla(int id)
        {
             try {
                await _service.OnaylaAsync(id, CurrentUserId, CurrentUserRole);
                return Ok(new { message = "Vardiya onaylandı." });
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id}/reddet")]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult> Reddet(int id, [FromBody] string neden)
        {
             try {
                await _service.ReddetAsync(id, neden, CurrentUserId, CurrentUserRole);
                return Ok(new { message = "Vardiya reddedildi." });
            } catch (KeyNotFoundException) { return NotFound(); }
              catch (UnauthorizedAccessException) { return Forbid(); }
        }
    }
}

