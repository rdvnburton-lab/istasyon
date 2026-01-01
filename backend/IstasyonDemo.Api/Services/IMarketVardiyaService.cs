using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;

namespace IstasyonDemo.Api.Services
{
    public interface IMarketVardiyaService
    {
        Task<IEnumerable<MarketVardiyaDto>> GetMarketVardiyalarAsync(int userId, string userRole, int? istasyonId);
        Task<MarketVardiya?> GetMarketVardiyaByIdAsync(int id, int userId, string userRole, int? istasyonId);
        Task<MarketVardiya> CreateMarketVardiyaAsync(CreateMarketVardiyaDto dto, int userId, int? istasyonId);
        
        Task<MarketZRaporu> AddZRaporuAsync(int vardiyaId, MarketZRaporuDto dto, int userId, string userRole, int? istasyonId);
        Task AddTahsilatAsync(int vardiyaId, MarketTahsilatDto dto, int userId, string userRole, int? istasyonId);
        Task<MarketGider> AddGiderAsync(int vardiyaId, MarketGiderDto dto, int userId, string userRole, int? istasyonId);
        Task DeleteGiderAsync(int giderId, int userId, string userRole, int? istasyonId);
        Task<MarketGelir> AddGelirAsync(int vardiyaId, MarketGelirDto dto, int userId, string userRole, int? istasyonId);
        Task DeleteGelirAsync(int gelirId, int userId, string userRole, int? istasyonId);
        
        Task OnayaGonderAsync(int id, int userId, string userRole, int? istasyonId);
        Task OnaylaAsync(int id, int userId, string userRole);
        Task ReddetAsync(int id, string neden, int userId, string userRole);

        Task<object> GetMarketRaporuAsync(DateTimeOffset baslangic, DateTimeOffset bitis, int userId, string userRole, int? istasyonId);
    }
}
