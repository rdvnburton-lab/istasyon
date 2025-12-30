namespace IstasyonDemo.Api.Dtos
{
    public class UserSettingsDto
    {
        public string Theme { get; set; } = "light";
        public bool NotificationsEnabled { get; set; }
        public bool EmailNotifications { get; set; }
        public string Language { get; set; } = "tr";
        public string? ExtraSettingsJson { get; set; }
    }
}
