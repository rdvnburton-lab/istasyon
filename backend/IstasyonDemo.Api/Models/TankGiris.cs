using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class TankGiris
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime Tarih { get; set; }

        [Required]
        [MaxLength(50)]
        public string FaturaNo { get; set; } = string.Empty;

        public int YakitId { get; set; }

        [ForeignKey("YakitId")]
        public Yakit? Yakit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Litre { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BirimFiyat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ToplamTutar { get; set; }

        [MaxLength(100)]
        public string Kaydeden { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? GelisYontemi { get; set; }

        [MaxLength(20)]
        public string? Plaka { get; set; }

        public DateTime? UrunGirisTarihi { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
