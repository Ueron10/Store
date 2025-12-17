using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class EmployeeDashboardPage : ContentPage
{
    public EmployeeDashboardPage()
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
        // Set current date
        DateLabel.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");

        var today = DateTime.Today;
        var range = new DateRange(today, today.AddDays(1).AddTicks(-1));
        var summary = DataStore.GetSummary(range);

        EmpDailyTransactionsLabel.Text = $"{summary.TotalTransactions} Transaksi";
        EmpDailySalesLabel.Text = $"Rp {summary.TotalSales:N0}";

        BuildStockSummary();
        BuildLowStockList();
        BuildRecentTransactions();
    }

    private void BuildStockSummary()
    {
        int criticalCount = 0;

        foreach (var product in DataStore.Products)
        {
            int stockQty = DataStore.GetCurrentStock(product.Id);
            if (stockQty <= 5)
            {
                criticalCount++;
            }
        }

        EmpCriticalStockCountLabel.Text = $"{criticalCount} Item";
    }

    private void BuildLowStockList()
    {
        EmpLowStockContainer.Children.Clear();

        var lowStockProducts = DataStore.Products
            .Select(p => new { Product = p, Qty = DataStore.GetCurrentStock(p.Id) })
            .Where(x => x.Qty > 0 && x.Qty <= 10)
            .OrderBy(x => x.Qty)
            .Take(5)
            .ToList();

        if (!lowStockProducts.Any())
        {
            EmpLowStockContainer.Children.Add(new Label
            {
                Text = "Belum ada data stok menipis.",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        foreach (var item in lowStockProducts)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Auto }
                }
            };

            var nameLabel = new Label
            {
                Text = item.Product.Name,
                FontSize = 14,
                TextColor = Color.FromArgb("#374151"),
            };
            grid.Add(nameLabel, 0, 0);

            var qtyLabel = new Label
            {
                Text = $"Tersisa: {item.Qty} {item.Product.Unit}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };
            grid.Add(qtyLabel, 1, 0);

            EmpLowStockContainer.Children.Add(grid);
        }
    }

    private void BuildRecentTransactions()
    {
        EmpRecentTransactionsContainer.Children.Clear();

        var today = DateTime.Today;
        var todaySales = DataStore.Sales
            .Where(s => s.Timestamp.Date == today)
            .OrderByDescending(s => s.Timestamp)
            .Take(5)
            .ToList();

        if (!todaySales.Any())
        {
            EmpRecentTransactionsContainer.Children.Add(new Label
            {
                Text = "Belum ada data transaksi untuk ditampilkan.",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        foreach (var sale in todaySales)
        {
            string timeText = sale.Timestamp.ToString("HH:mm");

            string itemsText;
            if (sale.Items != null && sale.Items.Count > 0)
            {
                if (sale.Items.Count == 1)
                {
                    var item = sale.Items[0];
                    itemsText = $"{item.ProductName} x{item.Quantity}";
                }
                else
                {
                    var first = sale.Items[0];
                    itemsText = $"{first.ProductName} dan {sale.Items.Count - 1} item lain";
                }
            }
            else
            {
                itemsText = "Transaksi"; // data item tidak dimuat dari database
            }

            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F3F4F6"),
                CornerRadius = 8,
                Padding = 12,
                HasShadow = false
            };

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

            frame.Content = grid;
            EmpRecentTransactionsContainer.Children.Add(frame);
        }
    }

    private void OnNewTransactionClicked(object sender, EventArgs e)
    {
        ShellNav.TrySelectTab("employeedashboard", "financial");
    }

    private void OnViewStockClicked(object sender, EventArgs e)
    {
        ShellNav.TrySelectTab("employeedashboard", "stock");
    }

    private void OnUpdateStockClicked(object sender, EventArgs e)
    {
        ShellNav.TrySelectTab("employeedashboard", "stock");
    }

    private void OnNotificationsClicked(object sender, EventArgs e)
    {
        ShellNav.TrySelectTab("employeedashboard", "notifications");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await SessionManager.LogoutAsync();
    }
}
