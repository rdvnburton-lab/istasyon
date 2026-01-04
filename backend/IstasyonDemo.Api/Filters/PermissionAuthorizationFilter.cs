using IstasyonDemo.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IstasyonDemo.Api.Filters
{
    public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly string _resourceKey;
        private readonly AppDbContext _context;

        public PermissionAuthorizationFilter(string resourceKey, AppDbContext context)
        {
            _resourceKey = resourceKey;
            _context = context;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Admin her zaman yetkili
            if (roleClaim.ToLower() == "admin")
            {
                return;
            }

            // Rolün yetkilerini kontrol et
            // Performans için cache mekanizması eklenebilir ama şimdilik doğrudan DB sorgusu
            var hasPermission = await _context.RolePermissions
                .AnyAsync(rp => rp.Role != null && rp.Role.Ad == roleClaim && rp.ResourceKey == _resourceKey);

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
