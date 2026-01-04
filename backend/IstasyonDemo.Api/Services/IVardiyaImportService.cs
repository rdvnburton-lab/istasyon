using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;

namespace IstasyonDemo.Api.Services
{
    public class VardiyaImportResult
    {
        public CreateVardiyaDto CreateDto { get; set; } = null!;
        public List<VardiyaTankEnvanteri> TankEnvanterleri { get; set; } = new();
        public string DosyaHash { get; set; } = string.Empty;
        public Istasyon Istasyon { get; set; } = null!;
        public string RawXmlContent { get; set; } = string.Empty;
        public byte[] ZipBytes { get; set; } = Array.Empty<byte>();
        public string TankDetailsJson { get; set; } = string.Empty;
        public string PumpDetailsJson { get; set; } = string.Empty;
        public string SaleDetailsJson { get; set; } = string.Empty;
    }

    public interface IVardiyaImportService
    {
        Task<VardiyaImportResult> ParseXmlZipAsync(Stream zipStream, string fileName, int userId);
    }
}
