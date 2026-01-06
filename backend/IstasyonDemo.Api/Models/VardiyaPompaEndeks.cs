using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class VardiyaPompaEndeks
    {
        public int Id { get; set; }

        public int VardiyaId { get; set; }
        public Vardiya Vardiya { get; set; } = null!;

        public int PompaNo { get; set; }
        public int TabancaNo { get; set; }

        [MaxLength(50)]
        public string YakitTuru { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaslangicEndeks { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BitisEndeks { get; set; }

        [NotMapped]
        public decimal FarkEndeks => BitisEndeks - BaslangicEndeks;
    }
}
