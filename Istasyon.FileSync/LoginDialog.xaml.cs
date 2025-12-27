using System.Windows;

namespace Istasyon.FileSync;

public partial class LoginDialog : Window
{
    public string Username { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;

    public LoginDialog()
    {
        InitializeComponent();
        TxtUsername.Focus();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        Username = TxtUsername.Text;
        Password = TxtPassword.Password;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            MessageBox.Show("Kullanıcı adı ve şifre zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
