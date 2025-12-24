using IstasyonDemo.Api.Models;

namespace IstasyonDemo.Api.Dtos
{
    public class MarketVardiyaDto
    {
        public int Id { get; set; }
        public int IstasyonId { get; set; }
        public string? IstasyonAdi { get; set; }
        public int SorumluId { get; set; }
        public string? SorumluAdi { get; set; }
        public DateTime Tarih { get; set; }
        public VardiyaDurum Durum { get; set; }
        public decimal ToplamSatisTutari { get; set; }
        public decimal ToplamTeslimatTutari { get; set; }
        public decimal ToplamFark { get; set; }
        public decimal ZRaporuTutari { get; set; }
        public string? ZRaporuNo { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }

    public class CreateMarketVardiyaDto
    {
        public DateTime Tarih { get; set; }
        public decimal ZRaporuTutari { get; set; }
        public string? ZRaporuNo { get; set; }
        
        public List<MarketZRaporuDto> ZRaporlari { get; set; } = new();
        public List<MarketTahsilatDto> Tahsilatlar { get; set; } = new();
        public List<MarketGiderDto> Giderler { get; set; } = new();
        public List<MarketGelirDto> Gelirler { get; set; } = new();
    }

    public class MarketZRaporuDto
    {
        public decimal GenelToplam { get; set; }
        public decimal Kdv0 { get; set; }
        public decimal Kdv1 { get; set; }
        public decimal Kdv10 { get; set; }
        public decimal Kdv20 { get; set; }
        public decimal KdvToplam { get; set; }
        public decimal KdvHaricToplam { get; set; }
    }

    public class MarketTahsilatDto
    {
        public int PersonelId { get; set; }
        public string? PersonelAdi { get; set; }
        public decimal Nakit { get; set; }
        public decimal KrediKarti { get; set; }
        public decimal ParoPuan { get; set; }
        public decimal SistemSatisTutari { get; set; }
        public decimal Toplam { get; set; }
        public string? Aciklama { get; set; }
    }

    public class MarketGiderDto
    {
        public GiderTuru GiderTuru { get; set; }
        public decimal Tutar { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public DateTime? BelgeTarihi { get; set; }
    }

    public class MarketGelirDto
    {
        public GelirTuru GelirTuru { get; set; }
        public decimal Tutar { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public DateTime? BelgeTarihi { get; set; }
    }
}
