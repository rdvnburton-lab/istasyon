using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models
{
    public class Istasyon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Ad { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Adres { get; set; }

        public bool Aktif { get; set; } = true;

        // Hierarchy
        public int? ParentIstasyonId { get; set; }
        public Istasyon? ParentIstasyon { get; set; }
        public ICollection<Istasyon> AltIstasyonlar { get; set; } = new List<Istasyon>();

        // Ownership (Patron)
        public int? PatronId { get; set; }
        public User? Patron { get; set; }

        public int? SorumluId { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("SorumluId")]
        public User? Sorumlu { get; set; }

        // Relations
        // Relations
        public ICollection<User> Kullanicilar { get; set; } = new List<User>(); // Renamed from Personeller to avoid confusion
        public ICollection<Personel> Calisanlar { get; set; } = new List<Personel>();
        public ICollection<Vardiya> Vardiyalar { get; set; } = new List<Vardiya>();
    }
}
