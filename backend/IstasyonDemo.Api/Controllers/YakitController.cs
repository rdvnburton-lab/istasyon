using IstasyonDemo.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IstasyonDemo.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class YakitController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public YakitController(AppDbContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Yakitlar.OrderBy(y => y.Sira).ToListAsync();
            var dtos = _mapper.Map<List<IstasyonDemo.Api.Dtos.YakitDto>>(list);
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var yakit = await _context.Yakitlar.FindAsync(id);
            if (yakit == null) return NotFound();
            return Ok(_mapper.Map<IstasyonDemo.Api.Dtos.YakitDto>(yakit));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IstasyonDemo.Api.Dtos.CreateYakitDto dto)
        {
            var entity = _mapper.Map<IstasyonDemo.Api.Models.Yakit>(dto);
            _context.Yakitlar.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(_mapper.Map<IstasyonDemo.Api.Dtos.YakitDto>(entity));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] IstasyonDemo.Api.Dtos.UpdateYakitDto dto)
        {
            var existing = await _context.Yakitlar.FindAsync(id);
            if (existing == null) return NotFound();

            _mapper.Map(dto, existing);
            await _context.SaveChangesAsync();
            return Ok(_mapper.Map<IstasyonDemo.Api.Dtos.YakitDto>(existing));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Check usage
            bool inUse = await _context.TankGirisler.AnyAsync(t => t.YakitId == id);
            if (inUse)
            {
                return BadRequest("Bu yakıt türüne ait stok girişleri olduğu için silinemez.");
            }

            var yakit = await _context.Yakitlar.FindAsync(id);
            if (yakit == null) return NotFound();

            _context.Yakitlar.Remove(yakit);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
