using IstasyonDemo.Api.Models;

namespace IstasyonDemo.Api.Dtos
{
    public class CreateVardiyaDto
    {
        public int IstasyonId { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public string? DosyaAdi { get; set; }
        public string? DosyaHash { get; set; }
        public string? DosyaIcerik { get; set; }
        public decimal GenelToplam { get; set; } // Otomasyon Satışları Toplamı
        
        public List<CreateOtomasyonSatisDto> OtomasyonSatislar { get; set; } = new();
        public List<CreateFiloSatisDto> FiloSatislar { get; set; } = new();
    
        // M-ODEM (Mobil Ödeme) Otomatik Takip İçin
        public List<MobilOdemeDto> MobilOdemeler { get; set; } = new();
    }

    public class MobilOdemeDto 
    {
        public string PersonelIsmi { get; set; } = "";
        public string PersonelKeyId { get; set; } = "";
        public decimal Tutar { get; set; }
        public string Aciklama { get; set; } = "";
        public string TurKodu { get; set; } = "MOBIL_ODEME";
        public bool Silinemez { get; set; } = false;
    }

    public class CreateOtomasyonSatisDto
    {
        public string? PersonelAdi { get; set; }
        public string? PersonelKeyId { get; set; }
        public int PompaNo { get; set; }
        public string YakitTuru { get; set; } = string.Empty;
        public decimal Litre { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal ToplamTutar { get; set; }
        public DateTime SatisTarihi { get; set; }
        public int? FisNo { get; set; }
        public string? Plaka { get; set; }
    }

    public class CreateFiloSatisDto
    {
        public DateTime Tarih { get; set; }
        public string FiloKodu { get; set; } = string.Empty;
        public string Plaka { get; set; } = string.Empty;
        public string YakitTuru { get; set; } = string.Empty;
        public decimal Litre { get; set; }
        public decimal Tutar { get; set; }
        public int PompaNo { get; set; }
        public int FisNo { get; set; }
    }
    public class OnayDto
    {
        public int OnaylayanId { get; set; }
        public string OnaylayanAdi { get; set; } = string.Empty;
    }

    public class RedDto
    {
        public int OnaylayanId { get; set; }
        public string OnaylayanAdi { get; set; } = string.Empty;
        public string RedNedeni { get; set; } = string.Empty;
    }

    public class SilmeTalebiDto
    {
        public string Nedeni { get; set; } = string.Empty;
    }
}
