namespace StoreProgram.Pages.Popups;

public partial class StockAdjustmentPopupPage : ContentPage
{
    public record StockAdjustmentResult(string Reason, int Qty);

    private readonly TaskCompletionSource<StockAdjustmentResult?> _tcs = new();

    public StockAdjustmentPopupPage(string productName, string unit)
    {
        InitializeComponent();

        SubtitleLabel.Text = $"Produk: {productName}";
        QtyLabel.Text = $"Jumlah keluar ({unit})";

        ReasonPicker.ItemsSource = new List<string> { "Kadaluarsa", "Rusak", "Hilang" };
        ReasonPicker.SelectedItem = "Rusak";
    }

    public Task<StockAdjustmentResult?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var reason = (ReasonPicker.SelectedItem as string) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            ShowError("Alasan harus dipilih.");
            return;
        }

        if (!int.TryParse(QtyEntry.Text?.Trim(), out var qty) || qty <= 0)
        {
            ShowError("Jumlah tidak valid.");
            return;
        }

        _tcs.TrySetResult(new StockAdjustmentResult(reason, qty));
        await Navigation.PopModalAsync(animated: false);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<StockAdjustmentResult?> ShowAsync(string productName, string unit)
    {
        var page = new StockAdjustmentPopupPage(productName, unit);
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
