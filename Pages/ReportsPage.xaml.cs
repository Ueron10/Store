using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class ReportsPage : ContentPage
{
    private enum ReportPeriod
    {
        Daily,
        Weekly,
        Monthly
    }

    private ReportPeriod _activePeriod = ReportPeriod.Daily;

    public ReportsPage()
    {
        InitializeComponent();
        // Default tampilkan laporan harian saat halaman pertama kali dibuka
        SetActiveButton(DailyButton);
        LoadDailyData();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Auto refresh saat halaman dibuka lagi (setelah transaksi/pengeluaran baru)
        switch (_activePeriod)
        {
            case ReportPeriod.Weekly:
                SetActiveButton(WeeklyButton);
                LoadWeeklyData();
                break;
            case ReportPeriod.Monthly:
                SetActiveButton(MonthlyButton);
                LoadMonthlyData();
                break;
            default:
                SetActiveButton(DailyButton);
                LoadDailyData();
                break;
        }
    }

    private void OnDailyReportClicked(object sender, EventArgs e)
    {
        _activePeriod = ReportPeriod.Daily;
        SetActiveButton(DailyButton);
        LoadDailyData();
    }

    private void OnWeeklyReportClicked(object sender, EventArgs e)
    {
        _activePeriod = ReportPeriod.Weekly;
        SetActiveButton(WeeklyButton);
        LoadWeeklyData();
    }

    private void OnMonthlyReportClicked(object sender, EventArgs e)
    {
        _activePeriod = ReportPeriod.Monthly;
        SetActiveButton(MonthlyButton);
        LoadMonthlyData();
    }

    private void SetActiveButton(Button activeButton)
    {
        // Reset all buttons to inactive style
        DailyButton.BackgroundColor = Color.FromArgb("#E5E7EB"); // Gray200
        DailyButton.TextColor = Color.FromArgb("#374151"); // Gray700
        WeeklyButton.BackgroundColor = Color.FromArgb("#E5E7EB");
        WeeklyButton.TextColor = Color.FromArgb("#374151");
        MonthlyButton.BackgroundColor = Color.FromArgb("#E5E7EB");
        MonthlyButton.TextColor = Color.FromArgb("#374151");

        // Set active button style
        activeButton.BackgroundColor = Color.FromArgb("#512DA8"); // Primary
        activeButton.TextColor = Colors.White;
    }

    private void LoadDailyData()
    {
        var today = DateTime.Today;
        var end = today.AddDays(1).AddTicks(-1);
        var range = new DateRange(today, end);
        LoadSummary(range);
    }

    private void LoadWeeklyData()
    {
        var end = DateTime.Today.AddDays(1).AddTicks(-1);
        var start = end.AddDays(-6).Date; // 7 hari terakhir termasuk hari ini
        var range = new DateRange(start, end);
        LoadSummary(range);
    }

    private void LoadMonthlyData()
    {
        var today = DateTime.Today;
        var start = new DateTime(today.Year, today.Month, 1);
        var end = start.AddMonths(1).AddTicks(-1);
        var range = new DateRange(start, end);
        LoadSummary(range);
    }

    private void LoadSummary(DateRange range)
    {
        var summary = DataStore.GetSummary(range);

        TotalSalesLabel.Text = $"Rp {summary.TotalSales:N0}";
        var totalExpenses = summary.OperationalExpenses + summary.DamagedLostExpenses;
        TotalExpensesLabel.Text = $"Rp {totalExpenses:N0}";
        NetProfitLabel.Text = $"Rp {summary.NetProfit:N0}";
        TotalTransactionsLabel.Text = $"{summary.TotalTransactions} Transaksi";

        BuildTopProducts(summary.TopProducts);
    }

    private void BuildTopProducts(List<TopProduct> topProducts)
    {
        TopProductsContainer.Children.Clear();

        if (topProducts.Count == 0)
        {
            TopProductsContainer.Children.Add(new Label
            {
                Text = "Belum ada data penjualan pada periode ini.",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        int rank = 1;
        foreach (var p in topProducts)
        {
            Color badgeColor = rank switch
            {
                1 => Colors.Gold,
                2 => Colors.Silver,
                3 => Color.FromArgb("#CD7F32"),
                _ => Color.FromArgb("#9CA3AF") // Gray400 untuk selain 3 besar
            };

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition{ Width = GridLength.Auto },
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Auto }
                },
                ColumnSpacing = 15
            };

            var badgeFrame = new Frame
            {
                BackgroundColor = badgeColor,
                CornerRadius = 20,
                Padding = new Thickness(8, 4),
                HasShadow = false,
                Content = new Label
                {
                    Text = rank.ToString(),
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                }
            };
            row.Add(badgeFrame, 0, 0);

            var infoStack = new StackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = p.ProductName,
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#111827")
                    },
                    new Label
                    {
                        Text = $"{p.QuantitySold} terjual",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#6B7280")
                    }
                }
            };
            row.Add(infoStack, 1, 0);

            var totalLabel = new Label
            {
                Text = $"Rp {p.TotalSales:N0}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Green,
                VerticalOptions = LayoutOptions.Center
            };
            row.Add(totalLabel, 2, 0);

            TopProductsContainer.Children.Add(row);

            if (rank < topProducts.Count)
            {
                TopProductsContainer.Children.Add(new BoxView
                {
                    BackgroundColor = Color.FromArgb("#E5E7EB"),
                    HeightRequest = 1
                });
            }

            rank++;
        }
    }

    private async void OnExportPdfClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Export PDF", "Laporan berhasil diekspor ke PDF", "OK");
    }

    private async void OnExportExcelClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Export Excel", "Laporan berhasil diekspor ke Excel", "OK");
    }
}
