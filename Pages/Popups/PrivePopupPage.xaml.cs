namespace StoreProgram.Pages.Popups;

public partial class PrivePopupPage : ContentPage
{
    public record PriveResult(DateTime Date, string Description, decimal Amount);

    private readonly TaskCompletionSource<PriveResult?> _tcs = new();

    public PrivePopupPage()
    {
        InitializeComponent();
        DatePicker.Date = DateTime.Today;
        DescriptionEntry.Text = "Prive pemilik";
    }

    public Task<PriveResult?> Result => _tcs.Task;

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
            description = "Prive pemilik";

        if (!decimal.TryParse(AmountEntry.Text?.Trim(), out var amount) || amount <= 0)
        {
            ShowError("Jumlah prive tidak valid.");
            return;
        }

        var date = DatePicker.Date.Date;

        _tcs.TrySetResult(new PriveResult(date, description, amount));
        await Navigation.PopModalAsync(animated: false);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<PriveResult?> ShowAsync()
    {
        var page = new PrivePopupPage();
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
