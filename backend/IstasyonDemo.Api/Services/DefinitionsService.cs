using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Services
{
    public class DefinitionsService : IDefinitionsService
    {
        private readonly AppDbContext _context;

        public DefinitionsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SystemDefinition>> GetDefinitionsByTypeAsync(DefinitionType type)
        {
            return await _context.SystemDefinitions
                .Where(x => x.Type == type && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
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
            if (existing == null) throw new Exception("Definition not found");

            existing.Name = definition.Name;
            existing.Description = definition.Description;
            existing.IsActive = definition.IsActive;
            existing.SortOrder = definition.SortOrder;
            
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteDefinitionAsync(int id)
        {
            var definition = await _context.SystemDefinitions.FindAsync(id);
            if (definition == null) return;

            // Soft delete
            definition.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SystemDefinition>> GetAllDefinitionsAsync()
        {
            return await _context.SystemDefinitions.ToListAsync();
        }

        public async Task SeedInitialDataAsync()
        {
            var existingTypes = await _context.SystemDefinitions.Select(x => x.Type).Distinct().ToListAsync();
            var definitionsToSeed = new List<SystemDefinition>();

            // Bankalar
            if (!existingTypes.Contains(DefinitionType.BANKA))
            {
                var bankalar = new[] { "Ziraat Bankası", "Garanti BBVA", "İş Bankası", "Yapı Kredi", "Akbank", "Halkbank", "Vakıfbank", "QNB Finansbank", "Denizbank" };
                int order = 1;
                foreach (var b in bankalar)
                {
                    definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.BANKA, Name = b, SortOrder = order++ });
                }
            }

            // Yakıtlar
            if (!existingTypes.Contains(DefinitionType.YAKIT))
            {
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.YAKIT, Name = "Benzin", Code = "BENZIN", SortOrder = 1 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.YAKIT, Name = "Motorin", Code = "MOTORIN", SortOrder = 2 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.YAKIT, Name = "LPG", Code = "LPG", SortOrder = 3 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.YAKIT, Name = "Euro Diesel", Code = "EURO_DIESEL", SortOrder = 4 });
            }

            // Giderler
            if (!existingTypes.Contains(DefinitionType.GIDER))
            {
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Ekmek", Code = "EKMEK", SortOrder = 1 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Temizlik", Code = "TEMIZLIK", SortOrder = 2 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Personel", Code = "PERSONEL", SortOrder = 3 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Kırtasiye", Code = "KIRTASIYE", SortOrder = 4 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GIDER, Name = "Diğer", Code = "DIGER", SortOrder = 99 });
            }

            // Gelirler
            if (!existingTypes.Contains(DefinitionType.GELIR))
            {
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GELIR, Name = "Komisyon", Code = "KOMISYON", SortOrder = 1 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GELIR, Name = "Prim", Code = "PRIM", SortOrder = 2 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GELIR, Name = "Diğer", Code = "DIGER", SortOrder = 99 });
            }

            // Ödeme Yöntemleri
            if (!existingTypes.Contains(DefinitionType.ODEME))
            {
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.ODEME, Name = "Nakit", Code = "NAKIT", SortOrder = 1 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.ODEME, Name = "Kredi Kartı", Code = "KREDI_KARTI", SortOrder = 2 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.ODEME, Name = "Paro Puan", Code = "PARO_PUAN", SortOrder = 3 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.ODEME, Name = "Mobil Ödeme", Code = "MOBIL_ODEME", SortOrder = 4 });
            }

            // Geliş Yöntemleri
            if (!existingTypes.Contains(DefinitionType.GELIS_YONTEMI))
            {
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GELIS_YONTEMI, Name = "Tanker", Code = "TANKER", SortOrder = 1 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GELIS_YONTEMI, Name = "Boru Hattı", Code = "BORU_HATTI", SortOrder = 2 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.GELIS_YONTEMI, Name = "Varil", Code = "VARIL", SortOrder = 3 });
            }

            // Pompa Giderleri
            if (!existingTypes.Contains(DefinitionType.POMPA_GIDER))
            {
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.POMPA_GIDER, Name = "Yıkama", Code = "YIKAMA", SortOrder = 1 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.POMPA_GIDER, Name = "Bahşiş", Code = "BAHSIS", SortOrder = 2 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.POMPA_GIDER, Name = "Temizlik", Code = "TEMIZLIK", SortOrder = 3 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.POMPA_GIDER, Name = "Tamir", Code = "TAMIR", SortOrder = 4 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.POMPA_GIDER, Name = "Diğer", Code = "DIGER", SortOrder = 99 });
            }

            // Pusula Türleri
            if (!existingTypes.Contains(DefinitionType.PUSULA_TURU))
            {
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.PUSULA_TURU, Name = "Kasa Açılış", Code = "KASA_ACILIS", SortOrder = 1 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.PUSULA_TURU, Name = "Kasa Devir", Code = "KASA_DEVIR", SortOrder = 2 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.PUSULA_TURU, Name = "Nakit Tahsilat", Code = "NAKIT_TAHSILAT", SortOrder = 3 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.PUSULA_TURU, Name = "Kredi Kartı", Code = "KREDI_KARTI", SortOrder = 4 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.PUSULA_TURU, Name = "Havale/EFT", Code = "HAVALE_EFT", SortOrder = 5 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.PUSULA_TURU, Name = "Masraf", Code = "MASRAF", SortOrder = 6 });
                definitionsToSeed.Add(new SystemDefinition { Type = DefinitionType.PUSULA_TURU, Name = "Diğer", Code = "DIGER", SortOrder = 99 });
            }

            if (definitionsToSeed.Any())
            {
                await _context.SystemDefinitions.AddRangeAsync(definitionsToSeed);
                await _context.SaveChangesAsync();
            }
        }
    }
}
