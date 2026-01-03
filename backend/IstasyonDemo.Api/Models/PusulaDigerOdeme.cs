using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class PusulaDigerOdeme
    {
        public int Id { get; set; }

        public int PusulaId { get; set; }
        public Pusula Pusula { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string TurKodu { get; set; } = string.Empty; // e.g., "CEK", "KUPON"

        [Required]
        [MaxLength(100)]
        public string TurAdi { get; set; } = string.Empty;  // e.g., "Müşteri Çeki"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }

        public bool Silinemez { get; set; } = false;
    }
}
