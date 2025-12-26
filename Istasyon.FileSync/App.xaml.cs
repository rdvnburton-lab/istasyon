using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Istasyon.FileSync.Services;
using Serilog;
using System.Windows.Controls;

namespace Istasyon.FileSync;

public partial class App : Application
{
    private TaskbarIcon? _notifyIcon;
    private ConfigService? _configService;
    private DatabaseService? _databaseService;
    private ApiService? _apiService;
    private FileWatcherService? _fileWatcherService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Serilog Yapılandırması
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/sync-log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Uygulama başlatılıyor...");

        // Servisleri Başlat
        _configService = new ConfigService();
        _databaseService = new DatabaseService();
        _apiService = new ApiService(_databaseService);
        _fileWatcherService = new FileWatcherService(_databaseService, _apiService);

        _apiService.Initialize(_configService.Config.ApiUrl, _configService.Config.ApiKey, _configService.Config.IstasyonId);
        
        if (!string.IsNullOrEmpty(_configService.Config.WatchFolderPath))
        {
            _fileWatcherService.Start(_configService.Config.WatchFolderPath);
        }

        // Tray Icon Oluştur
        CreateNotifyIcon();

        // Eğer ilk kez çalışıyorsa veya ayarlar boşsa ana pencereyi aç
        if (string.IsNullOrEmpty(_configService.Config.WatchFolderPath))
        {
            ShowMainWindow();
        }
    }

    private void CreateNotifyIcon()
    {
        _notifyIcon = new TaskbarIcon();
        _notifyIcon.IconSource = (System.Windows.Media.ImageSource)FindResource("AppLogo");
        _notifyIcon.ToolTipText = "İstasyon Dosya Senkronizasyon";
        
        var contextMenu = new ContextMenu();
        
        var settingsItem = new MenuItem { Header = "Ayarlar" };
        settingsItem.Click += (s, e) => ShowMainWindow();
        
        var exitItem = new MenuItem { Header = "Çıkış" };
        exitItem.Click += (s, e) => Shutdown();

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenu = contextMenu;
        _notifyIcon.DoubleClickCommand = new RelayCommand(ShowMainWindow);
    }

    public void ShowMainWindow()
    {
        if (MainWindow == null || !MainWindow.IsVisible)
        {
            MainWindow = new MainWindow(_configService!, _fileWatcherService!);
            MainWindow.Show();
        }
        else
        {
            MainWindow.Activate();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        _fileWatcherService?.Stop();
        Log.Information("Uygulama kapatıldı.");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

// Basit bir RelayCommand sınıfı
public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}
