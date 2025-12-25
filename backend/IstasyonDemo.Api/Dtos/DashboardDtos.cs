namespace IstasyonDemo.Api.Dtos
{
    public class SorumluDashboardDto
    {
        public string KullaniciAdi { get; set; } = string.Empty;
        public string AdSoyad { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string FirmaAdi { get; set; } = string.Empty;
        public string IstasyonAdi { get; set; } = string.Empty;
        public int? AktifVardiyaId { get; set; }
        public int? AktifMarketVardiyaId { get; set; }
        public DateTime? SonVardiyaTarihi { get; set; }
        public decimal SonVardiyaTutar { get; set; }
        public DateTime? SonMarketVardiyaTarihi { get; set; }
        public decimal SonMarketVardiyaTutar { get; set; }
        public int BekleyenOnaySayisi { get; set; }
        public int BekleyenMarketOnaySayisi { get; set; }
    }
}
