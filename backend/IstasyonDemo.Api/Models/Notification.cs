using System.ComponentModel.DataAnnotations;

namespace IstasyonDemo.Api.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User? User { get; set; }
        
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // VARDIYA_ONAYLANDI, VARDIYA_REDDEDILDI, MARKET_ONAYLANDI, etc.
        
        [MaxLength(20)]
        public string Severity { get; set; } = "info"; // info, success, warning, danger
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int? RelatedVardiyaId { get; set; }
        public Vardiya? RelatedVardiya { get; set; }
        
        public int? RelatedMarketVardiyaId { get; set; }
        public MarketVardiya? RelatedMarketVardiya { get; set; }
        
        public int? RelatedLogId { get; set; } // VardiyaLog'dan oluşturulduysa
    }
}
