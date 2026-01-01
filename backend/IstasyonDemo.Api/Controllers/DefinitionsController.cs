using IstasyonDemo.Api.Models;
using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DefinitionsController : ControllerBase
    {
        private readonly IDefinitionsService _service;

        public DefinitionsController(IDefinitionsService service)
        {
            _service = service;
        }

        [HttpGet("{type}")]
        public async Task<ActionResult<IEnumerable<SystemDefinition>>> GetByType(DefinitionType type)
        {
            return Ok(await _service.GetDefinitionsByTypeAsync(type));
        }

        [HttpPost]
        public async Task<ActionResult<SystemDefinition>> Create(SystemDefinition definition)
        {
            var result = await _service.AddDefinitionAsync(definition);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SystemDefinition>> Update(int id, SystemDefinition definition)
        {
            try
            {
                var result = await _service.UpdateDefinitionAsync(id, definition);
                return Ok(result);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteDefinitionAsync(id);
            return NoContent();
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            await _service.SeedInitialDataAsync();
            return Ok("Seed completed");
        }
    }
}
