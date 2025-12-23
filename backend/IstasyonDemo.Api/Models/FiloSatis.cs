using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class FiloSatis
    {
        public int Id { get; set; }
        
        public int VardiyaId { get; set; }
        
        public DateTime Tarih { get; set; }
        
        [MaxLength(50)]
        public string FiloKodu { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Plaka { get; set; } = string.Empty;
        
        public YakitTuru YakitTuru { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Litre { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }
        
        public int PompaNo { get; set; }
        
        public int FisNo { get; set; }
        
        // Navigation Properties
        public Vardiya? Vardiya { get; set; }
    }
}
