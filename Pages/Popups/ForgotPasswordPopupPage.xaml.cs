using StoreProgram.Services;

namespace StoreProgram.Pages.Popups;

public partial class ForgotPasswordPopupPage : ContentPage
{
    private bool _revealed;

    public ForgotPasswordPopupPage()
    {
        InitializeComponent();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (_revealed)
        {
            await Navigation.PopModalAsync(animated: false);
            return;
        }

        ErrorLabel.IsVisible = false;

        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Username wajib diisi.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Email wajib diisi.");
            return;
        }

        // Query langsung ke database supaya selalu up-to-date.
        ConfirmButton.IsEnabled = false;
        try
        {
            var password = await Task.Run(() => DatabaseService.GetPasswordByUsernameAndEmail(username, email));
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Username atau email tidak cocok.");
                return;
            }

            PasswordLabel.Text = password;
            PasswordBox.IsVisible = true;

            ConfirmButton.Text = "Tutup";
            _revealed = true;
        }
        finally
        {
            ConfirmButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
        PasswordBox.IsVisible = false;
    }

    public static async Task ShowAsync()
    {
        var page = new ForgotPasswordPopupPage();

        // Di Shell, lebih aman pakai Navigation dari CurrentPage daripada Shell.Current.Navigation.
        var hostPage = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        var nav = hostPage?.Navigation;
        if (nav == null) return;

        await nav.PushModalAsync(page, animated: false);
    }
}
