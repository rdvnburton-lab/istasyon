using System;
using System.Collections.Generic;

namespace IstasyonDemo.Api.Dtos
{
    public class FarkRaporuDto
    {
        public FarkRaporOzetDto Ozet { get; set; } = new();
        public List<FarkRaporItemDto> Vardiyalar { get; set; } = new();
    }

    public class FarkRaporOzetDto
    {
        public decimal ToplamFark { get; set; }
        public decimal ToplamAcik { get; set; }
        public decimal ToplamFazla { get; set; }
        public int VardiyaSayisi { get; set; }
        public int AcikVardiyaSayisi { get; set; }
        public int FazlaVardiyaSayisi { get; set; }
    }

    public class FarkRaporItemDto
    {
        public int VardiyaId { get; set; }
        public DateTime Tarih { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public decimal OtomasyonToplam { get; set; }
        public decimal TahsilatToplam { get; set; }
        public decimal Fark { get; set; }
        public string Durum { get; set; } = string.Empty;
        public List<PersonelFarkDto> PersonelFarklari { get; set; } = new();
    }

    public class PersonelFarkDto
    {
        public string PersonelAdi { get; set; } = string.Empty;
        public string PersonelKeyId { get; set; } = string.Empty;
        public decimal Otomasyon { get; set; }
        public decimal Tahsilat { get; set; }
        public decimal Fark { get; set; }
    }
}
