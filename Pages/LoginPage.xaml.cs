namespace StoreProgram.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim();
        string password = PasswordEntry.Text?.Trim();

        // Hide error message
        ErrorLabel.IsVisible = false;

        // Validate input
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ErrorLabel.Text = "Username dan password harus diisi!";
            ErrorLabel.IsVisible = true;
            return;
        }

        // Simulate login validation
        bool isValidLogin = ValidateLogin(username, password);

        if (isValidLogin)
        {
            // Determine role and navigate accordingly
            if (OwnerRadio.IsChecked)
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

    private bool ValidateLogin(string username, string password)
    {
        // Mock validation - replace with actual authentication
        return (username == "owner" && password == "owner123") || 
               (username == "pegawai" && password == "pegawai123");
    }
}