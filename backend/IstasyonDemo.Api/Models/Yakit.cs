using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models
{
    public class Yakit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Ad { get; set; } = string.Empty; // Örn: Motorin, Benzin

        [MaxLength(100)]
        public string OtomasyonUrunAdi { get; set; } = string.Empty; // Örn: "MOTORIN", "DIZEL" (Eşleşme için)

        [MaxLength(20)]
        public string Renk { get; set; } = "#3b82f6"; // UI rengi

        public int Sira { get; set; } = 0;

        [MaxLength(50)]
        public string? TurpakUrunKodu { get; set; } // Örn: "4,5" veya "6,7,8"
    }
}
