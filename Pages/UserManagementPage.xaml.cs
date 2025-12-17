using StoreProgram.Models;
using StoreProgram.Pages.Popups;
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
        var result = await UserFormPopupPage.ShowAsync();
        if (result == null) return;

        var user = result.User;

        DatabaseService.InsertUser(user);
        DataStore.Users.Add(user);

        await InfoPopupPage.ShowAsync("Sukses", $"Pengguna '{user.Username}' berhasil dibuat.");
        BuildUsersList();
    }

    private async void OnEditUserClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string username)
            return;

        var user = DataStore.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return;

        var result = await UserFormPopupPage.ShowAsync(user);
        if (result == null) return;

        // result.User adalah reference yang sama dengan 'user' (edit mode)
        DatabaseService.InsertUser(result.User); // InsertOrReplace

        await InfoPopupPage.ShowAsync("Sukses", $"Pengguna '{user.Username}' berhasil diperbarui.");
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
            await InfoPopupPage.ShowAsync("Tidak diizinkan", "Tidak bisa menghapus akun yang sedang digunakan.");
            return;
        }

        bool confirm = await ConfirmPopupPage.ShowAsync(
            title: "Hapus Pengguna",
            message: $"Yakin ingin menghapus pengguna '{username}'?",
            confirmText: "Hapus",
            cancelText: "Batal");
        if (!confirm) return;

        DatabaseService.DeleteUser(username);
        DataStore.Users.RemoveAll(u => u.Username == username);

        await InfoPopupPage.ShowAsync("Sukses", $"Pengguna '{username}' sudah dihapus.");
        BuildUsersList();
    }
}