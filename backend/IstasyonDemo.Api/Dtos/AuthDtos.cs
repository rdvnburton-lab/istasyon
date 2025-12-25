using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Dtos
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "User";
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class CreateUserDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        public int? IstasyonId { get; set; }
        public string? AdSoyad { get; set; }
        public string? Telefon { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? IstasyonId { get; set; }
        public string? IstasyonAdi { get; set; }
        public string? AdSoyad { get; set; }
        public string? Telefon { get; set; }
    }

    public class UpdateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int RoleId { get; set; }
        public int? IstasyonId { get; set; }
        public string? AdSoyad { get; set; }
        public string? Telefon { get; set; }
    }
}
