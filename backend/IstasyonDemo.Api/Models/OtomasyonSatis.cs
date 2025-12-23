using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class OtomasyonSatis
    {
        public int Id { get; set; }
        
        public int VardiyaId { get; set; }
        
        public int? PersonelId { get; set; }
        
        [MaxLength(100)]
        public string PersonelAdi { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string PersonelKeyId { get; set; } = string.Empty;
        
        public int PompaNo { get; set; }
        
        public YakitTuru YakitTuru { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Litre { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BirimFiyat { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ToplamTutar { get; set; }
        
        public DateTime SatisTarihi { get; set; }
        
        public int? FisNo { get; set; }
        
        [MaxLength(20)]
        public string? Plaka { get; set; }
        
        // Navigation Properties
        public Vardiya? Vardiya { get; set; }
        public Personel? Personel { get; set; }
    }
}
