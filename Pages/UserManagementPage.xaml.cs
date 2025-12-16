using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class UserManagementPage : ContentPage
{
    public UserManagementPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildUsersList();
    }

    private void BuildUsersList()
    {
        UsersContainer.Children.Clear();

        var users = DataStore.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
            .ToList();

        if (!users.Any())
        {
            UsersContainer.Children.Add(new Label
            {
                Text = "Belum ada pengguna. Tekan '+ Tambah' untuk membuat akun baru.",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        foreach (var user in users)
        {
            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 10,
                Padding = 12,
                HasShadow = true
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 10
            };

            var infoStack = new StackLayout();
            infoStack.Children.Add(new Label
            {
                Text = user.Username,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#111827")
            });
            infoStack.Children.Add(new Label
            {
                Text = $"Role: {user.Role}",
                FontSize = 13,
                TextColor = Color.FromArgb("#4B5563")
            });

            if (!string.IsNullOrWhiteSpace(user.Email) || !string.IsNullOrWhiteSpace(user.Phone))
            {
                infoStack.Children.Add(new Label
                {
                    Text = $"Email: {user.Email ?? "-"} | Telp: {user.Phone ?? "-"}",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6B7280")
                });
            }

            grid.Add(infoStack, 0, 0);

            var editButton = new Button
            {
                Text = "Edit",
                BackgroundColor = Color.FromArgb("#3B82F6"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(10, 4),
                FontSize = 12,
                CommandParameter = user.Username
            };
            editButton.Clicked += OnEditUserClicked;
            grid.Add(editButton, 1, 0);

            var deleteButton = new Button
            {
                Text = "Hapus",
                BackgroundColor = Color.FromArgb("#EF4444"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(10, 4),
                FontSize = 12,
                CommandParameter = user.Username
            };
            deleteButton.Clicked += OnDeleteUserClicked;
            grid.Add(deleteButton, 2, 0);

            frame.Content = grid;
            UsersContainer.Children.Add(frame);
        }
    }

    private async void OnAddUserClicked(object sender, EventArgs e)
    {
        string username = await DisplayPromptAsync("Pengguna Baru", "Username:");
        if (string.IsNullOrWhiteSpace(username)) return;

        if (DataStore.Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlert("Error", "Username sudah digunakan.", "OK");
            return;
        }

        string password = await DisplayPromptAsync("Pengguna Baru", "Password:");
        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Password tidak boleh kosong.", "OK");
            return;
        }

        string role = await DisplayActionSheet("Pilih Role", "Batal", null, "Owner", "Employee");
        if (string.IsNullOrEmpty(role) || role == "Batal") return;

        string email = await DisplayPromptAsync("Pengguna Baru", "Email (opsional):");
        string phone = await DisplayPromptAsync("Pengguna Baru", "No. Telepon (opsional):");

        var user = new AppUser
        {
            Username = username.Trim(),
            Password = password.Trim(),
            Role = role,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
        };

        DatabaseService.InsertUser(user);
        DataStore.Users.Add(user);

        await DisplayAlert("Sukses", $"Pengguna '{user.Username}' berhasil dibuat.", "OK");
        BuildUsersList();
    }

    private async void OnEditUserClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string username)
            return;

        var user = DataStore.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return;

        string? newPassword = await DisplayPromptAsync("Edit Pengguna", "Password (kosongkan jika tidak diubah):");
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            user.Password = newPassword.Trim();
        }

        string role = await DisplayActionSheet("Role", "Batal", null, "Owner", "Employee");
        if (!string.IsNullOrEmpty(role) && role != "Batal")
        {
            user.Role = role;
        }

        string email = await DisplayPromptAsync("Edit Pengguna", "Email:", initialValue: user.Email ?? string.Empty);
        string phone = await DisplayPromptAsync("Edit Pengguna", "No. Telepon:", initialValue: user.Phone ?? string.Empty);

        user.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        user.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        DatabaseService.InsertUser(user); // InsertOrReplace
        await DisplayAlert("Sukses", $"Pengguna '{user.Username}' berhasil diperbarui.", "OK");
        BuildUsersList();
    }
    private async void OnDeleteUserClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string username)
            return;

        var user = DataStore.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return;

        // Opsional: cegah hapus diri sendiri
        if (DataStore.CurrentUser != null &&
            DataStore.CurrentUser.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlert("Tidak diizinkan", "Tidak bisa menghapus akun yang sedang digunakan.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Hapus Pengguna",
            $"Yakin ingin menghapus pengguna '{username}'?", "Ya", "Batal");
        if (!confirm) return;

        DatabaseService.DeleteUser(username);
        DataStore.Users.RemoveAll(u => u.Username == username);

        await DisplayAlert("Sukses", $"Pengguna '{username}' sudah dihapus.", "OK");
        BuildUsersList();
    }
}