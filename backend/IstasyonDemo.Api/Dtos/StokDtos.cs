using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Dtos
{
    public class TankGirisDto
    {
        public Guid Id { get; set; }
        public DateTime Tarih { get; set; }
        public string FaturaNo { get; set; } = string.Empty;
        public int YakitId { get; set; }
        public string YakitAd { get; set; } = string.Empty;
        public string YakitRenk { get; set; } = string.Empty;
        public decimal Litre { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal ToplamTutar { get; set; }
        public string Kaydeden { get; set; } = string.Empty;
        public string? GelisYontemi { get; set; }
        public string? Plaka { get; set; }
        public DateTime? UrunGirisTarihi { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTankGirisDto
    {
        [Required]
        public DateTime Tarih { get; set; }

        [Required]
        [MaxLength(50)]
        public string FaturaNo { get; set; } = string.Empty;

        [Required]
        public int YakitId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Litre 0'dan büyük olmalıdır.")]
        public decimal Litre { get; set; }

        public decimal BirimFiyat { get; set; }
        public decimal ToplamTutar { get; set; }
        public string? Kaydeden { get; set; }
    }

    public class TankStokOzetDto
    {
        public int YakitId { get; set; }
        public string YakitTuru { get; set; } = string.Empty;
        public string Renk { get; set; } = string.Empty;
        public decimal GecenAyDevir { get; set; }
        public decimal BuAyGiris { get; set; }
        public decimal BuAySatis { get; set; }
        public decimal KalanStok { get; set; }
    }

    // --- Invoice/Fatura Entry DTOs ---

    public class CreateTankGirisItemDto
    {
        [Required]
        public int YakitId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Litre { get; set; }

        public decimal BirimFiyat { get; set; }
    }

    public class CreateFaturaGirisDto
    {
        [Required]
        public DateTime Tarih { get; set; }

        [Required]
        [MaxLength(50)]
        public string FaturaNo { get; set; } = string.Empty;
        
        public string? Kaydeden { get; set; }
        public string? GelisYontemi { get; set; }
        public string? Plaka { get; set; }
        public DateTime? UrunGirisTarihi { get; set; }

        [Required]
        public List<CreateTankGirisItemDto> Kalemler { get; set; } = new();
    }

    public class StokGirisFisDto
    {
        public string FaturaNo { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
        public decimal ToplamTutar { get; set; }
        public string Kaydeden { get; set; } = string.Empty;
        public string? GelisYontemi { get; set; }
        public string? Plaka { get; set; }
        public DateTime? UrunGirisTarihi { get; set; }
        public decimal ToplamLitre { get; set; }
        public List<TankGirisDto> Kalemler { get; set; } = new();
    }
}
