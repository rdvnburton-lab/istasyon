using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileTransferController : ControllerBase
{
    private readonly ILogger<FileTransferController> _logger;
    private readonly AppDbContext _context;

    public FileTransferController(ILogger<FileTransferController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyConfig([FromQuery] int istasyonId, [FromHeader(Name = "X-Api-Key")] string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return Unauthorized("API Key eksik.");
        }

        var istasyon = await _context.Istasyonlar
            .Include(i => i.IstasyonSorumlu)
            .Include(i => i.VardiyaSorumlu)
            .Include(i => i.Firma).ThenInclude(f => f.Patron)
            .FirstOrDefaultAsync(i => i.Id == istasyonId);

        if (istasyon == null)
        {
            return NotFound("Bu ID ile kayıtlı bir istasyon bulunamadı.");
        }

        if (istasyon.ApiKey != apiKey)
        {
            return Unauthorized("API Key hatalı.");
        }

        if (!istasyon.Aktif)
        {
            return BadRequest("İstasyon pasif durumda.");
        }

        // DEVICE LOCK CHECK
        // Get ClientId from Header
        var clientId = Request.Headers["X-Client-Id"].ToString();
        if (!string.IsNullOrEmpty(clientId))
        {
            if (string.IsNullOrEmpty(istasyon.RegisteredDeviceId))
            {
                // İlk bağlantı: Cihazı kaydet (Binding)
                istasyon.RegisteredDeviceId = clientId;
                await _context.SaveChangesAsync();
                _logger.LogInformation("İstasyon {Id} için cihaz kilidi oluşturuldu: {ClientId}", istasyon.Id, clientId);
            }
            else if (istasyon.RegisteredDeviceId != clientId)
            {
                // Kilitli ve farklı cihaz
                _logger.LogWarning("Yetkisiz cihaz erişimi! İstasyon {Id}. Kayıtlı: {Registered}, Gelen: {Incoming}", istasyon.Id, istasyon.RegisteredDeviceId, clientId);
                return StatusCode(403, "Bu istasyon başka bir bilgisayara kilitlenmiştir. Lütfen yönetici ile iletişime geçin.");
            }
        }

        // Update Last Connection Time
        istasyon.LastConnectionTime = DateTime.UtcNow;
        await _context.SaveChangesAsync(); // DeviceId binding already saves, but good to ensure update here if binding didn't happen

        return Ok(new 
        { 
            message = "Konfigürasyon geçerli.", 
            istasyonAdi = istasyon.Ad,
            istasyonAdresi = istasyon.Adres ?? "Adres girilmemiş",
            firmaAdi = istasyon.Firma?.Ad ?? "Firma Belirtilmemiş",
            istasyonSorumlusu = istasyon.IstasyonSorumlu?.AdSoyad ?? istasyon.IstasyonSorumlu?.Username ?? "Atanmamış",
            vardiyaSorumlusu = istasyon.VardiyaSorumlu?.AdSoyad ?? istasyon.VardiyaSorumlu?.Username ?? "Atanmamış",
            companyBoss = istasyon.Firma?.Patron?.AdSoyad ?? istasyon.Firma?.Patron?.Username ?? "Atanmamış" // Patron
        });
    }

    [HttpGet("test")]
    public IActionResult TestConnection()
    {
        return Ok(new { message = "Bağlantı başarılı." });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string originalHash, [FromForm] int? istasyonId, [FromHeader(Name = "X-Api-Key")] string apiKey)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Dosya seçilmedi.");
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            return Unauthorized("API Key eksik.");
        }

        try
        {
            // API Key ve İstasyon Doğrulama
            var istasyon = await _context.Istasyonlar.FirstOrDefaultAsync(i => i.Id == istasyonId && i.ApiKey == apiKey && i.Aktif);
            if (istasyon == null)
            {
                _logger.LogWarning("Yetkisiz erişim denemesi. IstasyonId: {IstasyonId}, ApiKey: {ApiKey}", istasyonId, apiKey);
                return Unauthorized("Geçersiz API Key veya İstasyon ID.");
            }

            // Dosya içeriğini byte array olarak oku
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            // Bütünlük kontrolü (Hash doğrulama)
            string calculatedHash = CalculateHash(fileBytes);

            if (calculatedHash != originalHash)
            {
                _logger.LogWarning("Dosya bütünlük hatası: {FileName}. Beklenen: {Original}, Hesaplanan: {Calculated}", file.FileName, originalHash, calculatedHash);
                return BadRequest("Dosya bütünlük kontrolü başarısız oldu. Hash uyuşmuyor.");
            }

            // DUPLICATE CHECK: Eğer aynı hash ve istasyon ID ile dosya zaten varsa kaydetme, başarılı dön.
            var existingFile = await _context.OtomatikDosyalar
                .FirstOrDefaultAsync(f => f.IstasyonId == istasyonId && f.Hash == calculatedHash);

            if (existingFile != null)
            {
                _logger.LogInformation("Mükerrer dosya gönderimi fark edildi ve atlandı: {FileName} (Hash: {Hash})", file.FileName, calculatedHash);
                return Ok(new { message = "Dosya zaten sunucuda mevcut. İşlem başarılı sayıldı.", id = existingFile.Id });
            }

            // Veritabanına kaydet
            istasyon.LastConnectionTime = DateTime.UtcNow; // Update health logic
            
            var otomatikDosya = new OtomatikDosya
            {
                DosyaAdi = file.FileName,
                DosyaIcerigi = fileBytes,
                Hash = calculatedHash,
                YuklemeTarihi = DateTime.UtcNow,
                Islendi = false,
                IstasyonId = istasyonId
            };

            _context.OtomatikDosyalar.Add(otomatikDosya);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dosya başarıyla veritabanına kaydedildi: {FileName} (İstasyon: {IstasyonAd})", file.FileName, istasyon.Ad);
            return Ok(new { message = "Dosya başarıyla alındı ve veritabanına kaydedildi.", id = otomatikDosya.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya yükleme sırasında hata oluştu.");
            return StatusCode(500, $"Sunucu hatası: {ex.Message}");
        }
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingFiles()
    {
        var files = await _context.OtomatikDosyalar
            .Include(f => f.Istasyon)
            .Where(f => !f.Islendi)
            .Select(f => new { 
                f.Id, 
                f.DosyaAdi, 
                f.YuklemeTarihi, 
                f.Hash,
                IstasyonAd = f.Istasyon != null ? f.Istasyon.Ad : "Bilinmiyor"
            })
            .OrderByDescending(f => f.YuklemeTarihi)
            .ToListAsync();
            
        return Ok(files);
    }

    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetFileContent(int id)
    {
        var file = await _context.OtomatikDosyalar.FindAsync(id);
        if (file == null) return NotFound();
        
        return Ok(new { 
            id = file.Id, 
            dosyaAdi = file.DosyaAdi, 
            icerik = Convert.ToBase64String(file.DosyaIcerigi) 
        });
    }

    [HttpPost("{id}/processed")]
    public async Task<IActionResult> MarkAsProcessed(int id)
    {
        var file = await _context.OtomatikDosyalar.FindAsync(id);
        if (file == null) return NotFound();
        
        file.Islendi = true;
        file.IslenmeTarihi = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return Ok();
    }

    private string CalculateHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
