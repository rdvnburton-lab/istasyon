using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Istasyon.FileSync.Models;
using Istasyon.FileSync.Services;

namespace Istasyon.FileSync;

public partial class AdminSetupDialog : Window
{
    private readonly FileWatcherService _service;
    public IstasyonDto? SelectedStation { get; private set; }

    public AdminSetupDialog(FileWatcherService service)
    {
        InitializeComponent();
        _service = service;
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
        BtnSave.IsEnabled = CmbStations.SelectedItem != null;
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
