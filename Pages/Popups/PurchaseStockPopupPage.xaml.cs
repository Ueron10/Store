namespace StoreProgram.Pages.Popups;

public partial class PurchaseStockPopupPage : ContentPage
{
    public record PurchaseStockResult(int Qty, decimal UnitCost, DateOnly ExpiryDate);

    private readonly TaskCompletionSource<PurchaseStockResult?> _tcs = new();

    public PurchaseStockPopupPage(string productName, string unit, decimal defaultUnitCost)
    {
        InitializeComponent();

        TitleLabel.Text = "Barang Masuk (Pembelian)";
        SubtitleLabel.Text = $"Produk: {productName}";
        QtyLabel.Text = $"Jumlah ({unit})";

        QtyEntry.Text = string.Empty;
        UnitCostEntry.Text = defaultUnitCost.ToString("0.##");
        ExpiryDatePicker.Date = DateTime.Today.AddMonths(6);
    }

    public Task<PurchaseStockResult?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (!int.TryParse(QtyEntry.Text?.Trim(), out var qty) || qty <= 0)
        {
            ShowError("Jumlah tidak valid.");
            return;
        }

        if (!decimal.TryParse(UnitCostEntry.Text?.Trim(), out var unitCost) || unitCost <= 0)
        {
            ShowError("Harga modal tidak valid.");
            return;
        }

        var expiry = DateOnly.FromDateTime(ExpiryDatePicker.Date.Date);

        _tcs.TrySetResult(new PurchaseStockResult(qty, unitCost, expiry));
        await Navigation.PopModalAsync(animated: false);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<PurchaseStockResult?> ShowAsync(string productName, string unit, decimal defaultUnitCost)
    {
        var page = new PurchaseStockPopupPage(productName, unit, defaultUnitCost);
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
