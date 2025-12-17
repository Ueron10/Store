using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using StoreProgram.Components;
using StoreProgram.Models;
using StoreProgram.Pages.Popups;
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

    private DateRange _currentRange;

    public ReportsPage()
    {
        InitializeComponent();

        var today = DateTime.Today;
        _currentRange = new DateRange(today, today.AddDays(1).AddTicks(-1));

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
        _currentRange = range;

        var summary = DataStore.GetSummary(range);

        TotalSalesLabel.Text = $"Rp {summary.TotalSales:N0}";
        var totalExpenses = summary.OperationalExpenses + summary.DamagedLostExpenses + summary.DiscountLossExpenses;
        TotalExpensesLabel.Text = $"Rp {totalExpenses:N0}";
        NetProfitLabel.Text = $"Rp {summary.NetProfit:N0}";
        TotalTransactionsLabel.Text = $"{summary.TotalTransactions} Transaksi";

        BuildTopProducts(summary.TopProducts);
        BuildSalesChart(range);
    }

    private void BuildSalesChart(DateRange range)
    {
        var salesInRange = DataStore.Sales
            .Where(s => s.Timestamp >= range.Start && s.Timestamp <= range.End)
            .OrderBy(s => s.Timestamp)
            .ToList();

        List<ChartPoint> points;
        string subtitle;

        switch (_activePeriod)
        {
            case ReportPeriod.Daily:
            {
                subtitle = $"Harian • {range.Start:dd MMM yyyy}";

                // per jam (00-23)
                var map = salesInRange
                    .GroupBy(s => s.Timestamp.Hour)
                    .ToDictionary(g => g.Key, g => (double)g.Sum(x => x.GrossAmount));

                points = Enumerable.Range(0, 24)
                    .Select(h => new ChartPoint(h.ToString("00"), map.TryGetValue(h, out var v) ? v : 0))
                    .ToList();
                break;
            }
            case ReportPeriod.Weekly:
            {
                subtitle = $"Mingguan • {range.Start:dd MMM} - {range.End:dd MMM yyyy}";
                var days = EachDay(range.Start.Date, range.End.Date);

                var map = salesInRange
                    .GroupBy(s => s.Timestamp.Date)
                    .ToDictionary(g => g.Key, g => (double)g.Sum(x => x.GrossAmount));

                points = days
                    .Select(d => new ChartPoint(d.ToString("dd/MM"), map.TryGetValue(d, out var v) ? v : 0))
                    .ToList();
                break;
            }
            case ReportPeriod.Monthly:
            default:
            {
                subtitle = $"Bulanan • {range.Start:MMMM yyyy}";
                var days = EachDay(range.Start.Date, range.End.Date);

                var map = salesInRange
                    .GroupBy(s => s.Timestamp.Date)
                    .ToDictionary(g => g.Key, g => (double)g.Sum(x => x.GrossAmount));

                points = days
                    .Select(d => new ChartPoint(d.Day.ToString(), map.TryGetValue(d, out var v) ? v : 0))
                    .ToList();
                break;
            }
        }

        ChartSubtitleLabel.Text = subtitle;
        SalesChartView.Drawable = new SalesBarChartDrawable(points);
        SalesChartView.Invalidate();
    }

    private static List<DateTime> EachDay(DateTime start, DateTime end)
    {
        var days = new List<DateTime>();
        for (var dt = start.Date; dt <= end.Date; dt = dt.AddDays(1))
            days.Add(dt);
        return days;
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
                TextColor = Color.FromArgb("#166534"), // SuccessText
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
        try
        {
            var periodName = _activePeriod switch
            {
                ReportPeriod.Daily => "Harian",
                ReportPeriod.Weekly => "Mingguan",
                _ => "Bulanan"
            };

            var filePath = await ReportExportService.ExportPdfAsync(_currentRange, periodName);
            await TryOpenFileAsync(filePath);
            await InfoPopupPage.ShowAsync("Export PDF", $"PDF tersimpan:\n{filePath}");
        }
        catch (Exception ex)
        {
            await InfoPopupPage.ShowAsync("Export PDF", $"Gagal export PDF: {ex.Message}");
        }
    }

    private async void OnExportExcelClicked(object sender, EventArgs e)
    {
        try
        {
            var periodName = _activePeriod switch
            {
                ReportPeriod.Daily => "Harian",
                ReportPeriod.Weekly => "Mingguan",
                _ => "Bulanan"
            };

            var filePath = await ReportExportService.ExportExcelAsync(_currentRange, periodName);
            await TryOpenFileAsync(filePath);
            await InfoPopupPage.ShowAsync("Export Excel", $"Excel tersimpan:\n{filePath}");
        }
        catch (Exception ex)
        {
            await InfoPopupPage.ShowAsync("Export Excel", $"Gagal export Excel: {ex.Message}");
        }
    }

    private static async Task TryOpenFileAsync(string path)
    {
        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(path)
            });
        }
        catch
        {
            // Ignore: sebagian platform tidak support open langsung.
        }
    }
}
