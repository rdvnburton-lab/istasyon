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
        public decimal BirimFiyat { get; set; }
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

    public class MutabakatViewModel
    {
        public object Vardiya { get; set; } = new object();
        public List<PersonelMutabakatOzetDto> PersonelOzetler { get; set; } = new();
        public FiloMutabakatOzetDto? FiloOzet { get; set; }
        public List<FiloMutabakatDetayDto> FiloDetaylari { get; set; } = new();
        public List<PusulaMutabakatDto> Pusulalar { get; set; } = new();
        public List<GiderMutabakatDto> Giderler { get; set; } = new();
        public GenelMutabakatOzetDto GenelOzet { get; set; } = new();
        public long _performanceMs { get; set; }
    }

    public class GenelMutabakatOzetDto
    {
        public decimal ToplamOtomasyon { get; set; }
        public decimal ToplamNakit { get; set; }
        public decimal ToplamKrediKarti { get; set; }
        public decimal ToplamGider { get; set; }
        public decimal ToplamPusula { get; set; } // Nakit + KK + Diğer
        public decimal Fark { get; set; } // (Pusula + Filo + Gider) - Otomasyon
    }

    public class PersonelMutabakatOzetDto
    {
        public string PersonelAdi { get; set; } = string.Empty;
        public int? PersonelId { get; set; }
        public decimal ToplamLitre { get; set; }
        public decimal ToplamTutar { get; set; }
        public int IslemSayisi { get; set; }
    }

    public class FiloMutabakatOzetDto
    {
        public decimal ToplamTutar { get; set; }
        public decimal ToplamLitre { get; set; }
        public int IslemSayisi { get; set; }
    }

    public class FiloMutabakatDetayDto
    {
        public string FiloAdi { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
        public decimal Litre { get; set; }
        public int IslemSayisi { get; set; }
    }

    public class PusulaMutabakatDto
    {
        public int Id { get; set; }
        public string PersonelAdi { get; set; } = string.Empty;
        public int? PersonelId { get; set; }
        public decimal Nakit { get; set; }
        public decimal KrediKarti { get; set; }
        public string? KrediKartiDetay { get; set; } // JSON String
        public List<PusulaDigerOdemeDto> DigerOdemeler { get; set; } = new();
        public List<PusulaVeresiyeDto> Veresiyeler { get; set; } = new();
        public string? Aciklama { get; set; }
        public decimal Toplam { get; set; }
    }


    public class GiderMutabakatDto
    {
        public int Id { get; set; }
        public string GiderTuru { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
        public string? Aciklama { get; set; }
    }
    public class VardiyaSummaryDto
    {
        public int Id { get; set; }
        public int IstasyonId { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public int Durum { get; set; }
        public decimal PompaToplam { get; set; }
        public decimal MarketToplam { get; set; }
        public decimal GenelToplam { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public string? DosyaAdi { get; set; }
        public string? RedNedeni { get; set; }
    }
}
