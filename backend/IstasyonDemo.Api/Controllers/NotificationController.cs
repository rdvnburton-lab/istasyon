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
    public class NotificationController : BaseController
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
                await _notificationService.NotifyUserAsync(
                    CurrentUserId,
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
            var user = await _context.Users.FindAsync(CurrentUserId);
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
            var notifications = await _context.Notifications
                .Where(n => n.UserId == CurrentUserId)
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
                .Where(n => n.UserId == CurrentUserId && !n.IsRead)
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
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);

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
            await _context.Notifications
                .Where(n => n.UserId == CurrentUserId && !n.IsRead)
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
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SyncLogs()
        {
            var logs = await _context.VardiyaLoglari
                .OrderByDescending(l => l.IslemTarihi)
                .Take(100) // Son 100 işlem
                .ToListAsync();

            if (!logs.Any()) return Ok(new { message = "İşlenecek log bulunamadı." });

            var logDates = logs.Select(l => l.IslemTarihi).ToList();
            var userIds = logs.Where(l => l.KullaniciId.HasValue).Select(l => l.KullaniciId.Value).Distinct().ToList();

            // Batch check for existing notifications
            // Note: This is an approximation. Ideally, we should have a LogId in Notification table to be precise.
            // But checking UserId + CreatedAt matches the logic used before.
            var existingNotifications = await _context.Notifications
                .Where(n => userIds.Contains(n.UserId) && logDates.Contains(n.CreatedAt))
                .Select(n => new { n.UserId, n.CreatedAt })
                .ToListAsync();

            var existingSet = new HashSet<(int, DateTime)>(existingNotifications.Select(n => (n.UserId, n.CreatedAt)));

            int count = 0;
            var newNotifications = new List<Notification>();

            foreach (var log in logs)
            {
                if (!log.KullaniciId.HasValue) continue;

                if (existingSet.Contains((log.KullaniciId.Value, log.IslemTarihi))) continue;

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

                newNotifications.Add(new Notification
                {
                    UserId = log.KullaniciId.Value,
                    Title = title,
                    Message = message,
                    Type = type,
                    Severity = severity,
                    CreatedAt = log.IslemTarihi,
                    IsRead = true, // Geçmiş bildirimler okundu olarak gelsin
                    RelatedVardiyaId = log.VardiyaId
                });
                
                // Add to set to prevent duplicates within the same batch if logs have identical timestamps
                existingSet.Add((log.KullaniciId.Value, log.IslemTarihi));
                count++;
            }

            if (newNotifications.Any())
            {
                _context.Notifications.AddRange(newNotifications);
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
