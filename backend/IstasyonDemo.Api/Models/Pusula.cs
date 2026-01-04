using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class Pusula
    {
        public int Id { get; set; }
        
        public int VardiyaId { get; set; }
        public Vardiya Vardiya { get; set; } = null!;
        
        [Required]
        [MaxLength(100)]
        public string PersonelAdi { get; set; } = string.Empty; // Personel adı
        
        public int? PersonelId { get; set; } // Opsiyonel personel ID
        
        [MaxLength(50)]
        public string PusulaTuru { get; set; } = "TAHSILAT"; // Default to TAHSILAT from SystemDefinitions
        
        // Ödeme Türleri
        [Column(TypeName = "decimal(18,2)")]
        public decimal Nakit { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal KrediKarti { get; set; }
        

        
        // Kredi Kartı Detayları (JSON olarak saklanacak)
        [MaxLength(2000)]
        public string? KrediKartiDetay { get; set; } // JSON: [{banka: "...", tutar: 123}]
        
        [MaxLength(500)]
        public string? Aciklama { get; set; }
        
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        
        public DateTime? GuncellemeTarihi { get; set; }
        
        // Computed property
        [NotMapped]
        public decimal Toplam => Nakit + KrediKarti + (DigerOdemeler?.Sum(x => x.Tutar) ?? 0) + (Veresiyeler?.Sum(x => x.Tutar) ?? 0);

        public ICollection<PusulaKrediKartiDetay> KrediKartiDetaylari { get; set; } = new List<PusulaKrediKartiDetay>();
        
        public ICollection<PusulaDigerOdeme> DigerOdemeler { get; set; } = new List<PusulaDigerOdeme>();
        
        public ICollection<PusulaVeresiye> Veresiyeler { get; set; } = new List<PusulaVeresiye>();
    }
}
