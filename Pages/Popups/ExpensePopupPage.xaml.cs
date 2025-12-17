namespace StoreProgram.Pages.Popups;

public partial class ExpensePopupPage : ContentPage
{
    public record ExpenseResult(DateTime Date, string Description, decimal Amount);

    private readonly TaskCompletionSource<ExpenseResult?> _tcs = new();

    public ExpensePopupPage()
    {
        InitializeComponent();
        DatePicker.Date = DateTime.Today;
    }

    public Task<ExpenseResult?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var description = DescriptionEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description))
        {
            ShowError("Deskripsi pengeluaran harus diisi.");
            return;
        }

        if (!decimal.TryParse(AmountEntry.Text?.Trim(), out var amount) || amount <= 0)
        {
            ShowError("Jumlah pengeluaran tidak valid.");
            return;
        }

        var date = DatePicker.Date.Date;

        _tcs.TrySetResult(new ExpenseResult(date, description, amount));
        await Navigation.PopModalAsync(animated: false);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<ExpenseResult?> ShowAsync()
    {
        var page = new ExpensePopupPage();
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
