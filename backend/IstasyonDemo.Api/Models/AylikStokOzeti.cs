using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    /// <summary>
    /// Aylık stok özeti - Her yakıt türü için aylık devir, giriş, satış ve kalan stok bilgilerini saklar
    /// </summary>
    public class AylikStokOzeti
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int YakitId { get; set; }

        [Required]
        public int Yil { get; set; }

        [Required]
        [Range(1, 12)]
        public int Ay { get; set; }

        /// <summary>
        /// Önceki aydan devir gelen stok (litre)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DevirStok { get; set; }

        /// <summary>
        /// Bu ay faturalardan giren toplam (litre)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AyGiris { get; set; }

        /// <summary>
        /// Bu ay onaylanan vardiyalardan satılan toplam (litre)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AySatis { get; set; }

        /// <summary>
        /// Ay sonunda kalan stok (litre) = DevirStok + AyGiris - AySatis
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal KalanStok { get; set; }

        /// <summary>
        /// Son hesaplama zamanı
        /// </summary>
        public DateTime HesaplamaZamani { get; set; }

        /// <summary>
        /// Ay kapandığında true yapılır - kapanan ayın değerleri değiştirilemez
        /// </summary>
        public bool Kilitli { get; set; }

        // Navigation
        public Yakit? Yakit { get; set; }
    }
}
