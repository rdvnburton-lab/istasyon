using System.Windows.Media;

namespace Istasyon.FileSync.Models;

public class DiagnosticResult
{
    public string CheckName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public string Icon => IsSuccess ? "✅" : "❌";
    public SolidColorBrush Color => IsSuccess ? Brushes.Green : Brushes.Red;
}
