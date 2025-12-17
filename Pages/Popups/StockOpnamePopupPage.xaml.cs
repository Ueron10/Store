namespace StoreProgram.Pages.Popups;

public partial class StockOpnamePopupPage : ContentPage
{
    private readonly TaskCompletionSource<int?> _tcs = new();

    public StockOpnamePopupPage(string productName, string unit, int systemQty)
    {
        InitializeComponent();

        SubtitleLabel.Text = $"Produk: {productName}";
        SystemQtyLabel.Text = $"Stok Sistem: {systemQty} {unit}";
        PhysicalLabel.Text = $"Stok Fisik ({unit})";
        PhysicalEntry.Text = systemQty.ToString();
    }

    public Task<int?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (!int.TryParse(PhysicalEntry.Text?.Trim(), out var physicalQty) || physicalQty < 0)
        {
            ShowError("Stok fisik tidak valid.");
            return;
        }

        _tcs.TrySetResult(physicalQty);
        await Navigation.PopModalAsync(animated: false);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<int?> ShowAsync(string productName, string unit, int systemQty)
    {
        var page = new StockOpnamePopupPage(productName, unit, systemQty);
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
