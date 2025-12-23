namespace IstasyonDemo.Api.Dtos
{
    public class CreatePusulaDto
    {
        public int VardiyaId { get; set; }
        public string PersonelAdi { get; set; } = string.Empty;
        public int? PersonelId { get; set; }
        public decimal Nakit { get; set; }
        public decimal KrediKarti { get; set; }
        public decimal ParoPuan { get; set; }
        public decimal MobilOdeme { get; set; }
        public string? KrediKartiDetay { get; set; }
        public string? Aciklama { get; set; }
    }

    public class UpdatePusulaDto
    {
        public decimal Nakit { get; set; }
        public decimal KrediKarti { get; set; }
        public decimal ParoPuan { get; set; }
        public decimal MobilOdeme { get; set; }
        public string? KrediKartiDetay { get; set; }
        public string? Aciklama { get; set; }
    }
}
