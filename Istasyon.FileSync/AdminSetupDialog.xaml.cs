using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Istasyon.FileSync.Models;
using Istasyon.FileSync.Services;

namespace Istasyon.FileSync;

public partial class AdminSetupDialog : Window
{
    private readonly ApiService _service;
    private readonly string _currentClientId;
    public IstasyonDto? SelectedStation { get; private set; }

    public AdminSetupDialog(ApiService service, string currentClientId)
    {
        InitializeComponent();
        _service = service;
        _currentClientId = currentClientId;
        Loaded += AdminSetupDialog_Loaded;
    }

    private async void AdminSetupDialog_Loaded(object sender, RoutedEventArgs e)
    {
        var firms = await _service.GetFirmasAsync();
        CmbFirms.ItemsSource = firms;
    }

    private async void CmbFirms_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CmbStations.ItemsSource = null;
        CmbStations.IsEnabled = false;
        BtnSave.IsEnabled = false;

        if (CmbFirms.SelectedItem is FirmaDto firma)
        {
            var stations = await _service.GetStationsAsync(firma.Id);
            CmbStations.ItemsSource = stations;
            CmbStations.IsEnabled = true;
        }
    }

    private void CmbStations_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbStations.SelectedItem is IstasyonDto station)
        {
            SelectedStation = station;
            RefreshUnlockUI(station);
        }
        else
        {
            BtnSave.IsEnabled = false;
            TxtWarning.Visibility = Visibility.Collapsed;
            BtnUnlock.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnUnlock_Click(object sender, RoutedEventArgs e)
    {
        if (CmbStations.SelectedItem is IstasyonDto station)
        {
            var result = await _service.UnlockStationAsync(station.Id);
            if (result)
            {
                MessageBox.Show("İstasyon kilidi başarıyla kaldırıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                station.RegisteredDeviceId = null; // Update local state
                RefreshUnlockUI(station); // Refresh UI state
            }
            else
            {
                MessageBox.Show("Kilit kaldırılamadı. Yetkinizi kontrol edin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RefreshUnlockUI(IstasyonDto station)
    {
        // Warn ONLY if it's locked by ANOTHER device
        bool isLockedByOther = !string.IsNullOrEmpty(station.RegisteredDeviceId) && 
                              station.RegisteredDeviceId != _currentClientId;
                              
        TxtWarning.Visibility = isLockedByOther ? Visibility.Visible : Visibility.Collapsed;
        BtnUnlock.Visibility = isLockedByOther ? Visibility.Visible : Visibility.Collapsed;
        
        // DEBUG INFO to solve user's persistent loop
        if (isLockedByOther)
        {
            TxtDebug.Text = $"Bizim ID: {_currentClientId}\nSunucu ID: {station.RegisteredDeviceId}";
            TxtDebug.Visibility = Visibility.Visible;
        }
        else
        {
            TxtDebug.Visibility = Visibility.Collapsed;
        }

        BtnSave.IsEnabled = true;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbStations.SelectedItem is IstasyonDto station)
        {
            SelectedStation = station;
            DialogResult = true;
            Close();
        }
    }
}
