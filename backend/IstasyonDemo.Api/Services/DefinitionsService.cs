using IstasyonDemo.Api.Models;
using IstasyonDemo.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;

namespace IstasyonDemo.Api.Services
{
    public class DefinitionsService : IDefinitionsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DefinitionsService> _logger;

        public DefinitionsService(AppDbContext context, ILogger<DefinitionsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<SystemDefinition>> GetDefinitionsByTypeAsync(DefinitionType type)
        {
            return await _context.SystemDefinitions
                .Where(d => d.Type == type && d.IsActive)
                .OrderBy(d => d.SortOrder)
                .ToListAsync();
        }

        public async Task<SystemDefinition> AddDefinitionAsync(SystemDefinition definition)
        {
            _context.SystemDefinitions.Add(definition);
            await _context.SaveChangesAsync();
            return definition;
        }

        public async Task<SystemDefinition> UpdateDefinitionAsync(int id, SystemDefinition definition)
        {
            var existing = await _context.SystemDefinitions.FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Tanım bulunamadı.");

            existing.Name = definition.Name;
            existing.Description = definition.Description;
            existing.IsActive = definition.IsActive;
            existing.SortOrder = definition.SortOrder;
            existing.Code = definition.Code;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteDefinitionAsync(int id)
        {
            var existing = await _context.SystemDefinitions.FindAsync(id);
            if (existing != null)
            {
                _context.SystemDefinitions.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<SystemDefinition>> GetAllDefinitionsAsync()
        {
            return await _context.SystemDefinitions
                .OrderBy(d => d.Type)
                .ThenBy(d => d.SortOrder)
                .ToListAsync();
        }

        public async Task SeedInitialDataAsync()
        {
            await SeedYakitlarAsync();
            await SeedSystemDefinitionsAsync();
        }

        private async Task SeedYakitlarAsync()
        {
            if (!await _context.Yakitlar.AnyAsync())
            {
                _logger.LogInformation("Yakıt tanımları oluşturuluyor...");
                var yakitlar = new List<Yakit>
                {
                    new Yakit { Ad = "Benzin", OtomasyonUrunAdi = "BENZIN,KURSUNSUZ", TurpakUrunKodu = "4", Renk = "#22c55e", Sira = 1 },
                    new Yakit { Ad = "Motorin", OtomasyonUrunAdi = "MOTORIN,DIZEL", TurpakUrunKodu = "6", Renk = "#eab308", Sira = 2 },
                    new Yakit { Ad = "LPG", OtomasyonUrunAdi = "LPG,OTOGAZ", Renk = "#3b82f6", TurpakUrunKodu = "5", Sira = 3 },
                    new Yakit { Ad = "Euro Diesel", OtomasyonUrunAdi = "EURO DIESEL", TurpakUrunKodu = "", Renk = "#f97316", Sira = 4 }
                };

                _context.Yakitlar.AddRange(yakitlar);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update existing records with default Turpak codes if they are empty
                var existingYakitlar = await _context.Yakitlar.ToListAsync();
                bool changed = false;

                foreach (var yakit in existingYakitlar)
                {
                    if (string.IsNullOrEmpty(yakit.TurpakUrunKodu))
                    {
                        if (yakit.OtomasyonUrunAdi.Contains("BENZIN")) { yakit.TurpakUrunKodu = "5,2"; changed = true; }
                        else if (yakit.OtomasyonUrunAdi.Contains("MOTORIN")) { yakit.TurpakUrunKodu = "4,6,7,8,1"; changed = true; }
                        else if (yakit.OtomasyonUrunAdi.Contains("LPG")) { yakit.TurpakUrunKodu = "9"; changed = true; }
                    }
                }

                if (changed)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Mevcut yakıt tanımları Turpak kodları ile güncellendi.");
                }
            }
        }

        private async Task SeedSystemDefinitionsAsync()
        {
            if (!await _context.SystemDefinitions.AnyAsync())
            {
                _logger.LogInformation("Sistem tanımları oluşturuluyor...");
                var definitions = new List<SystemDefinition>();

                // Gider Türleri
                definitions.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Personel", Code = "PERSONEL", SortOrder = 1 });
                definitions.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Yemek", Code = "YEMEK", SortOrder = 2 });
                definitions.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Temizlik", Code = "TEMIZLIK", SortOrder = 3 });
                definitions.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Kırtasiye", Code = "KIRTASIYE", SortOrder = 4 });
                definitions.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Diğer", Code = "DIGER", SortOrder = 99 });

                // Gelir Türleri
                definitions.Add(new SystemDefinition { Type = DefinitionType.GELIR, Name = "Prim", Code = "PRIM", SortOrder = 1 });
                definitions.Add(new SystemDefinition { Type = DefinitionType.GELIR, Name = "Komisyon", Code = "KOMISYON", SortOrder = 2 });
                definitions.Add(new SystemDefinition { Type = DefinitionType.GELIR, Name = "Diğer", Code = "DIGER", SortOrder = 99 });

                _context.SystemDefinitions.AddRange(definitions);
                await _context.SaveChangesAsync();
            }
        }
    }
}
