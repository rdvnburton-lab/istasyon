using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models
{
    public class Firma
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Ad { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Adres { get; set; }

        public bool Aktif { get; set; } = true;

        // Ownership (Patron)
        public int? PatronId { get; set; }
        public User? Patron { get; set; }

        // Relations
        public ICollection<Istasyon> Istasyonlar { get; set; } = new List<Istasyon>();
    }
}
