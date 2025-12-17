using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages.Popups;

public partial class UserFormPopupPage : ContentPage
{
    public record UserFormResult(AppUser User, bool IsNew);

    private readonly TaskCompletionSource<UserFormResult?> _tcs = new();
    private readonly bool _isEdit;
    private readonly AppUser? _editingUser;

    public UserFormPopupPage(AppUser? userToEdit)
    {
        InitializeComponent();

        RolePicker.ItemsSource = new List<string> { "Owner", "Employee" };

        _isEdit = userToEdit != null;
        _editingUser = userToEdit;

        if (_isEdit)
        {
            TitleLabel.Text = "Edit Pengguna";
            SubtitleLabel.Text = "Ubah data pengguna. Password boleh dikosongkan jika tidak ingin diubah.";

            UsernameEntry.Text = userToEdit!.Username;
            UsernameEntry.IsEnabled = false;

            PasswordLabel.Text = "Password (kosongkan jika tidak diubah)";
            PasswordEntry.Text = string.Empty;

            RolePicker.SelectedItem = string.Equals(userToEdit.Role, "Owner", StringComparison.OrdinalIgnoreCase)
                ? "Owner"
                : "Employee";

            EmailEntry.Text = userToEdit.Email ?? string.Empty;
            PhoneEntry.Text = userToEdit.Phone ?? string.Empty;
        }
        else
        {
            TitleLabel.Text = "Pengguna Baru";
            SubtitleLabel.Text = "Buat akun baru untuk aplikasi.";

            UsernameEntry.IsEnabled = true;
            RolePicker.SelectedItem = "Employee";
        }
    }

    public Task<UserFormResult?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text?.Trim() ?? string.Empty;
        var role = (RolePicker.SelectedItem as string) ?? string.Empty;
        var email = EmailEntry.Text?.Trim();
        var phone = PhoneEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Username wajib diisi.");
            return;
        }

        if (!_isEdit)
        {
            if (DataStore.Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                ShowError("Username sudah digunakan.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Password tidak boleh kosong.");
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            ShowError("Role harus dipilih.");
            return;
        }

        var resultUser = _isEdit
            ? _editingUser!
            : new AppUser();

        resultUser.Username = _isEdit ? _editingUser!.Username : username;
        resultUser.Role = role;
        resultUser.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        resultUser.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;

        // Password: set only if provided in edit, required in create
        if (!_isEdit || !string.IsNullOrWhiteSpace(password))
        {
            resultUser.Password = password;
        }

        _tcs.TrySetResult(new UserFormResult(resultUser, IsNew: !_isEdit));
        await Navigation.PopModalAsync(animated: false);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<UserFormResult?> ShowAsync(AppUser? userToEdit = null)
    {
        var page = new UserFormPopupPage(userToEdit);
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
