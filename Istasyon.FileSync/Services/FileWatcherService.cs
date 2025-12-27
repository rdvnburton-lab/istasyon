using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Istasyon.FileSync.Models;
using Serilog;

namespace Istasyon.FileSync.Services;

public class FileWatcherService
{
    private FileSystemWatcher? _watcher;
    private readonly DatabaseService _dbService;
    private readonly ApiService _apiService;
    private string _watchPath = string.Empty;

    public FileWatcherService(DatabaseService dbService, ApiService apiService)
    {
        _dbService = dbService;
        _apiService = apiService;
    }

    public void UpdateApiConfig(string url, string apiKey, int istasyonId)
    {
        _apiService.Initialize(url, apiKey, istasyonId);
    }

    public async Task<(bool success, string message)> TestConnectionAsync(string url)
    {
        return await _apiService.TestConnectionAsync(url);
    }

    public async Task<(bool success, string message, string? role)> LoginAsync(string username, string password)
    {
        return await _apiService.LoginAsync(username, password);
    }

    public async Task<List<DiagnosticResult>> RunDiagnosticsAsync(string url, string apiKey, int istasyonId)
    {
        return await _apiService.RunDiagnosticsAsync(url, apiKey, istasyonId);
    }

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
        _watcher.Created += OnCreated;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.EnableRaisingEvents = true;

        Log.Information("Klasör izleme başlatıldı: {Path}", _watchPath);
        
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
        var files = Directory.GetFiles(_watchPath);
        foreach (var file in files)
        {
            ProcessFile(file);
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
