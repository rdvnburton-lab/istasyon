using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IstasyonDemo.Api.Services
{
    public class YakitService : IYakitService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<YakitService> _logger;
        private List<Yakit>? _cache;

        public YakitService(AppDbContext context, ILogger<YakitService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Yakit>> GetAllYakitlarAsync()
        {
            if (_cache == null)
            {
                await RefreshCacheAsync();
            }
            return _cache ?? new List<Yakit>();
        }

        public async Task RefreshCacheAsync()
        {
            _logger.LogInformation("Yakıt tanımları önbelleği yenileniyor...");
            _cache = await _context.Yakitlar.OrderBy(y => y.Sira).ToListAsync();
        }

        public async Task<Yakit?> IdentifyYakitAsync(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return null;

            var yakitlar = await GetAllYakitlarAsync();
            var normalized = rawValue.Trim().ToUpperInvariant();

            // 1. Sayısal Kod Kontrolü (TurpakUrunKodu)
            // Eğer rawValue bir sayıysa, TurpakUrunKodu alanında bu sayıyı ara
            var matchByCode = yakitlar.FirstOrDefault(y => 
                !string.IsNullOrEmpty(y.TurpakUrunKodu) && 
                y.TurpakUrunKodu.Split(',').Select(k => k.Trim()).Contains(normalized));

            if (matchByCode != null) return matchByCode;

            // 2. Metin Bazlı Kontrol (OtomasyonUrunAdi ve Ad)
            var matchByName = yakitlar.FirstOrDefault(y => 
                (y.OtomasyonUrunAdi != null && y.OtomasyonUrunAdi.ToUpperInvariant().Split(',').Any(k => normalized.Contains(k.Trim()))) ||
                normalized.Contains(y.Ad.ToUpperInvariant()));

            return matchByName;
        }
    }
}
