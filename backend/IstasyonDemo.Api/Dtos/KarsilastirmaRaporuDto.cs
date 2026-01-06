using System;
using System.Collections.Generic;

namespace IstasyonDemo.Api.Dtos
{
    public class KarsilastirmaRaporuDto
    {
        public int VardiyaId { get; set; }
        public DateTime Tarih { get; set; }
        public decimal SistemToplam { get; set; }
        public decimal TahsilatToplam { get; set; }
        public decimal Fark { get; set; }
        public decimal FarkYuzde { get; set; }
        public string Durum { get; set; } = string.Empty;
        public List<KarsilastirmaDetayDto> Detaylar { get; set; } = new();
        public List<PompaSatisOzetDto> PompaSatislari { get; set; } = new();
    }

    public class KarsilastirmaDetayDto
    {
        public string OdemeYontemi { get; set; } = string.Empty;
        public decimal SistemTutar { get; set; }
        public decimal TahsilatTutar { get; set; }
        public decimal Fark { get; set; }
    }

    public class PompaSatisOzetDto
    {
        public int PompaNo { get; set; }
        public string YakitTuru { get; set; } = string.Empty;
        public decimal Litre { get; set; }
        public decimal ToplamTutar { get; set; }
        public int IslemSayisi { get; set; }
        public decimal BaslangicEndeks { get; set; }
        public decimal BitisEndeks { get; set; }
    }
}
