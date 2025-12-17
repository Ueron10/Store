using StoreProgram.Models;

namespace StoreProgram.Pages.Popups;

public partial class DiscountPopupPage : ContentPage
{
    private readonly TaskCompletionSource<decimal?> _tcs = new();
    private readonly Product _product;
    private readonly StockBatch _batch;

    public DiscountPopupPage(Product product, StockBatch batch)
    {
        InitializeComponent();

        _product = product;
        _batch = batch;

        var now = batch.DiscountPercent is > 0 and <= 100 ? batch.DiscountPercent.Value : 0m;
        PercentEntry.Text = now.ToString("0");

        SubtitleLabel.Text = $"{product.Name} • Exp {batch.ExpiryDate:dd MMM yyyy} • Qty {batch.Quantity} {product.Unit}";
        CurrentInfoLabel.Text = $"Diskon saat ini: {now:0}%";
        UpdatePreview(now);

        PercentEntry.TextChanged += (_, __) =>
        {
            if (decimal.TryParse(PercentEntry.Text, out var p))
                UpdatePreview(ClampPercent(p));
        };
    }

    public Task<decimal?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (!decimal.TryParse(PercentEntry.Text, out var percent))
        {
            ShowError("Diskon tidak valid.");
            return;
        }

        percent = ClampPercent(percent);
        _tcs.TrySetResult(percent);
        await Navigation.PopModalAsync(animated: false);
    }

    private void UpdatePreview(decimal percent)
    {
        decimal discountedPrice = _product.SellPrice * (100 - percent) / 100m;
        PricePreviewLabel.Text = $"Harga: Rp {_product.SellPrice:N0} → Rp {discountedPrice:N0}";
    }

    private static decimal ClampPercent(decimal percent)
    {
        if (percent < 0) return 0;
        if (percent > 100) return 100;
        return percent;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<decimal?> ShowAsync(Product product, StockBatch batch)
    {
        var page = new DiscountPopupPage(product, batch);
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
