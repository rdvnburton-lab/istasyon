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

        // Tank Envanteri (XML'den okunan)
        public List<CreateVardiyaTankEnvanteriDto> TankEnvanterleri { get; set; } = new();
    }

    public class CreateVardiyaTankEnvanteriDto
    {
        public int TankNo { get; set; }
        public string TankAdi { get; set; } = string.Empty;
        public string YakitTipi { get; set; } = string.Empty;
        public decimal BaslangicStok { get; set; }
        public decimal BitisStok { get; set; }
        public decimal SatilanMiktar { get; set; }
        public decimal SevkiyatMiktar { get; set; }
        // Hesaplanan alanlar backend'de de hesaplanabilir ama XML'den geliyorsa alalım
        // XML yapısını bilmiyorum, ama genelde raw data gelir.
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

        // New Fields from XML
        public string FiloAdi { get; set; } = string.Empty; // FleetName
        public string TagNr { get; set; } = string.Empty; // TagNr
        public int MotorSaati { get; set; } // EngineHour
        public int Kilometre { get; set; } // Odometer
        public int SatisTuru { get; set; } // TxnType
        public int TabancaNo { get; set; } // NozzleNr
        public int OdemeTuru { get; set; } // PaymentType
        public string YazarKasaPlaka { get; set; } = string.Empty; // ECRPlate
        public int YazarKasaFisNo { get; set; } // ECRReceiptNr
        public decimal PuanKullanimi { get; set; } // Redemption
        public decimal IndirimTutar { get; set; } // DiscountAmount
        public int KazanilanPuan { get; set; } // EarnedPoints
        public decimal KazanilanPara { get; set; } // EarnedMoney
        public string? SadakatKartNo { get; set; } // LoyaltyCardNo
        public int SadakatKartTipi { get; set; } // LoyaltyCardType
        public decimal TamBirimFiyat { get; set; } // FullUnitPrice
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

        // New Fields from XML
        public string FiloAdi { get; set; } = string.Empty; // FleetName
        public string TagNr { get; set; } = string.Empty; // TagNr
        public int MotorSaati { get; set; } // EngineHour
        public int Kilometre { get; set; } // Odometer
        public int SatisTuru { get; set; } // TxnType
        public int TabancaNo { get; set; } // NozzleNr
        public int OdemeTuru { get; set; } // PaymentType
        public string YazarKasaPlaka { get; set; } = string.Empty; // ECRPlate
        public int YazarKasaFisNo { get; set; } // ECRReceiptNr
        public decimal PuanKullanimi { get; set; } // Redemption
        public decimal IndirimTutar { get; set; } // DiscountAmount
        public int KazanilanPuan { get; set; } // EarnedPoints
        public decimal KazanilanPara { get; set; } // EarnedMoney
        public string? SadakatKartNo { get; set; } // LoyaltyCardNo
        public int SadakatKartTipi { get; set; } // LoyaltyCardType
        public decimal TamBirimFiyat { get; set; } // FullUnitPrice
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
