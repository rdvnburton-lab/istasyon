using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Dtos
{
    public class YakitDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string OtomasyonUrunAdi { get; set; } = string.Empty;
        public string Renk { get; set; } = string.Empty;
        public int Sira { get; set; }
        public string? TurpakUrunKodu { get; set; }
    }

    public class CreateYakitDto
    {
        [Required]
        [MaxLength(50)]
        public string Ad { get; set; } = string.Empty;

        [MaxLength(100)]
        public string OtomasyonUrunAdi { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Renk { get; set; } = "#3b82f6";

        public int Sira { get; set; } = 0;

        [MaxLength(50)]
        public string? TurpakUrunKodu { get; set; }
    }

    public class UpdateYakitDto
    {
        [Required]
        [MaxLength(50)]
        public string Ad { get; set; } = string.Empty;

        [MaxLength(100)]
        public string OtomasyonUrunAdi { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Renk { get; set; } = string.Empty;

        public int Sira { get; set; }

        [MaxLength(50)]
        public string? TurpakUrunKodu { get; set; }
    }
}
