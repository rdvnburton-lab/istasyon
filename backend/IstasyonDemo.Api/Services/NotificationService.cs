using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(int userId, string title, string message, string type, string severity, int? relatedVardiyaId = null, int? relatedMarketVardiyaId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Severity = severity,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                RelatedVardiyaId = relatedVardiyaId,
                RelatedMarketVardiyaId = relatedMarketVardiyaId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyAdminsAsync(string title, string message, string type, string severity, int? relatedVardiyaId = null, int? relatedMarketVardiyaId = null)
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Ad == "admin");
            var patronRole = await _context.Roles.FirstOrDefaultAsync(r => r.Ad == "patron");

            if (adminRole == null && patronRole == null) return;

            var adminUserIds = await _context.Users
                .Where(u => (adminRole != null && u.RoleId == adminRole.Id) || (patronRole != null && u.RoleId == patronRole.Id))
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in adminUserIds)
            {
                await CreateNotificationAsync(userId, title, message, type, severity, relatedVardiyaId, relatedMarketVardiyaId);
            }
        }

        public async Task NotifyUserAsync(int userId, string title, string message, string type, string severity, int? relatedVardiyaId = null, int? relatedMarketVardiyaId = null)
        {
            await CreateNotificationAsync(userId, title, message, type, severity, relatedVardiyaId, relatedMarketVardiyaId);
        }
    }
}
