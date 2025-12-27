using System.Collections.Generic;

namespace Istasyon.FileSync.Models
{
    public class FirmaDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public bool Aktif { get; set; }
    }

    public class IstasyonDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public bool Aktif { get; set; }
        public int FirmaId { get; set; }
        public string? ApiKey { get; set; }
        public string? RegisteredDeviceId { get; set; }
    }
}
