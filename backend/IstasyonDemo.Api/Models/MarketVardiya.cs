using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class MarketVardiya
    {
        public int Id { get; set; }
        
        public int IstasyonId { get; set; }
        public Istasyon? Istasyon { get; set; }
        
        public int SorumluId { get; set; }
        public User? Sorumlu { get; set; }
        
        public DateTime Tarih { get; set; }
        
        public VardiyaDurum Durum { get; set; } = VardiyaDurum.ACIK;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ToplamSatisTutari { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ToplamTeslimatTutari { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ToplamFark { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ZRaporuTutari { get; set; }
        
        [MaxLength(50)]
        public string? ZRaporuNo { get; set; }
        
        public int? OnaylayanId { get; set; }
        public DateTime? OnayTarihi { get; set; }
        
        [MaxLength(500)]
        public string? RedNedeni { get; set; }
        
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<MarketZRaporu> ZRaporlari { get; set; } = new List<MarketZRaporu>();
        public ICollection<MarketTahsilat> Tahsilatlar { get; set; } = new List<MarketTahsilat>();
        public ICollection<MarketGider> Giderler { get; set; } = new List<MarketGider>();
        public ICollection<MarketGelir> Gelirler { get; set; } = new List<MarketGelir>();
    }

    public class MarketZRaporu
    {
        public int Id { get; set; }
        public int MarketVardiyaId { get; set; }
        public MarketVardiya? MarketVardiya { get; set; }
        
        public DateTime Tarih { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GenelToplam { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Kdv0 { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Kdv1 { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Kdv10 { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Kdv20 { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal KdvToplam { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal KdvHaricToplam { get; set; }
        
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    }

    public class MarketTahsilat
    {
        public int Id { get; set; }
        public int MarketVardiyaId { get; set; }
        public MarketVardiya? MarketVardiya { get; set; }
        
        public int PersonelId { get; set; }
        public Personel? Personel { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Nakit { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal KrediKarti { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ParoPuan { get; set; }
        
        public int? BankaId { get; set; }
        public SystemDefinition? Banka { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SistemSatisTutari { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Toplam { get; set; }
        
        [MaxLength(255)]
        public string? Aciklama { get; set; }
        
        public string? KrediKartiDetayJson { get; set; }
        
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    }

    public class MarketGider
    {
        public int Id { get; set; }
        public int MarketVardiyaId { get; set; }
        public MarketVardiya? MarketVardiya { get; set; }
        
        [MaxLength(50)]
        public string GiderTuru { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }
        
        [MaxLength(255)]
        public string Aciklama { get; set; } = string.Empty;
        
        public DateTime? BelgeTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    }

    public class MarketGelir
    {
        public int Id { get; set; }
        public int MarketVardiyaId { get; set; }
        public MarketVardiya? MarketVardiya { get; set; }
        
        [MaxLength(50)]
        public string GelirTuru { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }
        
        [MaxLength(255)]
        public string Aciklama { get; set; } = string.Empty;
        
        public DateTime? BelgeTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    }
}
