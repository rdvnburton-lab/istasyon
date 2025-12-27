using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(RegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Kullanıcı adı zaten mevcut.");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Ad == request.Role);
            if (role == null)
            {
                return BadRequest("Geçersiz rol.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                PasswordHash = passwordHash,
                RoleId = role.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kullanıcı başarıyla oluşturuldu." });
        }

        [HttpPost("create-user")]
        [Authorize(Roles = "admin,patron")]
        public async Task<ActionResult<User>> CreateUser(CreateUserDto request)
        {
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Kullanıcı adı zaten mevcut.");
            }

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return BadRequest("Geçersiz rol.");
            }

            // Role validation
            if (currentUserRole == "patron")
            {
                if (role.Ad == "admin" || role.Ad == "patron")
                {
                    return Forbid("Patronlar sadece çalışan hesabı oluşturabilir.");
                }

                // Station validation
                if (request.IstasyonId.HasValue)
                {
                    var istasyon = await _context.Istasyonlar.Include(i => i.Firma).FirstOrDefaultAsync(i => i.Id == request.IstasyonId.Value);
                    if (istasyon == null || istasyon.Firma == null || istasyon.Firma.PatronId != currentUserId)
                    {
                        return BadRequest("Geçersiz istasyon. Bu istasyonun sahibi değilsiniz.");
                    }
                }
                else
                {
                    return BadRequest("İstasyon ID zorunludur.");
                }
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                PasswordHash = passwordHash,
                RoleId = role.Id,
                IstasyonId = request.IstasyonId,
                AdSoyad = request.AdSoyad,
                Telefon = request.Telefon,
                FotografData = request.FotografData
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Handle Firma assignment for Patron
            if (role.Ad == "patron" && request.FirmaId.HasValue)
            {
                if (currentUserRole != "admin")
                {
                    return Forbid("Sadece yöneticiler patron atayabilir.");
                }

                var firma = await _context.Firmalar.FindAsync(request.FirmaId.Value);
                if (firma != null)
                {
                    firma.PatronId = user.Id;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Kullanıcı başarıyla oluşturuldu.", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    return BadRequest("Kullanıcı bulunamadı.");
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return BadRequest("Yanlış şifre.");
                }

                user.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                string token = CreateToken(user);

                var response = new AuthResponseDto
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role?.Ad ?? "User"
                };

                if (response.Role == "patron")
                {
                    var firma = await _context.Firmalar
                        .Include(f => f.Istasyonlar)
                        .FirstOrDefaultAsync(f => f.PatronId == user.Id);

                    if (firma != null)
                    {
                        response.FirmaAdi = firma.Ad;
                        response.Istasyonlar = firma.Istasyonlar
                            .Where(i => i.Aktif)
                            .Select(i => new SimpleIstasyonDto { Id = i.Id, Ad = i.Ad })
                            .ToList();
                    }
                }
                else if (user.IstasyonId.HasValue)
                {
                     var istasyon = await _context.Istasyonlar.FindAsync(user.IstasyonId.Value);
                     if (istasyon != null)
                     {
                         response.Istasyonlar.Add(new SimpleIstasyonDto { Id = istasyon.Id, Ad = istasyon.Ad });
                     }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use ILogger here if injected)
                Console.WriteLine($"Login Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        private string CreateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "super_secret_key_change_this_in_production_12345"; // Fallback for dev

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.Ad ?? "User"),
                new Claim("id", user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
