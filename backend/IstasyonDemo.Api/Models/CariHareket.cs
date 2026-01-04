using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class CariHareket
    {
        [Key]
        public int Id { get; set; }

        public int CariKartId { get; set; }
        [ForeignKey("CariKartId")]
        public virtual CariKart CariKart { get; set; }

        public DateTime Tarih { get; set; }

        // SATIS, TAHSILAT, ADE, VIRMAN
        [Required]
        [MaxLength(20)]
        public string IslemTipi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }

        [MaxLength(500)]
        public string? Aciklama { get; set; }

        [MaxLength(50)]
        public string? BelgeNo { get; set; }

        // Opsiyonel: Hangi vardiyadan geldi?
        public int? VardiyaId { get; set; }

        public int OlusturanId { get; set; }

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    }
}
