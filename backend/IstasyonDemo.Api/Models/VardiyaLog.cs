using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models
{
    public class VardiyaLog
    {
        public int Id { get; set; }
        
        public int VardiyaId { get; set; }
        public Vardiya? Vardiya { get; set; }
        
        [MaxLength(100)]
        public string Islem { get; set; } = string.Empty; // OLUSTURULDU, ONAYA_GONDERILDI, ONAYLANDI, REDDEDILDI, SILME_TALEP_EDILDI, SILINDI, SILME_REDDEDILDI
        
        [MaxLength(500)]
        public string? Aciklama { get; set; }
        
        public int? KullaniciId { get; set; }
        
        [MaxLength(100)]
        public string? KullaniciAdi { get; set; }
        
        [MaxLength(50)]
        public string? KullaniciRol { get; set; }
        
        public DateTime IslemTarihi { get; set; } = DateTime.UtcNow;
        
        [MaxLength(50)]
        public string? EskiDurum { get; set; }
        
        [MaxLength(50)]
        public string? YeniDurum { get; set; }
    }
}
