using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/pusula-ocr")]
    [ApiController]
    public class PusulaOcrController : ControllerBase
    {
        private readonly GeminiService _geminiService;

        public PusulaOcrController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
        {
            if (string.IsNullOrEmpty(request.ImageBase64))
            {
                return BadRequest("Image data is required.");
            }

            try
            {
                var result = await _geminiService.AnalyzeReceiptAsync(request.ImageBase64);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class AnalyzeRequest
    {
        public string ImageBase64 { get; set; } = string.Empty;
    }
}
