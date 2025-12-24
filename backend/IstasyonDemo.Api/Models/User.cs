using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "User"; // Admin, User, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? AdSoyad { get; set; }

        [MaxLength(20)]
        public string? Telefon { get; set; }

        public int? IstasyonId { get; set; }
        public Istasyon? Istasyon { get; set; }
    }
}
