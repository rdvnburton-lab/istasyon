namespace Istasyon.FileSync.Models;

public class AppConfig
{
    public string WatchFolderPath { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "http://localhost:5133/api/FileTransfer/upload";
    public string ApiKey { get; set; } = string.Empty;
    public int IstasyonId { get; set; }
    public string ClientUniqueId { get; set; } = string.Empty;
    public bool AutoStart { get; set; } = true;
}
