using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models;

public class OtomatikDosya
{
    public int Id { get; set; }
    
    public int? IstasyonId { get; set; }
    public Istasyon? Istasyon { get; set; }
    
    [Required]
    public string DosyaAdi { get; set; } = string.Empty;
    
    [Required]
    public byte[] DosyaIcerigi { get; set; } = Array.Empty<byte>();
    
    [Required]
    public string Hash { get; set; } = string.Empty;
    
    public DateTime YuklemeTarihi { get; set; } = DateTime.UtcNow;
    
    public bool Islendi { get; set; } = false;
    
    public DateTime? IslenmeTarihi { get; set; }
    
    public string? HataMesaji { get; set; }
}
