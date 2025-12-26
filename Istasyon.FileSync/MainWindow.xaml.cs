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
        _databaseService = new DatabaseService(); // App.xaml.cs'deki ile aynı DB'ye bağlanır

        _refreshTimer = new DispatcherTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(5);
        _refreshTimer.Tick += (s, e) => LoadLogs();

        LoadSettings();
        LoadLogs();
    }

    private void LoadSettings()
    {
        var config = _configService.Config;
        TxtFolderPath.Text = config.WatchFolderPath;
        TxtApiUrl.Text = config.ApiUrl;
        TxtApiKey.Text = config.ApiKey;
        TxtIstasyonId.Text = config.IstasyonId.ToString();
        ChkAutoStart.IsChecked = config.AutoStart;
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
        _fileWatcherService.UpdateApiConfig(config.ApiUrl, config.ApiKey, config.IstasyonId);
        
        _fileWatcherService.Stop();
        _fileWatcherService.Start(config.WatchFolderPath);

        MessageBox.Show("Ayarlar kaydedildi ve izleme başlatıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtApiUrl.Text;
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Lütfen önce bir API URL girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (success, message) = await _fileWatcherService.TestConnectionAsync(url);
        
        if (success)
            MessageBox.Show(message, "Bağlantı Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show(message, "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
    }


    private void BtnRefreshLogs_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        _refreshTimer.Start();
        LoadLogs();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        _refreshTimer.Stop();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
    }
}