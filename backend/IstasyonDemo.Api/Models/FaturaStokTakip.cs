using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    /// <summary>
    /// Fatura bazında stok takibi - FIFO mantığı ile hangi faturadan ne kadar kaldığını izler
    /// </summary>
    public class FaturaStokTakip
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FaturaNo { get; set; } = string.Empty;

        [Required]
        public int YakitId { get; set; }

        /// <summary>
        /// Fatura tarihi - FIFO sıralaması için kullanılır
        /// </summary>
        public DateTime FaturaTarihi { get; set; }

        /// <summary>
        /// Faturada giren toplam miktar (litre)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal GirenMiktar { get; set; }

        /// <summary>
        /// Henüz satılmamış kalan miktar (litre)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal KalanMiktar { get; set; }

        /// <summary>
        /// Tüm miktar satıldığında true
        /// </summary>
        public bool Tamamlandi { get; set; }

        /// <summary>
        /// Kayıt oluşturulma zamanı
        /// </summary>
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Son güncelleme zamanı
        /// </summary>
        public DateTime GuncellenmeTarihi { get; set; } = DateTime.UtcNow;

        // Navigation
        public Yakit? Yakit { get; set; }
    }
}
