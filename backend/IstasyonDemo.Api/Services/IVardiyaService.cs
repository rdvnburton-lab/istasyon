using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;

namespace IstasyonDemo.Api.Services
{
    public interface IVardiyaService
    {
        Task<Vardiya> CreateVardiyaAsync(CreateVardiyaDto dto, int userId, string? userRole, string? userName);
        Task<Vardiya?> GetVardiyaByIdAsync(int id);
        Task<List<Vardiya>> GetOnayBekleyenlerAsync(int userId, string? userRole);
        Task OnayaGonderAsync(int id, int userId, string? userRole);
        Task<MutabakatViewModel> CalculateVardiyaFinancials(int vardiyaId);
        Task SilmeTalebiOlusturAsync(int id, SilmeTalebiDto dto, int userId, string? userRole, string? userName);
        Task OnaylaAsync(int id, OnayDto dto, int userId, string? userRole);
        Task ReddetAsync(int id, RedDto dto, int userId, string? userRole);
        Task ProcessXmlZipAsync(Stream zipStream, string fileName, int userId, string? userRole, string? userName);
        Task RestoreVardiyaDataAsync(int vardiyaId, int userId, string? userRole);
    }
}
