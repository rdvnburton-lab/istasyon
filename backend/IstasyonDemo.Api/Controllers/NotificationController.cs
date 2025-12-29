using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using IstasyonDemo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;

namespace IstasyonDemo.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public NotificationController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public class TestNotificationDto
        {
            public int? UserId { get; set; } // Tekil gönderim için (Geriye uyumluluk)
            public List<int>? UserIds { get; set; } // Toplu gönderim için
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        [HttpPost("send-test")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationDto dto)
        {
            int successCount = 0;

            // 1. Toplu Gönderim (UserIds varsa)
            if (dto.UserIds != null && dto.UserIds.Any())
            {
                foreach (var uid in dto.UserIds)
                {
                    try
                    {
                        await _notificationService.NotifyUserAsync(
                            uid,
                            dto.Title,
                            dto.Message,
                            "TEST_NOTIFICATION",
                            "info"
                        );
                        successCount++;
                    }
                    catch (Exception)
                    {
                        // Log error but continue
                    }
                }
                return Ok(new { message = $"{successCount} kullanıcıya bildirim gönderildi." });
            }
            // 2. Tekil Gönderim (UserId varsa)
            else if (dto.UserId.HasValue)
            {
                await _notificationService.NotifyUserAsync(
                    dto.UserId.Value,
                    dto.Title,
                    dto.Message,
                    "TEST_NOTIFICATION",
                    "info"
                );
                return Ok(new { message = $"Kullanıcıya ({dto.UserId}) test bildirimi gönderildi." });
            }
            // 3. Kendine Gönderim (Hiçbiri yoksa)
            else
            {
                var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized();
                }

                await _notificationService.NotifyUserAsync(
                    userId,
                    dto.Title,
                    dto.Message,
                    "TEST_NOTIFICATION",
                    "info"
                );
                return Ok(new { message = "Kendinize test bildirimi gönderildi." });
            }
        }

        public class RegisterTokenDto
        {
            public string Token { get; set; } = string.Empty;
        }

        [HttpPost("register-token")]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenDto dto)
        {
            var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.FcmToken = dto.Token;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Token başarıyla kaydedildi." });
        }

        [HttpGet]
        public async Task<ActionResult<NotificationSummaryDto>> GetNotifications()
        {
            var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20) // Son 20 bildirim
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Time = GetRelativeTime(n.CreatedAt),
                    Read = n.IsRead,
                    Icon = GetIconForType(n.Type),
                    Severity = n.Severity,
                    RelatedId = n.RelatedVardiyaId ?? n.RelatedMarketVardiyaId,
                    RelatedType = n.RelatedVardiyaId != null ? "vardiya" : "market"
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            return Ok(new NotificationSummaryDto
            {
                UnreadCount = unreadCount,
                Notifications = notifications
            });
        }

        [HttpPost("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

            return Ok();
        }

        private static string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Az önce";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} dakika önce";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} saat önce";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} gün önce";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} hafta önce";
            
            return dateTime.ToLocalTime().ToString("dd.MM.yyyy");
        }

        [HttpPost("sync-logs")]
        public async Task<IActionResult> SyncLogs()
        {
            // Admin only check (optional, but recommended)
            // var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            // if (userRole != "admin") return Forbid();

            var logs = await _context.VardiyaLoglari
                .OrderByDescending(l => l.IslemTarihi)
                .Take(100) // Son 100 işlem
                .ToListAsync();

            int count = 0;
            foreach (var log in logs)
            {
                if (!log.KullaniciId.HasValue) continue;

                // Check if notification already exists for this log (approximate check by time and user)
                var exists = await _context.Notifications.AnyAsync(n => 
                    n.UserId == log.KullaniciId.Value && 
                    n.CreatedAt == log.IslemTarihi);

                if (!exists)
                {
                    string title = "İşlem Bildirimi";
                    string message = log.Aciklama ?? "İşlem yapıldı.";
                    string type = "INFO";
                    string severity = "info";

                    switch (log.Islem)
                    {
                        case "OLUSTURULDU":
                            title = "Vardiya Oluşturuldu";
                            type = "VARDIYA_OLUSTURULDU";
                            severity = "success";
                            break;
                        case "ONAYA_GONDERILDI":
                            title = "Onaya Gönderildi";
                            type = "VARDIYA_ONAY_BEKLIYOR";
                            severity = "info";
                            break;
                        case "ONAYLANDI":
                            title = "Vardiya Onaylandı";
                            type = "VARDIYA_ONAYLANDI";
                            severity = "success";
                            break;
                        case "REDDEDILDI":
                            title = "Vardiya Reddedildi";
                            type = "VARDIYA_REDDEDILDI";
                            severity = "error";
                            break;
                        case "SILME_TALEP_EDILDI":
                            title = "Silme Talebi";
                            type = "VARDIYA_SILME_ONAYI_BEKLIYOR";
                            severity = "warn";
                            break;
                        case "SILINDI":
                            title = "Vardiya Silindi";
                            type = "VARDIYA_SILINDI";
                            severity = "error";
                            break;
                    }

                    var notification = new Notification
                    {
                        UserId = log.KullaniciId.Value,
                        Title = title,
                        Message = message,
                        Type = type,
                        Severity = severity,
                        CreatedAt = log.IslemTarihi,
                        IsRead = true, // Geçmiş bildirimler okundu olarak gelsin
                        RelatedVardiyaId = log.VardiyaId
                    };

                    _context.Notifications.Add(notification);
                    count++;
                }
            }

            if (count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = $"{count} geçmiş işlem bildirime dönüştürüldü." });
        }

        private static string GetIconForType(string type)
        {
            return type switch
            {
                "VARDIYA_ONAYLANDI" => "pi-check-circle",
                "VARDIYA_REDDEDILDI" => "pi-times-circle",
                "VARDIYA_ONAY_BEKLIYOR" => "pi-clock",
                "VARDIYA_SILME_ONAYLANDI" => "pi-trash",
                "VARDIYA_SILME_REDDEDILDI" => "pi-ban",
                "MARKET_ONAYLANDI" => "pi-check-circle",
                "MARKET_REDDEDILDI" => "pi-times-circle",
                "MARKET_ONAY_BEKLIYOR" => "pi-clock",
                "VARDIYA_OLUSTURULDU" => "pi-plus-circle",
                "VARDIYA_SILINDI" => "pi-trash",
                _ => "pi-info-circle"
            };
        }
    }
}
