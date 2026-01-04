using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class PusulaVeresiye
    {
        [Key]
        public int Id { get; set; }

        public int PusulaId { get; set; }
        [ForeignKey("PusulaId")]
        public virtual Pusula Pusula { get; set; } = null!;

        public int CariKartId { get; set; }
        [ForeignKey("CariKartId")]
        public virtual CariKart CariKart { get; set; } = null!;

        [MaxLength(50)]
        public string? Plaka { get; set; }

        [MaxLength(50)]
        public string? YakitCinsi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Litre { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }

        [MaxLength(200)]
        public string? Aciklama { get; set; }
    }
}
