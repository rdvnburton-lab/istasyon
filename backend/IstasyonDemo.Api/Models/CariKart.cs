using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IstasyonDemo.Api.Models
{
    public class CariKart
    {
        [Key]
        public int Id { get; set; }

        public int IstasyonId { get; set; }
        [ForeignKey("IstasyonId")]
        public virtual Istasyon? Istasyon { get; set; }

        [Required]
        [MaxLength(200)]
        public string Ad { get; set; }

        [MaxLength(50)]
        public string? VergiDairesi { get; set; }

        [MaxLength(20)]
        [Column("TCKN_VKN")]
        public string? TckN_VKN { get; set; }

        [MaxLength(20)]
        public string? Telefon { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Adres { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Bakiye { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Limit { get; set; } = 0;

        public bool Aktif { get; set; } = true;

        [MaxLength(50)]
        public string? Kod { get; set; }

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
        public DateTime? GuncellemeTarihi { get; set; }
    }
}
