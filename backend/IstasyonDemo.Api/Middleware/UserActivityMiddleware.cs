using IstasyonDemo.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IstasyonDemo.Api.Middleware
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UserActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("id")?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    // Update LastActivity every 5 minutes to avoid excessive DB writes
                    // Or just update it every request if performance is not a concern for this demo
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        user.LastActivity = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }

            await _next(context);
        }
    }
}
