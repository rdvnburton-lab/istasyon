using IstasyonDemo.Api.Models;
using System.Threading.Tasks;

namespace IstasyonDemo.Api.Services
{
    public interface IYakitService
    {
        Task<Yakit?> IdentifyYakitAsync(string rawValue);
        Task<IEnumerable<Yakit>> GetAllYakitlarAsync();
        Task RefreshCacheAsync();
    }
}
