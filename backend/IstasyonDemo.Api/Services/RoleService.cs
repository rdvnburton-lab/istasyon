using AutoMapper;
using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Services
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public RoleService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _context.Roles.ToListAsync();
            return _mapper.Map<IEnumerable<RoleDto>>(roles);
        }

        public async Task<RoleDto?> GetRoleByIdAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            return _mapper.Map<RoleDto>(role);
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
        {
            if (await _context.Roles.AnyAsync(r => r.Ad == dto.Ad))
            {
                throw new InvalidOperationException("Bu isimde bir rol zaten mevcut.");
            }

            var role = _mapper.Map<Role>(dto);
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return _mapper.Map<RoleDto>(role);
        }

        public async Task UpdateRoleAsync(int id, UpdateRoleDto dto)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) throw new KeyNotFoundException("Rol bulunamadı.");

            if (role.IsSystemRole)
            {
                throw new InvalidOperationException("Sistem rolleri düzenlenemez.");
            }

            if (await _context.Roles.AnyAsync(r => r.Ad == dto.Ad && r.Id != id))
            {
                throw new InvalidOperationException("Bu isimde bir rol zaten mevcut.");
            }

            _mapper.Map(dto, role);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) throw new KeyNotFoundException("Rol bulunamadı.");

            if (role.IsSystemRole)
            {
                throw new InvalidOperationException("Sistem rolleri silinemez.");
            }

            if (await _context.Users.AnyAsync(u => u.RoleId == id))
            {
                throw new InvalidOperationException("Bu role atanmış kullanıcılar olduğu için silinemez.");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }
    }
}
