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
    private string? _authToken;

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



    private string GetBaseUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
             return uri.GetLeftPart(UriPartial.Authority);
        }
        return url.TrimEnd('/');
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

    public class StationLoginDto
    {
        public int Id { get; set; }
        public string Ad { get; set; }
        public string ApiKey { get; set; }
    }

    public async Task<(bool success, string message, string? role, System.Collections.Generic.List<StationLoginDto>? stations)> LoginAsync(string username, string password)
    {
        try
        {
            var loginData = new { Username = username, Password = password };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(loginData), System.Text.Encoding.UTF8, "application/json");

            var baseUrl = GetBaseUrl(_apiUrl);
            if (string.IsNullOrEmpty(baseUrl)) return (false, "Geçersiz URL formatı.", null, null);

            var authUrl = $"{baseUrl}/api/Auth/login";
            
            var response = await _httpClient.PostAsync(authUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(responseString);
                
                string? role = null;
                if (doc.RootElement.TryGetProperty("role", out var roleProp) || doc.RootElement.TryGetProperty("Role", out roleProp))
                {
                    role = roleProp.GetString();
                }
                
                var stations = new System.Collections.Generic.List<StationLoginDto>();
                if (doc.RootElement.TryGetProperty("istasyonlar", out var stationsProp))
                {
                   foreach(var s in stationsProp.EnumerateArray())
                   {
                       stations.Add(new StationLoginDto {
                           Id = s.GetProperty("id").GetInt32(),
                           Ad = s.GetProperty("ad").GetString() ?? "",
                           ApiKey = s.TryGetProperty("apiKey", out var k) ? k.GetString() : ""
                       });
                   }
                }

                if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                {
                    _authToken = tokenProp.GetString();
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
                }

                return (true, "Giriş başarılı", role, stations);
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            return (false, $"Giriş başarısız: {errorMsg}", null, null);
        }
        catch (Exception ex)
        {
            return (false, $"Giriş hatası: {ex.Message}", null, null);
        }
    }

    public async Task<List<FirmaDto>> GetFirmasAsync()
    {
        if (string.IsNullOrEmpty(_authToken)) return new List<FirmaDto>();
        
        try
        {
            var baseUrl = GetBaseUrl(_apiUrl);
            var response = await _httpClient.GetAsync($"{baseUrl}/api/Firma");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<List<FirmaDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FirmaDto>();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Firmalar alınamadı.");
        }
        return new List<FirmaDto>();
    }

    public async Task<List<IstasyonDto>> GetStationsAsync(int? firmaId = null)
    {
        if (string.IsNullOrEmpty(_authToken)) return new List<IstasyonDto>();
        
        try
        {
            var baseUrl = GetBaseUrl(_apiUrl);
            var url = $"{baseUrl}/api/Istasyon";
            if (firmaId.HasValue) url += $"?firmaId={firmaId}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<List<IstasyonDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<IstasyonDto>();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "İstasyonlar alınamadı.");
        }
        return new List<IstasyonDto>();
    }

    public async Task<(List<DiagnosticResult> results, StationInfo? info)> RunDiagnosticsAsync(string url, string apiKey, int istasyonId)
    {
        var results = new List<DiagnosticResult>();
        StationInfo? stationInfo = null;

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
            var baseUrl = GetBaseUrl(url);
            if (!string.IsNullOrEmpty(baseUrl))
            {
                // URL: {base}/api/FileTransfer/test
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var testUrl = $"{baseUrl}/api/FileTransfer/test";
                
                var response = await _httpClient.GetAsync(testUrl, cts.Token); 
                
                if (response.IsSuccessStatusCode)
                {
                     apiCheck.IsSuccess = true;
                     apiCheck.Message = "Sunucuya erişilebiliyor.";
                }
                else
                {
                     apiCheck.IsSuccess = false;
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
            var baseUrl = GetBaseUrl(url);
            if (!string.IsNullOrEmpty(baseUrl))
            {
                var verifyUrl = $"{baseUrl}/api/FileTransfer/verify?istasyonId={istasyonId}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, verifyUrl);
                request.Headers.Add("X-Api-Key", apiKey);
                if (!string.IsNullOrEmpty(_clientUniqueId)) request.Headers.Add("X-Client-Id", _clientUniqueId);

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
            var baseUrl = GetBaseUrl(_apiUrl);
            var uploadUrl = $"{baseUrl}/api/FileTransfer/upload";

            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(log.FilePath);
            using var fileContent = new StreamContent(fileStream);
            
            content.Add(fileContent, "file", log.FileName);
            content.Add(new StringContent(log.Hash), "originalHash");
            content.Add(new StringContent(_istasyonId.ToString()), "istasyonId");

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Add("X-Api-Key", _apiKey);
            if (!string.IsNullOrEmpty(_clientUniqueId))
            {
                request.Headers.Add("X-Client-Id", _clientUniqueId);
            }
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

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
                Log.Warning("Dosya gönderimi başarısız: {FileName}, Hata: {Status} - {Error}", log.FileName, response.StatusCode, log.ErrorMessage);
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
    public async Task SendHeartbeatAsync()
    {
        if (string.IsNullOrEmpty(_apiUrl)) return;

        try
        {
            var baseUrl = GetBaseUrl(_apiUrl);
            if (!string.IsNullOrEmpty(baseUrl))
            {
                var verifyUrl = $"{baseUrl}/api/FileTransfer/verify?istasyonId={_istasyonId}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, verifyUrl);
                request.Headers.Add("X-Api-Key", _apiKey);
                if (!string.IsNullOrEmpty(_clientUniqueId)) request.Headers.Add("X-Client-Id", _clientUniqueId);

                await _httpClient.SendAsync(request);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Heartbeat gönderilemedi.");
        }
    }
}
