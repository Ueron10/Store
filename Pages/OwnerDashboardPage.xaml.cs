using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class OwnerDashboardPage : ContentPage
{
    public OwnerDashboardPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        // Tanggal hari ini
        DateLabel.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");

        // Ringkasan keuangan hari ini
        var today = DateTime.Today;
        var range = new DateRange(today, today.AddDays(1).AddTicks(-1));
        var summary = DataStore.GetSummary(range);

        OwnerDailySalesLabel.Text = $"Rp {summary.TotalSales:N0}";
        OwnerDailyProfitLabel.Text = $"Rp {summary.NetProfit:N0}";
        OwnerTotalProductsLabel.Text = $"{DataStore.Products.Count} Item";
        OwnerDailyTransactionsLabel.Text = $"{summary.TotalTransactions} Transaksi";

        BuildStockAlerts();
        BuildRecentTransactions();
    }

    private void BuildStockAlerts()
    {
        OwnerStockAlertsContainer.Children.Clear();

        var (nearing, expired) = DataStore.GetExpiryAlerts();

        if (!nearing.Any() && !expired.Any())
        {
            OwnerStockAlertsContainer.Children.Add(new Label
            {
                Text = "Tidak ada peringatan stok.",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        int expiredCount = expired
            .Where(b => b.Quantity > 0)
            .Select(b => b.ProductId)
            .Distinct()
            .Count();

        int nearingCount = nearing
            .Where(b => b.Quantity > 0)
            .Select(b => b.ProductId)
            .Distinct()
            .Count();

        if (expiredCount > 0)
        {
            OwnerStockAlertsContainer.Children.Add(new Label
            {
                Text = $"• {expiredCount} produk sudah kadaluarsa",
                FontSize = 14,
                TextColor = Colors.Red
            });
        }

        if (nearingCount > 0)
        {
            OwnerStockAlertsContainer.Children.Add(new Label
            {
                Text = $"• {nearingCount} produk mendekati kadaluarsa",
                FontSize = 14,
                TextColor = Color.FromArgb("#F97316") // orange
            });
        }
    }

    private void BuildRecentTransactions()
    {
        OwnerRecentTransactionsContainer.Children.Clear();

        var today = DateTime.Today;
        var recentSales = DataStore.Sales
            .OrderByDescending(s => s.Timestamp)
            .Take(5)
            .ToList();

        if (!recentSales.Any())
        {
            OwnerRecentTransactionsContainer.Children.Add(new Label
            {
                Text = "Belum ada data transaksi untuk ditampilkan.",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        foreach (var sale in recentSales)
        {
            string timeText = sale.Timestamp.ToString("HH:mm");
            string itemsText = sale.Items.Count == 1
                ? $"{sale.Items[0].ProductName} x{sale.Items[0].Quantity}"
                : $"{sale.Items[0].ProductName} dan {sale.Items.Count - 1} item lain";

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Auto }
                }
            };

            var textStack = new StackLayout();
            textStack.Children.Add(new Label
            {
                Text = itemsText,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#111827")
            });
            textStack.Children.Add(new Label
            {
                Text = $"{sale.PaymentMethod} - {timeText}",
                FontSize = 12,
                TextColor = Color.FromArgb("#6B7280")
            });

            grid.Add(textStack, 0, 0);

            grid.Add(new Label
            {
                Text = $"Rp {sale.GrossAmount:N0}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Green,
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            OwnerRecentTransactionsContainer.Children.Add(grid);
            OwnerRecentTransactionsContainer.Children.Add(new BoxView
            {
                BackgroundColor = Color.FromArgb("#E5E7EB"),
                HeightRequest = 1
            });
        }

        // Hapus garis pemisah terakhir agar lebih rapi
        if (OwnerRecentTransactionsContainer.Children.LastOrDefault() is BoxView)
        {
            OwnerRecentTransactionsContainer.Children.RemoveAt(OwnerRecentTransactionsContainer.Children.Count - 1);
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("users");
    }

    private async void OnViewStockAlertsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ownerdashboard/notifications");
    }

    private async void OnViewAllTransactionsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ownerdashboard/financial");
    }

    private async void OnReportsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ownerdashboard/reports");
    }

    private async void OnManageStockClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ownerdashboard/stock");
    }

    private async void OnFinancialClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ownerdashboard/financial");
    }

    private async void OnNotificationsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ownerdashboard/notifications");
    }

    private async void OnStockOpnameClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("stockopname");
    }
}
