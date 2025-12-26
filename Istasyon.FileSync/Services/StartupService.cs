using Microsoft.Win32;
using System;
using System.IO;

namespace Istasyon.FileSync.Services;

public static class StartupService
{
    private const string AppName = "IstasyonFileSync";

    public static void SetAutoStart(bool enable)
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) return;

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
            {
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Otomatik başlatma ayarı değiştirilemedi.");
        }
    }
}
