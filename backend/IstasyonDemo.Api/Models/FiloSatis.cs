using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class FiloSatis
    {
        public int Id { get; set; }
        
        public int VardiyaId { get; set; }
        
        public DateTime Tarih { get; set; }
        
        [MaxLength(50)]
        public string FiloKodu { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Plaka { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string YakitTuru { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Litre { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tutar { get; set; }
        
        public int PompaNo { get; set; }
        
        public int FisNo { get; set; }

        // New Fields from XML
        [MaxLength(100)]
        public string FiloAdi { get; set; } = string.Empty; // FleetName

        [MaxLength(50)]
        public string TagNr { get; set; } = string.Empty; // TagNr

        public int MotorSaati { get; set; } // EngineHour

        public int Kilometre { get; set; } // Odometer

        public int SatisTuru { get; set; } // TxnType

        public int TabancaNo { get; set; } // NozzleNr

        public int OdemeTuru { get; set; } // PaymentType

        [MaxLength(20)]
        public string YazarKasaPlaka { get; set; } = string.Empty; // ECRPlate

        public int YazarKasaFisNo { get; set; } // ECRReceiptNr

        [Column(TypeName = "decimal(18,2)")]
        public decimal PuanKullanimi { get; set; } // Redemption

        [Column(TypeName = "decimal(18,2)")]
        public decimal IndirimTutar { get; set; } // DiscountAmount

        public int KazanilanPuan { get; set; } // EarnedPoints

        [Column(TypeName = "decimal(18,2)")]
        public decimal KazanilanPara { get; set; } // EarnedMoney

        [MaxLength(50)]
        public string? SadakatKartNo { get; set; } // LoyaltyCardNo

        public int SadakatKartTipi { get; set; } // LoyaltyCardType

        [Column(TypeName = "decimal(18,2)")]
        public decimal TamBirimFiyat { get; set; } // FullUnitPrice
        
        // Navigation Properties
        public Vardiya? Vardiya { get; set; }
    }
}
