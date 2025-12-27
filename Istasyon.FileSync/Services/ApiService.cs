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
    private string _clientUniqueId = string.Empty;

    public ApiService(DatabaseService dbService)
    {
        _httpClient = new HttpClient();
        _dbService = dbService;
    }

    public void Initialize(string apiUrl, string apiKey, int istasyonId, string clientUniqueId)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _istasyonId = istasyonId;
        _clientUniqueId = clientUniqueId;
    }

// ... (skip TestConnection) ...

    public async Task<(List<DiagnosticResult> results, StationInfo? info)> RunDiagnosticsAsync(string url, string apiKey, int istasyonId)
    {
        // ...
        // IN VerifyConfig Block:
                var request = new HttpRequestMessage(HttpMethod.Get, verifyUrl);
                request.Headers.Add("X-Api-Key", apiKey);
                if(!string.IsNullOrEmpty(_clientUniqueId)) request.Headers.Add("X-Client-Id", _clientUniqueId);

// ... (In UploadFile logic) ...
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            if(!string.IsNullOrEmpty(_clientUniqueId)) _httpClient.DefaultRequestHeaders.Add("X-Client-Id", _clientUniqueId);
// ...

    public async Task<(bool success, string message)> TestConnectionAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return (false, "URL boş olamaz.");

        try
        {
            // Bekleyen dosyalar listesini çekerek bağlantıyı test et
            var testUrl = url.Replace("/upload", "/pending");
            var response = await _httpClient.GetAsync(testUrl);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, "Bağlantı başarılı.");
            }
            
            return (false, $"Hata: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}");
        }
    }

    public async Task<(bool success, string message, string? role)> LoginAsync(string username, string password)
    {
        try
        {
            var loginData = new { Username = username, Password = password };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(loginData), System.Text.Encoding.UTF8, "application/json");

            // Güvenli URL oluşturma
            if (Uri.TryCreate(_apiUrl, UriKind.Absolute, out var uri))
            {
                // Force base URL (http://localhost:5000) + /api/Auth/login
                // This prevents issues if _apiUrl contains /api/FileTransfer/test
                var baseUrl = uri.GetLeftPart(UriPartial.Authority);
                var authUrl = $"{baseUrl}/api/Auth/login";
                
                var response = await _httpClient.PostAsync(authUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    // Case-insensitive deserialization
                    using var doc = System.Text.Json.JsonDocument.Parse(responseString);
                    
                    string? role = null;
                    if (doc.RootElement.TryGetProperty("role", out var roleProp) || doc.RootElement.TryGetProperty("Role", out roleProp))
                    {
                        role = roleProp.GetString();
                    }
                    else
                    {
                        // JSON içinde 'role' veya 'Role' bulunamadı
                        return (true, "Giriş başarılı ancak rol bilgisi alınamadı.", null);
                    }
                    
                    return (true, "Giriş başarılı", role);
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                return (false, $"Giriş başarısız: {errorMsg}", null);
            }
            
            return (false, "Geçersiz URL formatı.", null);
        }
        catch (Exception ex)
        {
            return (false, $"Giriş hatası: {ex.Message}", null);
        }
    }

    public async Task<(List<DiagnosticResult> results, StationInfo? info)> RunDiagnosticsAsync(string url, string apiKey, int istasyonId)
    {
        var results = new List<DiagnosticResult>();
        StationInfo? stationInfo = null;

        // 1. İnternet Bağlantısı
        // 1. İnternet Bağlantısı
        var internetCheck = new DiagnosticResult { CheckName = "İnternet Bağlantısı" };
        
        if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            try 
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 2000);
                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    internetCheck.IsSuccess = true;
                    internetCheck.Message = $"Bağlantı var ({reply.RoundtripTime}ms).";
                }
                else
                {
                    internetCheck.IsSuccess = false;
                    internetCheck.Message = "İnternet yok (Ping başarısız).";
                }
            }
            catch
            {
                internetCheck.IsSuccess = false;
                internetCheck.Message = "İnternet yok (Ping hatası).";
            }
        }
        else
        {
            internetCheck.IsSuccess = false;
            internetCheck.Message = "Ağ bağlantısı yok (Kablo/Wi-Fi kapalı).";
        }
        results.Add(internetCheck);
        if (!internetCheck.IsSuccess) return (results, null); 

        // 2. API Erişilebilirliği
        var apiCheck = new DiagnosticResult { CheckName = "API Sunucusu" };
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var baseAuth = uri.GetLeftPart(UriPartial.Authority);
                // We added [HttpGet("test")] to FileTransferController
                // URL: {base}/api/FileTransfer/test
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var testUrl = $"{baseAuth}/api/FileTransfer/test";
                
                var response = await _httpClient.GetAsync(testUrl, cts.Token); 
                
                if (response.IsSuccessStatusCode)
                {
                     apiCheck.IsSuccess = true;
                     apiCheck.Message = "Sunucuya erişilebiliyor.";
                }
                else
                {
                     apiCheck.IsSuccess = false;
                     // If 404, maybe using old backend? Try root verification?
                     // But for now, just report status
                     apiCheck.Message = $"Sunucu hatası: {response.StatusCode}";
                }
            }
            else
            {
                apiCheck.IsSuccess = false;
                apiCheck.Message = "Geçersiz URL.";
            }
        }
        catch (Exception ex)
        {
            apiCheck.IsSuccess = false;
            apiCheck.Message = $"Erişim yok: {ex.Message}";
        }
        results.Add(apiCheck);
        if (!apiCheck.IsSuccess) return (results, null);

        // 3. Konfigürasyon Doğrulama (İstasyon & Key)
        var configCheck = new DiagnosticResult { CheckName = "İstasyon & Yetki" };
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var baseAuth = uri.GetLeftPart(UriPartial.Authority);
                var verifyUrl = $"{baseAuth}/api/FileTransfer/verify?istasyonId={istasyonId}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, verifyUrl);
                request.Headers.Add("X-Api-Key", apiKey);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    configCheck.IsSuccess = true;
                    configCheck.Message = "İstasyon aktif ve yetkili.";
                    
                    try 
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(content);
                        stationInfo = new StationInfo();
                        if(doc.RootElement.TryGetProperty("istasyonAdi", out var name)) stationInfo.IstasyonAdi = name.GetString() ?? "";
                        if(doc.RootElement.TryGetProperty("istasyonAdresi", out var adres)) stationInfo.IstasyonAdresi = adres.GetString() ?? "";
                        if(doc.RootElement.TryGetProperty("firmaAdi", out var firma)) stationInfo.FirmaAdi = firma.GetString() ?? "";
                        if(doc.RootElement.TryGetProperty("istasyonSorumlusu", out var sorumlu)) stationInfo.IstasyonSorumlusu = sorumlu.GetString() ?? "";
                        if(doc.RootElement.TryGetProperty("vardiyaSorumlusu", out var vardiya)) stationInfo.VardiyaSorumlusu = vardiya.GetString() ?? "";
                        if(doc.RootElement.TryGetProperty("companyBoss", out var boss)) stationInfo.PatronAdi = boss.GetString() ?? "";
                    }
                    catch { /* Parse error, ignore details */ }
                }
                else
                {
                    configCheck.IsSuccess = false;
                    try 
                    {
                         using var doc = System.Text.Json.JsonDocument.Parse(content);
                         if(doc.RootElement.TryGetProperty("detail", out var detail)) 
                         {
                             configCheck.Message = detail.GetString() ?? content;
                         }
                         else 
                         {
                             configCheck.Message = content;
                         }
                    }
                    catch
                    {
                        configCheck.Message = content;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            configCheck.IsSuccess = false;
            configCheck.Message = $"Doğrulama hatası: {ex.Message}";
        }
        results.Add(configCheck);

        return (results, stationInfo);
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
