using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IstasyonDemo.Api.Models
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        [JsonIgnore]
        public Role? Role { get; set; }

        [Required]
        [MaxLength(100)]
        public string ResourceKey { get; set; } = string.Empty; // VARDIYA_LISTESI, YONETIM_KULLANICI etc.
    }
}
