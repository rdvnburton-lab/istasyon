namespace IstasyonDemo.Api.Dtos
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public bool IsSystemRole { get; set; }
    }

    public class CreateRoleDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
    }

    public class UpdateRoleDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
    }
}
