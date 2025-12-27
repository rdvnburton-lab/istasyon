namespace IstasyonDemo.Api.Dtos
{
    public class IstasyonDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public bool Aktif { get; set; }
        public int FirmaId { get; set; }
        
        // 3 ayrı sorumlu ID
        public int? IstasyonSorumluId { get; set; }
        public int? VardiyaSorumluId { get; set; }
        public int? MarketSorumluId { get; set; }
        
        public string? ApiKey { get; set; }
        
        // Backend'den gelen hazır sorumlu adları (kolon gösterimi için)
        public string? IstasyonSorumlusu { get; set; }
        public string? VardiyaSorumlusu { get; set; }
        public string? MarketSorumlusu { get; set; }
        
        // Cihaz Kilidi & Sağlık Durumu
        public string? RegisteredDeviceId { get; set; }
        public DateTime? LastConnectionTime { get; set; }
        public bool IsOnline => LastConnectionTime.HasValue && LastConnectionTime.Value > DateTime.UtcNow.AddMinutes(-5);
    }

    public class CreateIstasyonDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public int FirmaId { get; set; }
        public int? IstasyonSorumluId { get; set; }
        public int? VardiyaSorumluId { get; set; }
        public int? MarketSorumluId { get; set; }
        public string? ApiKey { get; set; }
    }

    public class UpdateIstasyonDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public bool Aktif { get; set; }
        public int? IstasyonSorumluId { get; set; }
        public int? VardiyaSorumluId { get; set; }
        public int? MarketSorumluId { get; set; }
        public string? ApiKey { get; set; }
    }
}
