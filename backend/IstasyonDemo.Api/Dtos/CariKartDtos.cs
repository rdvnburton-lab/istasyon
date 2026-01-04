using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Dtos
{
    public class CariKartDto
    {
        public int Id { get; set; }
        public int IstasyonId { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? YetkiliKisi { get; set; }
        public string? TCKN_VKN { get; set; }
        public string? VergiDairesi { get; set; }
        public string? Adres { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public decimal Bakiye { get; set; }
        public decimal Limit { get; set; }
        public string? Kod { get; set; }
        public bool Aktif { get; set; }
    }

    public class CreateCariKartDto
    {
        [Required]
        public int IstasyonId { get; set; }
        
        [Required]
        [MaxLength(150)]
        public string Ad { get; set; } = string.Empty;
        
        public string? YetkiliKisi { get; set; }
        public string? TCKN_VKN { get; set; }
        public string? VergiDairesi { get; set; }
        public string? Adres { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public decimal Limit { get; set; }
        public string? Kod { get; set; }
    }

    public class UpdateCariKartDto
    {
        [Required]
        [MaxLength(150)]
        public string Ad { get; set; } = string.Empty;
        
        public string? YetkiliKisi { get; set; }
        public string? TCKN_VKN { get; set; }
        public string? VergiDairesi { get; set; }
        public string? Adres { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public decimal Limit { get; set; }
        public string? Kod { get; set; }
        public bool Aktif { get; set; }
    }

    public class CariHareketDto
    {
        public int Id { get; set; }
        public int CariKartId { get; set; }
        public int? VardiyaId { get; set; }
        public DateTime Tarih { get; set; }
        public decimal Tutar { get; set; }
        public string IslemTipi { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public DateTime KayitTarihi { get; set; }
    }

    public class CreateTahsilatDto
    {
        public decimal Tutar { get; set; }
        public string? Aciklama { get; set; }
    }
}
