using IstasyonDemo.Api.Models;

namespace IstasyonDemo.Api.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string title, string message, string type, string severity, int? relatedVardiyaId = null, int? relatedMarketVardiyaId = null);
        Task NotifyAdminsAsync(string title, string message, string type, string severity, int? relatedVardiyaId = null, int? relatedMarketVardiyaId = null);
        Task NotifyUserAsync(int userId, string title, string message, string type, string severity, int? relatedVardiyaId = null, int? relatedMarketVardiyaId = null);
    }
}
