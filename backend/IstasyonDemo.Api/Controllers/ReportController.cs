using Microsoft.AspNetCore.Mvc;
using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace IstasyonDemo.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("vardiya/{id}/excel")]
        [Authorize]
        public async Task<IActionResult> ExportMutabakatExcel(int id)
        {
            try 
            {
                var fileContent = await _reportService.GenerateMutabakatExcel(id);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Vardiya_{id}_Mutabakat.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest($"Excel raporu oluşturulurken hata: {ex.Message}");
            }
        }

        [HttpGet("vardiya/{id}/pdf")]
        [Authorize]
        public async Task<IActionResult> ExportMutabakatPdf(int id)
        {
            try 
            {
                var fileContent = await _reportService.GenerateMutabakatPdf(id);
                return File(fileContent, "application/pdf", $"Vardiya_{id}_Mutabakat.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"PDF raporu oluşturulurken hata: {ex.Message}");
            }
        }
    }
}
