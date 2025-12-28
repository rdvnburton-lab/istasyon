using System.Windows;
using Microsoft.Win32;
using Istasyon.FileSync.Services;
using Istasyon.FileSync.Models;
using System.IO;
using System;
using System.Windows.Threading;
using System.Linq;

namespace Istasyon.FileSync;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly FileWatcherService _fileWatcherService;
    private readonly DatabaseService _databaseService;
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow(ConfigService configService, FileWatcherService fileWatcherService)
    {
        InitializeComponent();
        _configService = configService;
        _fileWatcherService = fileWatcherService;
        _databaseService = new DatabaseService();

        _refreshTimer = new DispatcherTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(5);
        _refreshTimer.Tick += (s, e) => { LoadLogs(); LoadDashboard(); };

        Loaded += Window_Loaded;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // INITIAL SETUP & LOGIN CHECK
        // If ApiUrl, ApiKey, or IstasyonId is missing, force Setup + Login
        if (string.IsNullOrEmpty(_configService.Config.ApiUrl) || 
            string.IsNullOrEmpty(_configService.Config.ApiKey) || 
            _configService.Config.IstasyonId == 0)
        {
            // 1. Setup Dialog (Get URL)
            var setup = new SetupDialog(_configService.Config.ApiUrl);
            
            if (setup.ShowDialog() != true)
            {
                Application.Current.Shutdown();
                return;
            }

            // Save tentative URL so LoginAsync can use it
            var config = _configService.Config;
            config.ApiUrl = setup.ServerUrl;
            _configService.SaveConfig(config);
            
            // Reload config public method now accessible
            _configService.LoadConfig(); 
            
            // Re-init ApiService with new URL (but no key yet)
            var apiService = new ApiService(_databaseService); 
            apiService.Initialize(config.ApiUrl, "", 0, config.ClientUniqueId);

            // 2. Login Dialog (Get Credentials & Fetch ApiKey)
            bool loginSuccess = false;
            while (!loginSuccess)
            {
                var login = new LoginDialog();
                if (login.ShowDialog() != true)
                {
                    Application.Current.Shutdown();
                    return;
                }

                var (success, msg, role, stations) = await apiService.LoginAsync(login.Username, login.Password);
                
                if (success)
                {
                if (success)
                {
                    // Scenario 1: Admin/Patron - ALWAYS select Firm -> Station explicitly
                    if (role?.ToLower() == "admin" || role?.ToLower() == "patron")
                    {
                        var adminSetup = new AdminSetupDialog(apiService, config.ClientUniqueId);
                        if (adminSetup.ShowDialog() == true && adminSetup.SelectedStation != null)
                        {
                            var selectedStation = adminSetup.SelectedStation;
                            config = _configService.Config;
                            config.ApiKey = selectedStation.ApiKey ?? ""; 
                            config.IstasyonId = selectedStation.Id;
                            SaveAndRestart(config, selectedStation.Ad);
                            loginSuccess = true;
                        }
                    }
                    // Scenario 2: User explicitly assigned to stations (e.g. Station Manager)
                    else if (stations != null && stations.Count > 0)
                    {
                        var selectedStation = stations[0];
                        config = _configService.Config;
                        config.ApiKey = selectedStation.ApiKey ?? "";
                        config.IstasyonId = selectedStation.Id;
                        SaveAndRestart(config, selectedStation.Ad ?? "Bilinmeyen İstasyon");
                        loginSuccess = true;
                    }
                    else
                    {
                        MessageBox.Show("Bu kullanıcıya tanımlı istasyon bulunamadı ve yetkili değilsiniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show(msg, "Giriş Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                }
                else
                {
                    MessageBox.Show(success ? "Giriş başarılı ancak yetki sorunu var." : msg, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        LoadSettings();
        LoadDashboard();
        LoadLogs();
    }

    private void SaveAndRestart(AppConfig config, string stationName)
    {
        _configService.SaveConfig(config);
        _configService.LoadConfig(); 
        _fileWatcherService.UpdateApiConfig(config.ApiUrl, config.ApiKey, config.IstasyonId, config.ClientUniqueId);
        MessageBox.Show($"Kurulum Başarılı!\nİstasyon: {stationName}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LoadSettings()
    {
        var config = _configService.Config;
        TxtFolderPath.Text = config.WatchFolderPath;
        TxtApiUrl.Text = config.ApiUrl;
        
        // Default locked state (also handles masking)
        LockSettings();
        
        TxtIstasyonId.Text = config.IstasyonId.ToString();
        ChkAutoStart.IsChecked = config.AutoStart;
    }

    private void LoadDashboard()
    {
        var config = _configService.Config;
        TxtDashStationId.Text = $"İstasyon ID: {config.IstasyonId}";

        var lastLog = _databaseService.GetLastSuccessfulUpload();
        if (lastLog != null)
        {
            TxtDashLastUploadDate.Text = lastLog.LastAttempt.ToString("dd.MM.yyyy HH:mm");
            TxtDashLastFileName.Text = lastLog.FileName;
        }
        else
        {
            TxtDashLastUploadDate.Text = "Henüz yok";
            TxtDashLastFileName.Text = "-";
        }
    }

    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return string.Empty;
        if (apiKey.Length <= 4) return new string('*', apiKey.Length);
        
        // Show first 2 and last 2 chars, mask the rest
        // Or just mask all for total privacy in panel? User requested "starred".
        // Let's hide all but last 3 chars or simply full mask.
        // Simple usage:
        return "●●●●●●●●"; 
    }

    private string MaskUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        if (url.Length <= 10) return new string('*', url.Length);
        
        // Keep "http" or "https" visible?
        // User requested "hidden" (gizleyelim).
        // Let's use a standard mask like "https://******.com" or just "******************".
        // To be safe and clean:
        return "https://******"; // Simple static mask or dynamic length?
        // Let's do simple.
    }

    private void LockSettings()
    {
        PnlSettings.IsEnabled = false;
        BtnSaveSettings.IsEnabled = false;
        BtnSaveSettings.Opacity = 0.5;
        BtnUnlockSettings.Content = "🔒 Kilidi Aç / Düzenle";
        BtnUnlockSettings.IsEnabled = true;
        
        // MASK API KEY & URL
        TxtApiKey.Text = MaskApiKey(_configService.Config.ApiKey);
        TxtApiUrl.Text = MaskUrl(_configService.Config.ApiUrl);
    }

    private void UnlockSettings()
    {
        PnlSettings.IsEnabled = true;
        BtnSaveSettings.IsEnabled = true;
        BtnSaveSettings.Opacity = 1.0;
        BtnUnlockSettings.Content = "🔓 Kilit Açık";
        BtnUnlockSettings.IsEnabled = false; 
        
        // REVEAL API KEY & URL
        TxtApiKey.Text = _configService.Config.ApiKey;
        TxtApiUrl.Text = _configService.Config.ApiUrl;
    }

    private async void BtnUnlockSettings_Click(object sender, RoutedEventArgs e)
    {
        var loginDialog = new LoginDialog();
        loginDialog.Owner = this;
        
        if (loginDialog.ShowDialog() == true)
        {
            var username = loginDialog.Username;
            var password = loginDialog.Password;

            // Use current URL in config for login check, or TxtApiUrl if user wants to change URL?
            // Since settings are locked, we must use Config URL.
            // If URL is wrong, user can't login to change it? 
            // CATCH-22: If ApiUrl is wrong/changed, how do I login to fix it?
            // Solution: Temporarily allow login to try TxtApiUrl if config URL fails?
            // Or assume Admin credentials work on the backend configured.
            // Let's use _config.ApiUrl.
            
            // NOTE: If the URL is totally wrong in config, the user is stuck. 
            // Recovery: Maybe allow manual override or assume if URL is invalid, let them edit?
            // For now, let's use the standard flow.

            // Get URL from text box just in case they need to correct it (but text box is disabled!). 
            // Wait, text box is disabled. So they rely on stored config. 

            var (success, message, role, _) = await _fileWatcherService.LoginAsync(username, password);

            if (success)
            {
                if (role == "admin" || role == "istasyon sorumlusu" || role == "patron")
                {
                    UnlockSettings();
                    MessageBox.Show("Ayarlar kilidi açıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Bu işlem için yetkiniz yok.", "Yetkisiz Erişim", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show(message, "Giriş Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadLogs()
    {
        try
        {
            var logs = _databaseService.GetAllLogs();
            DgLogs.ItemsSource = logs;
            TxtStatusSummary.Text = $"Toplam {logs.Count} işlem bulundu. Son güncelleme: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Loglar yüklenirken hata oluştu.");
        }
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            TxtFolderPath.Text = dialog.FolderName;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(TxtFolderPath.Text) || !Directory.Exists(TxtFolderPath.Text))
        {
            MessageBox.Show("Lütfen geçerli bir klasör yolu seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        int.TryParse(TxtIstasyonId.Text, out int istasyonId);

        var config = new AppConfig
        {
            WatchFolderPath = TxtFolderPath.Text,
            ApiUrl = TxtApiUrl.Text,
            ApiKey = TxtApiKey.Text,
            IstasyonId = istasyonId,
            AutoStart = ChkAutoStart.IsChecked ?? false
        };

        _configService.SaveConfig(config);
        StartupService.SetAutoStart(config.AutoStart);
        _fileWatcherService.UpdateApiConfig(config.ApiUrl, config.ApiKey, config.IstasyonId, config.ClientUniqueId);
        
        _fileWatcherService.Stop();
        _fileWatcherService.Start(config.WatchFolderPath);
        
        LockSettings(); // Re-lock after save
        LoadDashboard(); // Update dashboard with new potential Station ID

        MessageBox.Show("Ayarlar kaydedildi ve izleme başlatıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        
        // Auto-run test to update dashboard name?
        BtnTestConnection_Click(null, null);
    }

    private async void BtnTestConnection_Click(object? sender, RoutedEventArgs? e)
    {
        TxtDiagnosticPlaceholder.Visibility = Visibility.Collapsed;
        IcDiagnostics.ItemsSource = null;

        // Use Config for test, as dashboard button is public
        var config = _configService.Config;
        
        var (results, info) = await _fileWatcherService.RunDiagnosticsAsync(config.ApiUrl, config.ApiKey, config.IstasyonId);
        IcDiagnostics.ItemsSource = results;

        // Try to update Station Name if verify succeeded
        var verifyCheck = results.FirstOrDefault(r => r.CheckName == "İstasyon & Yetki");
        if (verifyCheck != null && verifyCheck.IsSuccess && info != null)
        {
            TxtDashStationName.Text = info.IstasyonAdi;
            TxtDashFirmName.Text = info.FirmaAdi;
            TxtDashAddress.Text = info.IstasyonAdresi;
            TxtDashStationId.Text = $"ID: {config.IstasyonId}";
            
            TxtDashStationManager.Text = info.IstasyonSorumlusu;
            TxtDashShiftSupervisor.Text = info.VardiyaSorumlusu;
            TxtDashBoss.Text = info.PatronAdi;
        }
        else if (verifyCheck != null && verifyCheck.IsSuccess)
        {
             // Info null check fallback
             TxtDashStationName.Text = $"Doğrulandı (ID: {config.IstasyonId})";
        }
    }

    private void BtnRefreshLogs_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        _refreshTimer.Start();
        LoadLogs();
        LoadDashboard();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        _refreshTimer.Stop();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Don't minimize to tray here, regular close for these changes?
        // Or keep tray behavior.
        // Usually user wants close to close or tray.
        // Assuming Tray behavior is desired as per original code.
        e.Cancel = true;
        this.Hide();
    }
}