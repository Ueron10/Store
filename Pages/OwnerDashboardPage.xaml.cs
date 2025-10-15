namespace StoreProgram.Pages;

public partial class OwnerDashboardPage : ContentPage
{
    public OwnerDashboardPage()
    {
        InitializeComponent();
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        // Set current date
        DateLabel.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
        
        // In a real app, you would load actual data from database here
        // For now, the mock data is in the XAML
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Settings", "Pengaturan aplikasi", "OK");
    }

    private async void OnViewStockAlertsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//notifications");
    }

    private async void OnViewAllTransactionsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//financial");
    }

    private async void OnReportsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//reports");
    }

    private async void OnManageStockClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//stock");
    }

    private async void OnFinancialClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//financial");
    }

    private async void OnNotificationsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//notifications");
    }
}