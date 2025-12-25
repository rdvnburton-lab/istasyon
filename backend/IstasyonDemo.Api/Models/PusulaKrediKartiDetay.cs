using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class PusulaKrediKartiDetay
    {
        public int Id { get; set; }

        public int PusulaId { get; set; }
        public Pusula Pusula { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string BankaAdi { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }
    }
}
