using IstasyonDemo.Api.Dtos;

namespace IstasyonDemo.Api.Services
{
    public interface IVardiyaFinancialService
    {
        Task<MutabakatViewModel> CalculateVardiyaFinancials(int vardiyaId);
        Task ProcessVardiyaApproval(int vardiyaId, int onaylayanId);
    }
}
