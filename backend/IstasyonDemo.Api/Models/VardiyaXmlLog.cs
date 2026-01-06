using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class VardiyaXmlLog
    {
        [Key]
        public int Id { get; set; }

        public int IstasyonId { get; set; }
        public Istasyon? Istasyon { get; set; }

        public int? VardiyaId { get; set; } // Vardiya oluşturulduktan sonra güncellenecek
        public Vardiya? Vardiya { get; set; }

        [MaxLength(255)]
        public string DosyaAdi { get; set; } = string.Empty;

        public byte[]? ZipDosyasi { get; set; } // Orijinal ZIP dosyası
        public string? XmlIcerik { get; set; } // XML İçeriği (Text olarak)


        public DateTime YuklemeTarihi { get; set; } = DateTime.UtcNow;
    }
}
