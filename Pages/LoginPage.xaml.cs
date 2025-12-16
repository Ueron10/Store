using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text?.Trim() ?? string.Empty;

        // Hide error message
        ErrorLabel.IsVisible = false;

        // Validate input
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ErrorLabel.Text = "Username dan password harus diisi!";
            ErrorLabel.IsVisible = true;
            return;
        }

        var user = AuthService.ValidateLogin(username, password);

        if (user != null)
        {
            // Tentukan role berdasarkan data user (bukan radio button)
            if (string.Equals(user.Role, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.GoToAsync("//ownerdashboard");
            }
            else
            {
                await Shell.Current.GoToAsync("//employeedashboard");
            }
        }
        else
        {
            ErrorLabel.Text = "Username atau password salah!";
            ErrorLabel.IsVisible = true;
        }
    }
}
