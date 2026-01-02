using System;

namespace IstasyonDemo.Api.Models
{
    public class VardiyaTankEnvanteri
    {
        public int Id { get; set; }
        public int VardiyaId { get; set; }
        public virtual Vardiya Vardiya { get; set; }

        // Tank Bilgileri
        public int TankNo { get; set; }
        public string TankAdi { get; set; } = string.Empty;        // "Kurşunsuz", "Motorin"
        public string YakitTipi { get; set; } = string.Empty;      // Normalized fuel type

        // Stok Bilgileri (Litre)
        public decimal BaslangicStok { get; set; }  // PreviousVolume
        public decimal BitisStok { get; set; }      // CurrentVolume
        public decimal SatilanMiktar { get; set; } // Delta
        public decimal SevkiyatMiktar { get; set; }// DeliveryVolume

        // Hesaplanan Değerler
        public decimal BeklenenTuketim { get; set; }  // BaslangicStok + Sevkiyat - BitisStok
        public decimal FarkMiktar { get; set; }       // BeklenenTuketim - SatilanMiktar (Kayıp/Kaçak)

        public DateTime KayitTarihi { get; set; }
    }
}
