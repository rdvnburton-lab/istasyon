using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class DosyaYuklemeAyarlari
    {
        [Key]
        public int Id { get; set; }

        public int? IstasyonId { get; set; } // Null ise global ayar
        public Istasyon? Istasyon { get; set; }

        [MaxLength(10)]
        public string DosyaUzantisi { get; set; } = ".xml"; // .xml, .txt

        [MaxLength(100)]
        public string HedefTablo { get; set; } = "VardiyaXmlLogs";

        public bool Aktif { get; set; } = true;

        public string? XmlNodeMappingJson { get; set; } // Dinamik property eşleştirme config
    }
}
