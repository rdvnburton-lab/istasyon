using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Istasyon.FileSync.Models;
using Serilog;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;

namespace Istasyon.FileSync.Services;

public class FileWatcherService
{
    private FileSystemWatcher? _watcher;
    private readonly DatabaseService _dbService;
    private readonly ApiService _apiService;
    private string _watchPath = string.Empty;
    private System.Timers.Timer? _heartbeatTimer;

    public FileWatcherService(DatabaseService dbService, ApiService apiService)
    {
        _dbService = dbService;
        _apiService = apiService;
    }

    public void UpdateApiConfig(string url, string apiKey, int istasyonId, string clientUniqueId, string stationCode = "")
    {
        _apiService.Initialize(url, apiKey, istasyonId, clientUniqueId, stationCode);
    }

    public async Task<(bool success, string message)> TestConnectionAsync(string url)
    {
        return await _apiService.TestConnectionAsync(url);
    }

    public async Task<(bool success, string message, string? role, System.Collections.Generic.List<ApiService.StationLoginDto>? stations)> LoginAsync(string username, string password)
    {
        return await _apiService.LoginAsync(username, password);
    }

    public async Task<(List<DiagnosticResult> results, StationInfo? info)> RunDiagnosticsAsync(string url, string apiKey, int istasyonId)
    {
        return await _apiService.RunDiagnosticsAsync(url, apiKey, istasyonId);
    }

    public async Task<List<FirmaDto>> GetFirmasAsync() => await _apiService.GetFirmasAsync();
    public async Task<List<IstasyonDto>> GetStationsAsync(int? firmaId = null) => await _apiService.GetStationsAsync(firmaId);

    public void Start(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            Log.Warning("İzlenecek klasör geçersiz: {Path}", path);
            return;
        }

        _watchPath = path;
        _watcher = new FileSystemWatcher(_watchPath);
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        _watcher.Filter = "*.*"; // Tüm dosyaları izle, ProcessFile içinde filtrele
        _watcher.Created += OnCreated;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.EnableRaisingEvents = true;

        Log.Information("Klasör izleme başlatıldı: {Path}", _watchPath);
        
        // Heartbeat Timer Başlat (3 dakikada bir)
        _heartbeatTimer = new System.Timers.Timer(180000); // 3 dakika
        _heartbeatTimer.Elapsed += async (s, e) => await _apiService.SendHeartbeatAsync();
        _heartbeatTimer.Start();

        // Başlangıçta bir kez heartbeat gönder
        Task.Run(async () => await _apiService.SendHeartbeatAsync());
        
        // Başlangıçta mevcut dosyaları tara
        Task.Run(() => ScanExistingFiles());
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
        
        if (_heartbeatTimer != null)
        {
            _heartbeatTimer.Stop();
            _heartbeatTimer.Dispose();
        }
    }

    private void OnCreated(object sender, FileSystemEventArgs e) => ProcessFile(e.FullPath);
    private void OnChanged(object sender, FileSystemEventArgs e) => ProcessFile(e.FullPath);
    private void OnRenamed(object sender, RenamedEventArgs e) => ProcessFile(e.FullPath);

    private async void ProcessFile(string filePath)
    {
        try
        {
            // Dosyanın yazılmasının bitmesini bekle (basit bir gecikme veya kilit kontrolü)
            await WaitForFile(filePath);

            if (!File.Exists(filePath)) return;

            // FİLTRELEME: Uzantı kontrolü (.D1*, .ZIP, .XML)
            string extension = Path.GetExtension(filePath);
            bool isValidExtension = extension.StartsWith(".D1", StringComparison.OrdinalIgnoreCase) ||
                                  extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                                  extension.Equals(".xml", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(extension) || !isValidExtension)
            {
                Log.Debug("Dosya uzantısı filtreye uymuyor, atlanıyor: {FilePath}", filePath);
                return;
            }

            // ZIP DOĞRULAMA (İstasyon Kodu Kontrolü)
            if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using (var archive = ZipFile.OpenRead(filePath))
                    {
                        var xmlEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                        if (xmlEntry != null)
                        {
                            using (var stream = xmlEntry.Open())
                            using (var reader = new StreamReader(stream)) // Encoding tespiti gerekebilir, default UTF-8 dener
                            {
                                var content = await reader.ReadToEndAsync();
                                // Basit XML Parsing
                                // XDocument kullanarak GlobalParams -> StationCode bulmaya çalış
                                // Hata toleransı için try-catch bloğu içinde
                                try 
                                {
                                    var xdoc = XDocument.Parse(content);
                                    var globalParams = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "GlobalParams");
                                    var fileStationCode = globalParams?.Elements().FirstOrDefault(x => x.Name.LocalName == "StationCode")?.Value;

                                    var expectedCode = _apiService.CurrentStationCode;

                                    if (!string.IsNullOrEmpty(expectedCode) && !string.IsNullOrEmpty(fileStationCode))
                                    {
                                        if (fileStationCode != expectedCode)
                                        {
                                            Log.Warning("BLOKE EDİLDİ: İstasyon Kodu Uyuşmazlığı! Dosya: {FileName}, Dosyadaki Kod: {FileCode}, Beklenen: {Expected}", Path.GetFileName(filePath), fileStationCode, expectedCode);
                                            
                                            // LOG REJECTION
                                            var rejectedLog = new FileLog
                                            {
                                                FileName = Path.GetFileName(filePath),
                                                FilePath = filePath,
                                                Hash = "REJECTED", 
                                                Status = "Reddedildi",
                                                ErrorMessage = $"İstasyon Kodu Uyuşmazlığı (Dosya: {fileStationCode}, Beklenen: {expectedCode})",
                                                LastAttempt = DateTime.Now
                                            };
                                            _dbService.SaveLog(rejectedLog);

                                            return; 
                                        }
                                        else
                                        {
                                            Log.Information("Dosya doğrulandı: İstasyon Kodu {Code} eşleşiyor.", fileStationCode);
                                        }
                                    }
                                }
                                catch
                                {
                                    // XML parse hatası, kritik değil, devam et
                                }
                            }
                        }
                    }
                }
                catch (Exception exZip)
                {
                    Log.Warning("Zip doğrulama sırasında hata: {Msg}", exZip.Message);
                    // Doğrulama yapılamadıysa da gönderime devam et (veya engelle?)
                    // Güvenli taraf: Devam et (User verification failed shouldn't block totally if corrupted/partial)
                    // AMA user mismatch critical.
                }
            }

            // FİLTRELEME: Boş dosya kontrolü
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                Log.Warning("Dosya içeriği boş, atlanıyor: {FilePath}", filePath);
                 var emptyLog = new FileLog
                {
                    FileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    Hash = "EMPTY",
                    Status = "Reddedildi",
                    ErrorMessage = "Dosya içeriği boş.",
                    LastAttempt = DateTime.Now
                };
                _dbService.SaveLog(emptyLog);
                return;
            }

            string hash = await CalculateHash(filePath);
            var existing = _dbService.GetLogByPath(filePath);

            if (existing != null && existing.Hash == hash && existing.Status == "Sent")
            {
                // Dosya zaten başarıyla gönderilmiş ve değişmemiş
                return;
            }

            var log = new FileLog
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                Hash = hash,
                Status = "Pending",
                LastAttempt = DateTime.Now
            };

            _dbService.SaveLog(log);
            
            // Gönderimi başlat
            await _apiService.UploadFileAsync(log);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dosya işleme hatası: {FilePath}", filePath);
        }
    }

    private void ScanExistingFiles()
    {
        try
        {
            var files = Directory.GetFiles(_watchPath, "*.*");
            foreach (var file in files)
            {
                ProcessFile(file);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dosya tarama hatası.");
        }
    }

    private async Task<string> CalculateHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private async Task WaitForFile(string filePath)
    {
        int retryCount = 0;
        while (retryCount < 10)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return;
            }
            catch (IOException)
            {
                retryCount++;
                await Task.Delay(500);
            }
        }
    }
}
