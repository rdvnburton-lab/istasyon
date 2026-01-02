using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class IstasyonAyarlari
    {
        [Key]
        public int Id { get; set; }

        public int IstasyonId { get; set; }
        public Istasyon? Istasyon { get; set; }

        [MaxLength(100)]
        public string? Version { get; set; } // Örn: TURPAK Pumpomat V9.2.0

        public int UnitPriceDecimal { get; set; } = 2;
        public int AmountDecimal { get; set; } = 2;
        public int TotalDecimal { get; set; } = 2;

        public DateTime GuncellemeTarihi { get; set; } = DateTime.UtcNow;
    }
}
