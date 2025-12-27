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
        
        [MaxLength(100)]
        public string? ApiKey { get; set; }

        [MaxLength(100)]
        public string? RegisteredDeviceId { get; set; }

        public DateTime? LastConnectionTime { get; set; }

        // Relation to Firma
        public int FirmaId { get; set; }
        public Firma? Firma { get; set; }

        // 3 ayrı sorumlu tipi
        public int? IstasyonSorumluId { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("IstasyonSorumluId")]
        public User? IstasyonSorumlu { get; set; }

        public int? VardiyaSorumluId { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("VardiyaSorumluId")]
        public User? VardiyaSorumlu { get; set; }

        public int? MarketSorumluId { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("MarketSorumluId")]
        public User? MarketSorumlu { get; set; }

        // Relations
        public ICollection<User> Kullanicilar { get; set; } = new List<User>();
        public ICollection<Personel> Calisanlar { get; set; } = new List<Personel>();
        public ICollection<Vardiya> Vardiyalar { get; set; } = new List<Vardiya>();
    }
}
