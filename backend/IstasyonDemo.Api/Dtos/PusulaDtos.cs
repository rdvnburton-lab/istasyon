namespace IstasyonDemo.Api.Dtos
{
    public class CreatePusulaDto
    {
        public int VardiyaId { get; set; }
        public string PersonelAdi { get; set; } = string.Empty;
        public int? PersonelId { get; set; }
        public decimal Nakit { get; set; }
        public decimal KrediKarti { get; set; }
        public string? KrediKartiDetay { get; set; }
        public List<PusulaKrediKartiDetayDto>? KrediKartiDetayList { get; set; }
        public string? Aciklama { get; set; }
        public string? PusulaTuru { get; set; }
        public List<PusulaDigerOdemeDto>? DigerOdemeList { get; set; }
        public List<PusulaVeresiyeDto>? VeresiyeList { get; set; }
    }

    public class UpdatePusulaDto
    {
        public decimal Nakit { get; set; }
        public decimal KrediKarti { get; set; }
        public string? KrediKartiDetay { get; set; }
        public List<PusulaKrediKartiDetayDto>? KrediKartiDetayList { get; set; }
        public string? Aciklama { get; set; }
        public string? PusulaTuru { get; set; }
        public List<PusulaDigerOdemeDto>? DigerOdemeList { get; set; }
        public List<PusulaVeresiyeDto>? VeresiyeList { get; set; }
    }

    public class PusulaKrediKartiDetayDto
    {
        public string BankaAdi { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
    }

    public class PusulaDigerOdemeDto
    {
        public string TurKodu { get; set; } = string.Empty;
        public string TurAdi { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
        public bool Silinemez { get; set; }
    }

    public class PusulaVeresiyeDto
    {
        public int CariKartId { get; set; }
        public string? CariAd { get; set; }
        public string? Plaka { get; set; }
        public decimal Litre { get; set; }
        public decimal Tutar { get; set; }
        public string? Aciklama { get; set; }
    }
}
