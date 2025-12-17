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
        var phone = PhoneEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Username wajib diisi.");
            return;
        }

        var user = DataStore.Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            ShowError("Akun tidak ditemukan.");
            return;
        }

        // Cocokkan hanya field yang memang tersimpan di database.
        // Ini bikin fitur tetap bisa dipakai walau ada akun lama yang Email/Phone-nya kosong.
        var storedEmail = user.Email?.Trim() ?? string.Empty;
        var storedPhone = user.Phone?.Trim() ?? string.Empty;

        var hasEmail = !string.IsNullOrWhiteSpace(storedEmail);
        var hasPhone = !string.IsNullOrWhiteSpace(storedPhone);

        if (hasEmail)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Email wajib diisi untuk akun ini.");
                return;
            }

            if (!storedEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Email tidak cocok.");
                return;
            }
        }

        if (hasPhone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                ShowError("No. telepon wajib diisi untuk akun ini.");
                return;
            }

            if (!NormalizePhone(storedPhone).Equals(NormalizePhone(phone), StringComparison.OrdinalIgnoreCase))
            {
                ShowError("No. telepon tidak cocok.");
                return;
            }
        }

        if (!hasEmail && !hasPhone)
        {
            // Akun belum punya Email/Phone di DB → izinkan berdasarkan username saja.
            // (Untuk tugas/assignment; di production sebaiknya pakai OTP/email reset.)
        }

        PasswordLabel.Text = user.Password;
        PasswordBox.IsVisible = true;

        ConfirmButton.Text = "Tutup";
        _revealed = true;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
        PasswordBox.IsVisible = false;
    }

    private static string NormalizePhone(string input)
    {
        var digits = new string((input ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits;
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
