namespace IstasyonDemo.Api.Dtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public bool Read { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // info, success, warning, danger
        public int? RelatedId { get; set; } // VardiyaId veya MarketVardiyaId
        public string? RelatedType { get; set; } // "vardiya" veya "market"
    }

    public class NotificationSummaryDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Notifications { get; set; } = new();
    }

    public class MarkNotificationReadDto
    {
        public int NotificationId { get; set; }
    }
}
