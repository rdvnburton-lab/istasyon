using System;
using System.Collections.Generic;

namespace IstasyonDemo.Api.Dtos
{
    public class VardiyaRaporuDto
    {
        public VardiyaRaporOzetDto Ozet { get; set; } = new();
        public List<VardiyaRaporItemDto> Vardiyalar { get; set; } = new();
    }

    public class VardiyaRaporOzetDto
    {
        public int ToplamVardiya { get; set; }
        public decimal ToplamTutar { get; set; }
        public decimal ToplamLitre { get; set; }
        public decimal ToplamIade { get; set; }
        public decimal ToplamGider { get; set; }
    }

    public class VardiyaRaporItemDto
    {
        public int Id { get; set; }
        public DateTime Tarih { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
        public string Durum { get; set; } = string.Empty;
    }
}
