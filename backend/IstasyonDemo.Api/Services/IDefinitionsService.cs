using IstasyonDemo.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IstasyonDemo.Api.Services
{
    public interface IDefinitionsService
    {
        Task<IEnumerable<SystemDefinition>> GetDefinitionsByTypeAsync(DefinitionType type);
        Task<SystemDefinition> AddDefinitionAsync(SystemDefinition definition);
        Task<SystemDefinition> UpdateDefinitionAsync(int id, SystemDefinition definition);
        Task DeleteDefinitionAsync(int id);
        Task<IEnumerable<SystemDefinition>> GetAllDefinitionsAsync();
        
        // Helper to seed initial data
        Task SeedInitialDataAsync();
    }
}
