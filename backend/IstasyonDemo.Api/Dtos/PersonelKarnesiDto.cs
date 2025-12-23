using System;
using System.Collections.Generic;

namespace IstasyonDemo.Api.Dtos
{
    public class PersonelKarnesiDto
    {
        public PersonelDto? Personel { get; set; }
        public List<PersonelHareketDto> Hareketler { get; set; } = new();
        public PersonelKarneOzetDto Ozet { get; set; } = new();
    }

    public class PersonelHareketDto
    {
        public DateTime Tarih { get; set; }
        public int VardiyaId { get; set; }
        public decimal OtomasyonSatis { get; set; }
        public decimal ManuelTahsilat { get; set; }
        public decimal Fark { get; set; }
        public int AracSayisi { get; set; }
        public decimal Litre { get; set; }
        public string? Aciklama { get; set; }
    }

    public class PersonelKarneOzetDto
    {
        public decimal ToplamSatis { get; set; }
        public decimal ToplamTahsilat { get; set; }
        public decimal ToplamFark { get; set; }
        public decimal ToplamLitre { get; set; }
        public int AracSayisi { get; set; }
        public decimal OrtalamaLitre { get; set; }
        public decimal OrtalamaTutar { get; set; }
        public List<YakitDagilimiDto> YakitDagilimi { get; set; } = new();
    }

    public class YakitDagilimiDto
    {
        public string Yakit { get; set; } = string.Empty;
        public decimal Litre { get; set; }
        public decimal Tutar { get; set; }
        public decimal Oran { get; set; }
    }

    public class PersonelDto
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string? KeyId { get; set; }
        public string Rol { get; set; } = string.Empty;
    }
}
