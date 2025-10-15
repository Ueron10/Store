namespace StoreProgram.Pages;

public partial class EmployeeDashboardPage : ContentPage
{
    public EmployeeDashboardPage()
    {
        InitializeComponent();
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        // Set current date
        DateLabel.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
        
        // In a real app, you would load actual employee-specific data from database here
    }

    private async void OnNewTransactionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//financial");
    }

    private async void OnViewStockClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//stock");
    }

    private async void OnUpdateStockClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//stock");
    }

    private async void OnNotificationsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//notifications");
    }
}