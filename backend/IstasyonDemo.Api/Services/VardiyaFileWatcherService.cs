using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IstasyonDemo.Api.Services
{
    public class VardiyaFileWatcherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VardiyaFileWatcherService> _logger;
        private readonly PeriodicTimer _timer;

        public VardiyaFileWatcherService(IServiceProvider serviceProvider, ILogger<VardiyaFileWatcherService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(30)); // Her 30 saniyede bir kontrol
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
             _logger.LogInformation("Vardiya Otomatik Dosya Takip Servisi Başlatıldı.");

            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessFilesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Dosya takip döngüsünde hata oluştu.");
                }
            }
        }

        private async Task ProcessFilesAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var vardiyaService = scope.ServiceProvider.GetRequiredService<IVardiyaService>();

                // Otomatik dosya yolu tanımlı aktif istasyonları getir
                var istasyonlar = await context.Istasyonlar
                    .Where(i => i.Aktif && !string.IsNullOrEmpty(i.OtomatikDosyaYolu))
                    .ToListAsync(cancellationToken);

                foreach (var istasyon in istasyonlar)
                {
                    await ProcessStationPathAsync(istasyon, vardiyaService, cancellationToken);
                }
            }
        }

        private async Task ProcessStationPathAsync(Istasyon istasyon, IVardiyaService vardiyaService, CancellationToken cancellationToken)
        {
            var path = istasyon.OtomatikDosyaYolu;
            if (!Directory.Exists(path))
            {
                // _logger.LogWarning($"İstasyon {istasyon.Ad} için tanımlı yol bulunamadı: {path}");
                return;
            }

            // Hem XML hem ZIP dosyalarını ara
            var files = Directory.GetFiles(path, "*.*")
                .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var filePath in files)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var fileName = Path.GetFileName(filePath);
                
                // Dosya kilitli mi kontrolü (yazma işlemi bitmiş mi?)
                if (IsFileLocked(filePath)) continue;

                _logger.LogInformation($"Yeni dosya algılandı: {fileName} (İstasyon: {istasyon.Ad})");

                try
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        // Servisi 'Sistem' kullanıcısı olarak çağırıyoruz (UserId: -1 veya 0, Rol: "system")
                        // NOT: Vardiya sorumlu ID'sini bulup o user adına yapmak daha şık olurdu ama şimdilik System.
                        int userId = istasyon.VardiyaSorumluId ?? istasyon.IstasyonSorumluId ?? 0;
                        string role = "system"; 
                        string userName = "OTOMATIK_YUKLEME";

                        await vardiyaService.ProcessXmlZipAsync(stream, fileName, userId, role, userName);
                    }

                    // Başarılı -> Processed klasörüne taşı
                    MoveFile(path, fileName, "Processed");
                    _logger.LogInformation($"Dosya başarıyla işlendi ve taşındı: {fileName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Dosya işlenirken hata: {fileName}");
                    // Hatalı -> Error klasörüne taşı
                    MoveFile(path, fileName, "Error");
                }
            }
        }

        private void MoveFile(string basePath, string fileName, string subFolder)
        {
            var targetDir = Path.Combine(basePath, subFolder);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            var sourceFile = Path.Combine(basePath, fileName);
            var destFile = Path.Combine(targetDir, fileName);

            // Eğer hedefte varsa üzerine yaz veya ismini değiştir? Şu an overwrite yapalım veya timestamp ekleyelim.
            if (File.Exists(destFile))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                destFile = Path.Combine(targetDir, $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}");
            }

            File.Move(sourceFile, destFile);
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // Dosya kullanımda
                return true;
            }
            return false;
        }
    }
}
