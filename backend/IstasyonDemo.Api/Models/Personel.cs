using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models
{
    public class Personel
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string OtomasyonAdi { get; set; } = string.Empty; // Otomasyon sistemindeki kısa ad
        
        [Required]
        [MaxLength(200)]
        public string AdSoyad { get; set; } = string.Empty; // Tam ad soyad
        
        [MaxLength(20)]
        public string? KeyId { get; set; } // El terminali/Otomasyon Key ID
        
        public PersonelRol Rol { get; set; } = PersonelRol.POMPACI;
        
        public bool Aktif { get; set; } = true;
    }
}
