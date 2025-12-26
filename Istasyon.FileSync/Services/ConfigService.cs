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
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        _config = LoadConfig();
    }

    public AppConfig Config => _config;

    private AppConfig LoadConfig()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
        }
        return new AppConfig();
    }

    public void SaveConfig(AppConfig config)
    {
        _config = config;
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(_configPath, json);
    }
}
