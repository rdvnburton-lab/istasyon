using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;

namespace IstasyonDemo.Api.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<RoleDto?> GetRoleByIdAsync(int id);
        Task<RoleDto> CreateRoleAsync(CreateRoleDto dto);
        Task UpdateRoleAsync(int id, UpdateRoleDto dto);
        Task DeleteRoleAsync(int id);
    }
}
