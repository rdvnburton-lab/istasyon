using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class Vardiya
    {
        public int Id { get; set; }
        
        public int IstasyonId { get; set; } // Şimdilik sabit veya parametrik olabilir
        
        public DateTime BaslangicTarihi { get; set; }
        
        public DateTime? BitisTarihi { get; set; }
        
        public VardiyaDurum Durum { get; set; } = VardiyaDurum.ACIK;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PompaToplam { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MarketToplam { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GenelToplam { get; set; }
        
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        
        public DateTime? GuncellemeTarihi { get; set; }
        
        [MaxLength(255)]
        public string? DosyaAdi { get; set; }

        public byte[]? DosyaIcerik { get; set; }

        // Onay Mekanizması
        public int? OnaylayanId { get; set; }
        
        [MaxLength(100)]
        public string? OnaylayanAdi { get; set; }
        
        public DateTime? OnayTarihi { get; set; }
        
        [MaxLength(500)]
        public string? RedNedeni { get; set; }
        
        // Navigation Properties
        public ICollection<OtomasyonSatis> OtomasyonSatislar { get; set; } = new List<OtomasyonSatis>();
        public ICollection<FiloSatis> FiloSatislar { get; set; } = new List<FiloSatis>();
        public ICollection<Pusula> Pusulalar { get; set; } = new List<Pusula>();
    }
}
