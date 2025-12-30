using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SettingsController : BaseController
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<UserSettingsDto>> GetSettings()
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == CurrentUserId);

            if (settings == null)
            {
                // Return defaults if not found
                return Ok(new UserSettingsDto());
            }

            return Ok(new UserSettingsDto
            {
                Theme = settings.Theme,
                NotificationsEnabled = settings.NotificationsEnabled,
                EmailNotifications = settings.EmailNotifications,
                Language = settings.Language,
                ExtraSettingsJson = settings.ExtraSettingsJson
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings(UserSettingsDto dto)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == CurrentUserId);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserId = CurrentUserId
                };
                _context.UserSettings.Add(settings);
            }

            settings.Theme = dto.Theme;
            settings.NotificationsEnabled = dto.NotificationsEnabled;
            settings.EmailNotifications = dto.EmailNotifications;
            settings.Language = dto.Language;
            settings.ExtraSettingsJson = dto.ExtraSettingsJson;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Ayarlar güncellendi." });
        }
    }
}
