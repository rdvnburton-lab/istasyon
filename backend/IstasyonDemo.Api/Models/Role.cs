using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Ad { get; set; } = string.Empty; // admin, patron, vardiya sorumlusu

        [MaxLength(200)]
        public string? Aciklama { get; set; }

        public bool IsSystemRole { get; set; } = false; // Silinemez roller için
    }
}
