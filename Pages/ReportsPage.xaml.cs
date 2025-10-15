namespace StoreProgram.Pages;

public partial class ReportsPage : ContentPage
{
    public ReportsPage()
    {
        InitializeComponent();
    }

    private void OnDailyReportClicked(object sender, EventArgs e)
    {
        SetActiveButton(DailyButton);
        LoadDailyData();
    }

    private void OnWeeklyReportClicked(object sender, EventArgs e)
    {
        SetActiveButton(WeeklyButton);
        LoadWeeklyData();
    }

    private void OnMonthlyReportClicked(object sender, EventArgs e)
    {
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
        TotalSalesLabel.Text = "Rp 2.450.000";
        TotalExpensesLabel.Text = "Rp 1.600.000";
        NetProfitLabel.Text = "Rp 850.000";
        TotalTransactionsLabel.Text = "47 Transaksi";
    }

    private void LoadWeeklyData()
    {
        TotalSalesLabel.Text = "Rp 15.280.000";
        TotalExpensesLabel.Text = "Rp 9.800.000";
        NetProfitLabel.Text = "Rp 5.480.000";
        TotalTransactionsLabel.Text = "298 Transaksi";
    }

    private void LoadMonthlyData()
    {
        TotalSalesLabel.Text = "Rp 68.450.000";
        TotalExpensesLabel.Text = "Rp 42.300.000";
        NetProfitLabel.Text = "Rp 26.150.000";
        TotalTransactionsLabel.Text = "1.247 Transaksi";
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