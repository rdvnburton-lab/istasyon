using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/gemini")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class GeminiController : ControllerBase
    {
        private readonly GeminiService _geminiService;

        public GeminiController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost("analyze-dashboard")]
        public async Task<IActionResult> AnalyzeDashboard([FromBody] object dashboardData)
        {
            try
            {
                var result = await _geminiService.AnalyzeDashboardAsync(dashboardData);
                if (string.IsNullOrEmpty(result)) return NotFound("Analiz oluşturulamadı.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
