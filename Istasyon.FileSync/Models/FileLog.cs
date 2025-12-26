using System;

namespace Istasyon.FileSync.Models;

public class FileLog
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed
    public DateTime LastAttempt { get; set; }
    public string? ErrorMessage { get; set; }
}
