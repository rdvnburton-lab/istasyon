using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Istasyon.FileSync.Models;
using Serilog;

namespace Istasyon.FileSync.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly DatabaseService _dbService;
    private string _apiUrl = string.Empty;
    private string _apiKey = string.Empty;
    private int _istasyonId;

    public ApiService(DatabaseService dbService)
    {
        _httpClient = new HttpClient();
        _dbService = dbService;
    }

    public void Initialize(string apiUrl, string apiKey, int istasyonId)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _istasyonId = istasyonId;
    }

    public async Task<(bool success, string message)> TestConnectionAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return (false, "URL boş olamaz.");

        try
        {
            // Bekleyen dosyalar listesini çekerek bağlantıyı test et
            var testUrl = url.Replace("/upload", "/pending");
            var response = await _httpClient.GetAsync(testUrl);
            
            if (response.IsSuccessStatusCode)
                return (true, "Bağlantı başarılı!");
            
            return (false, $"Sunucu hata döndürdü: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}");
        }
    }

    public async Task<bool> UploadFileAsync(FileLog log)
    {
        if (string.IsNullOrEmpty(_apiUrl)) return false;

        try
        {
            using var content = new MultipartFormDataContent();
            var fileStream = File.OpenRead(log.FilePath);
            var fileContent = new StreamContent(fileStream);
            
            content.Add(fileContent, "file", log.FileName);
            content.Add(new StringContent(log.Hash), "originalHash");
            content.Add(new StringContent(_istasyonId.ToString()), "istasyonId");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                log.Status = "Sent";
                log.ErrorMessage = null;
                Log.Information("Dosya başarıyla gönderildi: {FileName}", log.FileName);
            }
            else
            {
                log.Status = "Failed";
                log.ErrorMessage = await response.Content.ReadAsStringAsync();
                Log.Warning("Dosya gönderimi başarısız: {FileName}, Hata: {Error}", log.FileName, log.ErrorMessage);
            }

            log.LastAttempt = DateTime.Now;
            _dbService.SaveLog(log);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
            log.LastAttempt = DateTime.Now;
            _dbService.SaveLog(log);
            Log.Error(ex, "API gönderim hatası: {FileName}", log.FileName);
            return false;
        }
    }
}
