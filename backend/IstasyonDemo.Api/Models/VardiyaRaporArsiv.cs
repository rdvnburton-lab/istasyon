using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    /// <summary>
    /// Onaylanan vardiyaların hesaplanmış rapor verilerini ve PDF'lerini saklayan arşiv tablosu.
    /// Performans optimizasyonu için tüm rapor verileri JSON olarak saklanır.
    /// </summary>
    public class VardiyaRaporArsiv
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Kaynak vardiya ID - Her vardiya için tek bir arşiv kaydı olabilir
        /// </summary>
        public int VardiyaId { get; set; }
        
        /// <summary>
        /// İstasyon ID - Hızlı filtreleme için
        /// </summary>
        public int IstasyonId { get; set; }
        
        /// <summary>
        /// Vardiya tarihi - Hızlı sıralama ve filtreleme için
        /// </summary>
        public DateTime Tarih { get; set; }
        
        // ═══════════════════════════════════════════════════════════════
        // ÖZET DEĞERLER (Hızlı sorgu ve listeleme için denormalize edilmiş)
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Otomasyon sisteminden gelen toplam satış tutarı
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SistemToplam { get; set; }
        
        /// <summary>
        /// Personel pusula girişlerinden gelen toplam tahsilat
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TahsilatToplam { get; set; }
        
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
        
        /// <summary>
        /// Tahsilat - Sistem farkı
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Fark { get; set; }
        
        /// <summary>
        /// Fark yüzdesi
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal FarkYuzde { get; set; }
        
        /// <summary>
        /// Rapor durumu: UYUMLU, FARK_VAR, KRITIK_FARK
        /// </summary>
        [MaxLength(50)]
        public string Durum { get; set; } = "UYUMLU";
        
        // ═══════════════════════════════════════════════════════════════
        // JSON RAPORLAR (Detaylı veri - anlık erişim için saklanır)
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Karşılaştırma raporu detayları (ödeme yöntemleri, pompa satışları, vb.)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? KarsilastirmaRaporuJson { get; set; }
        
        /// <summary>
        /// Personel bazlı fark raporu detayları
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? FarkRaporuJson { get; set; }
        
        /// <summary>
        /// Pompa bazlı satış özetleri
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? PompaSatisRaporuJson { get; set; }
        
        /// <summary>
        /// Ödeme yöntemi detayları (nakit, kredi kartı, vb.)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? TahsilatDetayJson { get; set; }
        
        /// <summary>
        /// Gider kalemleri raporu
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? GiderRaporuJson { get; set; }
        
        // ═══════════════════════════════════════════════════════════════
        // PDF RAPORLAR (Binary olarak saklanır)
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Karşılaştırma raporu PDF içeriği
        /// </summary>
        public byte[]? KarsilastirmaPdfIcerik { get; set; }
        
        /// <summary>
        /// Fark raporu PDF içeriği
        /// </summary>
        public byte[]? FarkRaporuPdfIcerik { get; set; }
        
        /// <summary>
        /// Vardiya özet raporu PDF içeriği
        /// </summary>
        public byte[]? VardiyaOzetPdfIcerik { get; set; }
        
        // ═══════════════════════════════════════════════════════════════
        // ONAY BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════
        
        public int? OnaylayanId { get; set; }
        
        [MaxLength(100)]
        public string? OnaylayanAdi { get; set; }
        
        public DateTime? OnayTarihi { get; set; }
        
        /// <summary>
        /// Vardiyayı oluşturan sorumlu ID
        /// </summary>
        public int? SorumluId { get; set; }
        
        [MaxLength(100)]
        public string? SorumluAdi { get; set; }
        
        // ═══════════════════════════════════════════════════════════════
        // META BİLGİLER
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Arşiv kaydının oluşturulma tarihi
        /// </summary>
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime? GuncellemeTarihi { get; set; }
        
        // ═══════════════════════════════════════════════════════════════
        // NAVIGATION PROPERTIES
        // ═══════════════════════════════════════════════════════════════
        
        public Vardiya? Vardiya { get; set; }
        public Istasyon? Istasyon { get; set; }
    }
}
