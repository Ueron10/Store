using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildExpiryAlerts();
    }

    private void BuildExpiryAlerts()
    {
        ExpiredContainer.Children.Clear();
        NearingContainer.Children.Clear();

        var (nearing, expired) = DataStore.GetExpiryAlerts();

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (!expired.Any())
        {
            ExpiredContainer.Children.Add(new Label
            {
                Text = "Tidak ada produk yang sudah kadaluarsa.",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
        }
        else
        {
            // Tampilkan per batch agar produk yang sama tapi expiry berbeda tetap terlihat.
            foreach (var batch in expired.OrderBy(b => b.ExpiryDate))
            {
                var product = DataStore.Products.FirstOrDefault(p => p.Id == batch.ProductId);
                if (product == null) continue;

                var frame = new Frame
                {
                    BackgroundColor = Colors.White,
                    CornerRadius = 10,
                    Padding = 15,
                    HasShadow = true
                };

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition{ Width = GridLength.Star },
                        new ColumnDefinition{ Width = GridLength.Auto }
                    },
                    ColumnSpacing = 10
                };

                var textStack = new StackLayout();
                textStack.Children.Add(new Label
                {
                    Text = product.Name,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#111827")
                });
                textStack.Children.Add(new Label
                {
                    Text = $"Batch: {batch.Quantity} {product.Unit}",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#6B7280")
                });
                textStack.Children.Add(new Label
                {
                    Text = $"Kadaluarsa: {batch.ExpiryDate:dd MMM yyyy}",
                    FontSize = 12,
                    TextColor = Colors.Red
                });

                grid.Add(textStack, 0, 0);

                // Hapus semua stok expired untuk produk ini (lebih aman daripada per-batch removal)
                var removeButton = new Button
                {
                    Text = "Hapus Stok Expired",
                    BackgroundColor = Colors.Red,
                    TextColor = Colors.White,
                    CornerRadius = 8,
                    Padding = new Thickness(10, 6),
                    FontSize = 12,
                    CommandParameter = product.Id
                };
                removeButton.Clicked += OnRemoveExpiredClicked;

                grid.Add(removeButton, 1, 0);

                frame.Content = grid;
                ExpiredContainer.Children.Add(frame);
            }
        }

        if (!nearing.Any())
        {
            NearingContainer.Children.Add(new Label
            {
                Text = "Tidak ada produk yang mendekati kadaluarsa (≤ 14 hari).",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
        }
        else
        {
            foreach (var batch in nearing.OrderBy(b => b.ExpiryDate))
            {
                var product = DataStore.Products.FirstOrDefault(p => p.Id == batch.ProductId);
                if (product == null) continue;

                int daysLeft = batch.ExpiryDate.DayNumber - today.DayNumber;
                string daysLeftText = daysLeft <= 0 ? "Hari ini" : $"{daysLeft} hari lagi";

                var frame = new Frame
                {
                    BackgroundColor = Colors.White,
                    CornerRadius = 10,
                    Padding = 15,
                    HasShadow = true
                };

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition{ Width = GridLength.Star },
                        new ColumnDefinition{ Width = GridLength.Auto }
                    },
                    ColumnSpacing = 10
                };

                var textStack = new StackLayout();
                textStack.Children.Add(new Label
                {
                    Text = product.Name,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#111827")
                });
                textStack.Children.Add(new Label
                {
                    Text = $"Batch: {batch.Quantity} {product.Unit}",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#6B7280")
                });
                textStack.Children.Add(new Label
                {
                    Text = $"Kadaluarsa: {batch.ExpiryDate:dd MMM yyyy} • {daysLeftText}",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#F97316") // orange
                });

                grid.Add(textStack, 0, 0);

                var discountButton = new Button
                {
                    Text = "Atur Diskon",
                    BackgroundColor = Color.FromArgb("#6366F1"),
                    TextColor = Colors.White,
                    CornerRadius = 8,
                    Padding = new Thickness(10, 6),
                    FontSize = 12
                };
                discountButton.Clicked += async (_, __) =>
                {
                    if (DataStore.CurrentUser != null &&
                        string.Equals(DataStore.CurrentUser.Role, "Owner", StringComparison.OrdinalIgnoreCase))
                    {
                        ShellNav.TrySelectTab("ownerdashboard", "discounts");
                    }
                    else
                    {
                        await DisplayAlert("Akses Ditolak", "Fitur diskon hanya untuk Owner.", "OK");
                    }
                };

                grid.Add(discountButton, 1, 0);

                frame.Content = grid;
                NearingContainer.Children.Add(frame);
            }
        }
    }

    private async void OnClearAllClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Konfirmasi", "Tandai semua notifikasi sebagai sudah ditangani? (Data stok tidak berubah)", "Ya", "Batal");
        if (confirm)
        {
            ExpiredContainer.Children.Clear();
            NearingContainer.Children.Clear();
            BuildExpiryAlerts();
        }
    }

    private async void OnRemoveExpiredClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not Guid productId)
            return;

        bool confirm = await DisplayAlert("Hapus Stok Expired", "Keluarkan semua stok expired untuk produk ini dari sistem?", "Ya", "Batal");
        if (!confirm) return;

        // Hitung total qty expired untuk produk ini dan keluarkan sebagai kerugian "Kadaluarsa"
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expiredBatches = DataStore.StockBatches
            .Where(b => b.ProductId == productId && b.Quantity > 0 && b.ExpiryDate < today)
            .ToList();

        int totalQty = expiredBatches.Sum(b => b.Quantity);
        if (totalQty > 0)
        {
            DataStore.AdjustStockForDamage(productId, totalQty, "Kadaluarsa");
        }

        BuildExpiryAlerts();
    }
}
