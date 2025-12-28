using System;
using System.IO;
using Newtonsoft.Json;
using Istasyon.FileSync.Models;

namespace Istasyon.FileSync.Services;

public class ConfigService
{
    private readonly string _configPath;
    private AppConfig _config;

    public ConfigService()
    {
        // "Single File" uygulamasında AppContext.BaseDirectory geçici (Temp) klasörü gösterir.
        // Bu yüzden Process.MainModule.FileName kullanarak .exe'nin olduğu gerçek klasörü buluyoruz.
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        var directory = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        
        _configPath = Path.Combine(directory, "appsettings.json");
        _config = LoadConfig();
    }

    public AppConfig Config => _config;

    public AppConfig LoadConfig()
    {
        if (File.Exists(_configPath))
        {
            var content = File.ReadAllText(_configPath);
            string json;
            
            // Try to decrypt
            try
            {
                var protectedBytes = Convert.FromBase64String(content);
                var bytes = System.Security.Cryptography.ProtectedData.Unprotect(
                    protectedBytes, 
                    null, 
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                json = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // Fallback: Read as plain text (migration scenario)
                json = content;
                // We will re-save as encrypted next time SaveConfig is called
            }

            var config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            
            // Ensure ClientUniqueId exists
            if (string.IsNullOrEmpty(config.ClientUniqueId))
            {
                config.ClientUniqueId = Guid.NewGuid().ToString();
                SaveConfig(config); // Save immediately (will encrypt now)
            }
            return config;
        }
        
        var newConfig = new AppConfig();
        newConfig.ClientUniqueId = Guid.NewGuid().ToString();
        // SaveConfig(newConfig); // Don't save yet, let the user configure it first? 
        // Or save it encrypted empty. 
        // Let's NOT save it yet, so MainWindow knows it's fresh?
        // Actually ConfigService constructor calls this. 
        // Let's save it encrypted.
        SaveConfig(newConfig);
        return newConfig;
    }

    public void SaveConfig(AppConfig config)
    {
        _config = config;
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        
        var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(
            bytes, 
            null, 
            System.Security.Cryptography.DataProtectionScope.CurrentUser);
            
        File.WriteAllText(_configPath, Convert.ToBase64String(protectedBytes));
    }
}
