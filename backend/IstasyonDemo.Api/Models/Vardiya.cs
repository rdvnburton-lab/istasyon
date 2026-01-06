using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class Vardiya
    {
        public int Id { get; set; }
        
        public int IstasyonId { get; set; }
        public Istasyon? Istasyon { get; set; }
        
        public DateTime BaslangicTarihi { get; set; }
        
        public DateTime? BitisTarihi { get; set; }
        
        public VardiyaDurum Durum { get; set; } = VardiyaDurum.ACIK;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PompaToplam { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MarketToplam { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GenelToplam { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Fark { get; set; }
        
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        
        public DateTime? GuncellemeTarihi { get; set; }
        
        [MaxLength(255)]
        public string? DosyaAdi { get; set; }

        [MaxLength(100)]
        public string? DosyaHash { get; set; }

        public byte[]? DosyaIcerik { get; set; }

        // Onay Mekanizması
        public int? OnaylayanId { get; set; }
        
        [MaxLength(100)]
        public string? OnaylayanAdi { get; set; }
        
        public DateTime? OnayTarihi { get; set; }
        
        [MaxLength(500)]
        public string? RedNedeni { get; set; }

        // Silme Talebi
        [MaxLength(500)]
        public string? SilinmeTalebiNedeni { get; set; }
        public int? SilinmeTalebiOlusturanId { get; set; }
        [MaxLength(100)]
        public string? SilinmeTalebiOlusturanAdi { get; set; }

        // Sorumlu (Vardiyayı Oluşturan)
        public int? SorumluId { get; set; }
        
        [MaxLength(100)]
        public string? SorumluAdi { get; set; }
        
        // ═══════════════════════════════════════════════════════════════
        // HESAPLANMIŞ ÖZET ALANLAR (Denormalizasyon - Performans için)
        // Vardiya onaylandığında bir kez hesaplanır ve kaydedilir
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Tüm pusulalardan gelen toplam tahsilat (Nakit + KK + Diğer)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TahsilatToplam { get; set; }
        
        /// <summary>
        /// Otomasyon sisteminden gelen toplam satış
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal OtomasyonToplam { get; set; }
        
        /// <summary>
        /// Filo satışları toplamı
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal FiloToplam { get; set; }
        
        /// <summary>
        /// Toplam gider
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiderToplam { get; set; }
        
        // ═══════════════════════════════════════════════════════════════
        // ARŞİV REFERANSI
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Bu vardiyaya ait arşiv kaydının ID'si (varsa)
        /// </summary>
        public int? RaporArsivId { get; set; }
        
        /// <summary>
        /// Vardiya arşivlenmiş mi?
        /// </summary>
        public bool Arsivlendi { get; set; } = false;
        
        public VardiyaRaporArsiv? RaporArsiv { get; set; }
        
        // Navigation Properties
        public ICollection<OtomasyonSatis> OtomasyonSatislar { get; set; } = new List<OtomasyonSatis>();
        public ICollection<FiloSatis> FiloSatislar { get; set; } = new List<FiloSatis>();
        public ICollection<Pusula> Pusulalar { get; set; } = new List<Pusula>();
        public ICollection<VardiyaPompaEndeks> PompaEndeksleri { get; set; } = new List<VardiyaPompaEndeks>();
        public ICollection<PompaGider> Giderler { get; set; } = new List<PompaGider>();
    }

    public class PompaGider
    {
        public int Id { get; set; }
        public int VardiyaId { get; set; }
        public Vardiya? Vardiya { get; set; }

        [MaxLength(50)]
        public string GiderTuru { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }

        [MaxLength(255)]
        public string Aciklama { get; set; } = string.Empty;

        public DateTime? BelgeTarihi { get; set; }
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    }
}
