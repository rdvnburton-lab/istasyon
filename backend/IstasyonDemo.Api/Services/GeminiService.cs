using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IstasyonDemo.Api.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<GeminiOcrResponse?> AnalyzeReceiptAsync(string base64Image)
        {
            var apiKey = _configuration["GeminiSettings:ApiKey"];
            var modelId = _configuration["GeminiSettings:ModelId"];

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Gemini API Key is missing.");
            
            // Default to gemini-1.5-flash if not set
            if (string.IsNullOrEmpty(modelId))
                modelId = "gemini-1.5-flash";

            // Clean base64 string if it contains header (e.g., "data:image/jpeg;base64,")
            if (base64Image.Contains(","))
            {
                base64Image = base64Image.Substring(base64Image.IndexOf(",") + 1);
            }

            // Normalize Bank Names List
            var validBanks = "['Ziraat Bankası', 'Garanti BBVA', 'İş Bankası', 'Yapı Kredi', 'Akbank', 'Halkbank', 'Vakıfbank', 'QNB Finansbank', 'Denizbank']";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = $"Extract payment details from this receipt image. Return ONLY a JSON object with this structure: {{ \"nakit\": number, \"krediKarti\": number, \"krediKartiDetay\": [ {{ \"banka\": \"bank name\", \"tutar\": number }} ], \"digerOdemeler\": [ {{ \"turKodu\": \"CODE\", \"tutar\": number }} ] }}. IMPORTANT: For 'digerOdemeler', use codes like 'PARO_PUAN' or 'MOBIL_ODEME'. For 'banka' field, map any detected bank name to exactly one of: {validBanks}. Do not include markdown formatting." },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 500,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent( JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsync(url, jsonContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {response.StatusCode} - {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseString);

            var textContent = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(textContent))
                return null;

            // Cleanup potential markdown code blocks if the model ignores the prompt instruction
            textContent = textContent.Replace("```json", "").Replace("```", "").Trim();

            try 
            {
                return JsonSerializer.Deserialize<GeminiOcrResponse>(textContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                // Log the text content to see what went wrong with parsing
                throw new Exception($"JSON Parse Error. Content: {textContent}. Error: {ex.Message}");
            }
        }
        public async Task<string?> AnalyzeDashboardAsync(object dashboardData)
        {
            var apiKey = _configuration["GeminiSettings:ApiKey"];
            var modelId = _configuration["GeminiSettings:ModelId"];

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Gemini API Key is missing.");
            
            if (string.IsNullOrEmpty(modelId))
                modelId = "gemini-1.5-flash";

            var dataJson = JsonSerializer.Serialize(dashboardData);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = $"Sen bir petrol istasyonu stratejik iş analiz uzmanısın. Aşağıdaki dashboard verilerini (günlük ciro, trendler, personel durumu, onay bekleyenler) profesyonel bir bakış açısıyla analiz et ve istasyon sahibine kurumsal, ciddi ve yol gösterici bir özet sun. \n\nVERİLER: {dataJson}\n\nYanıtını şu JSON yapısında döndür: {{ \"mood\": \"emoji ve kurumsal durum başlığı\", \"tespit\": \"en önemli finansal veya operasyonel analiz bulgusu\", \"tavsiye\": \"stratejik bir aksiyon önerisi\" }}. Kesinlikle markdown kullanma, sadece saf JSON döndür. Dil ciddi ve kurumsal olsun." },
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 400,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsync(url, jsonContent);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var responseString = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseString);
            return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        }
    }

    // API Response Models
    public class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    // Our Business Domain Model
    public class GeminiOcrResponse
    {
        [JsonPropertyName("nakit")]
        public decimal Nakit { get; set; }

        [JsonPropertyName("krediKarti")]
        public decimal KrediKarti { get; set; }

        [JsonPropertyName("krediKartiDetay")]
        public List<GeminiKrediKartiDetay> KrediKartiDetay { get; set; } = new();

        [JsonPropertyName("digerOdemeler")]
        public List<GeminiDigerOdeme> DigerOdemeler { get; set; } = new();
        
        // Legacy fields for compatibility during transition
        [JsonPropertyName("paroPuan")]
        public decimal ParoPuan { get; set; }

        [JsonPropertyName("mobilOdeme")]
        public decimal MobilOdeme { get; set; }
    }

    public class GeminiKrediKartiDetay
    {
        [JsonPropertyName("banka")]
        public string Banka { get; set; } = string.Empty;

        [JsonPropertyName("tutar")]
        public decimal Tutar { get; set; }
    }

    public class GeminiDigerOdeme
    {
        [JsonPropertyName("turKodu")]
        public string TurKodu { get; set; } = string.Empty;

        [JsonPropertyName("tutar")]
        public decimal Tutar { get; set; }
    }
}
