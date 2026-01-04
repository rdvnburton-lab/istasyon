using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public enum DefinitionType
    {
        YAKIT = 1,
        BANKA = 2,
        GIDER = 3,
        GELIR = 4,
        ODEME = 5,
        GELIS_YONTEMI = 6,
        POMPA_GIDER = 7,
        PUSULA_TURU = 8
    }

    [Table("SystemDefinitions")]
    public class SystemDefinition
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DefinitionType Type { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        // Enum value mapping for backward compatibility (optional)
        [MaxLength(50)]
        public string? Code { get; set; }
    }
}
