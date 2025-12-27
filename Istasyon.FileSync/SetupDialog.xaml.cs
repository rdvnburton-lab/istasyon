using System;
using System.Windows;

namespace Istasyon.FileSync;

public partial class SetupDialog : Window
{
    public string ServerUrl { get; set; } = string.Empty;

    public SetupDialog(string initialUrl = "")
    {
        InitializeComponent();
        if (!string.IsNullOrEmpty(initialUrl))
        {
            ServerUrl = initialUrl;
            TxtUrl.Text = initialUrl;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtUrl.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Lütfen geçerli bir URL giriniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            MessageBox.Show("Lütfen 'http://' veya 'https://' ile başlayan geçerli bir URL giriniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ServerUrl = url;
        DialogResult = true;
        Close();
    }
}
