using IstasyonDemo.Api.Models;
using System.Collections.Generic;

namespace IstasyonDemo.Api.Dtos.Reports
{
    public class MutabakatDto
    {
        public MutabakatVardiyaDto Vardiya { get; set; }
        public List<MutabakatPersonelOzetDto> PersonelOzetler { get; set; }
        public MutabakatFiloOzetDto FiloOzet { get; set; }
        public List<MutabakatFiloDetayDto> FiloDetaylari { get; set; }
        public List<MutabakatPusulaDto> Pusulalar { get; set; }
        public List<PompaGider> Giderler { get; set; }
    }

    public class MutabakatVardiyaDto
    {
        public int Id { get; set; }
        public int IstasyonId { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public string Durum { get; set; }
        public decimal PompaToplam { get; set; }
        public decimal MarketToplam { get; set; }
        public decimal GenelToplam { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public string? DosyaAdi { get; set; }
        public string? RedNedeni { get; set; }
    }

    public class MutabakatPersonelOzetDto
    {
        public string PersonelAdi { get; set; }
        public int? PersonelId { get; set; }
        public decimal ToplamLitre { get; set; }
        public decimal ToplamTutar { get; set; }
        public int IslemSayisi { get; set; }
    }

    public class MutabakatFiloOzetDto
    {
        public decimal ToplamTutar { get; set; }
        public decimal ToplamLitre { get; set; }
        public int IslemSayisi { get; set; }
    }

    public class MutabakatFiloDetayDto
    {
        public string FiloKodu { get; set; }
        public decimal Tutar { get; set; }
        public decimal Litre { get; set; }
    }

    public class MutabakatPusulaDto
    {
        public int Id { get; set; }
        public string PersonelAdi { get; set; }
        public int? PersonelId { get; set; }
        public decimal Nakit { get; set; }
        public decimal KrediKarti { get; set; }
        public decimal ParoPuan { get; set; }
        public decimal MobilOdeme { get; set; }
        public decimal Toplam { get; set; }
        public string? Aciklama { get; set; }
        public string? KrediKartiDetay { get; set; }
        public List<PusulaDigerOdemeDto>? DigerOdemeler { get; set; }
    }
}
