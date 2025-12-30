using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class UserSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [MaxLength(20)]
        public string Theme { get; set; } = "light"; // light, dark, system

        public bool NotificationsEnabled { get; set; } = true;

        public bool EmailNotifications { get; set; } = false;

        [MaxLength(5)]
        public string Language { get; set; } = "tr";

        // Future-proof: Store extra settings as JSON string if needed
        public string? ExtraSettingsJson { get; set; }
    }
}
