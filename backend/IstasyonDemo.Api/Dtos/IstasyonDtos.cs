namespace IstasyonDemo.Api.Dtos
{
    public class IstasyonDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public bool Aktif { get; set; }
        public int? ParentIstasyonId { get; set; }
        public int? PatronId { get; set; }
        public int? SorumluId { get; set; }
    }

    public class CreateIstasyonDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public int? ParentIstasyonId { get; set; }
        public int? PatronId { get; set; }
        public int? SorumluId { get; set; }
    }

    public class UpdateIstasyonDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public bool Aktif { get; set; }
        public int? SorumluId { get; set; }
    }
}
