namespace IstasyonDemo.Api.Dtos
{
    public class FirmaDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public bool Aktif { get; set; }
        public int? PatronId { get; set; }
    }

    public class CreateFirmaDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public int? PatronId { get; set; }
    }

    public class UpdateFirmaDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public bool Aktif { get; set; }
    }
}
