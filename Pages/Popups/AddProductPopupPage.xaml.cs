using StoreProgram.Models;

namespace StoreProgram.Pages.Popups;

public partial class AddProductPopupPage : ContentPage
{
    public record AddProductResult(Product Product, int InitialStock, DateOnly InitialExpiry);

    private readonly TaskCompletionSource<AddProductResult?> _tcs = new();

    public AddProductPopupPage()
    {
        InitializeComponent();

        CategoryEntry.Text = "makanan";
        UnitEntry.Text = "pcs";
        InitialStockEntry.Text = "0";
        ExpiryDatePicker.Date = DateTime.Today.AddMonths(6);
    }

    public Task<AddProductResult?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        string name = NameEntry.Text?.Trim() ?? string.Empty;
        string category = CategoryEntry.Text?.Trim() ?? string.Empty;
        string unit = UnitEntry.Text?.Trim() ?? string.Empty;
        string? barcode = BarcodeEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(unit))
        {
            ShowError("Nama/Kategori/Satuan wajib diisi.");
            return;
        }

        if (!decimal.TryParse(SellPriceEntry.Text?.Trim(), out var sellPrice) || sellPrice <= 0)
        {
            ShowError("Harga jual tidak valid.");
            return;
        }

        if (!decimal.TryParse(CostPriceEntry.Text?.Trim(), out var costPrice) || costPrice <= 0)
        {
            ShowError("Harga modal tidak valid.");
            return;
        }

        if (!int.TryParse(InitialStockEntry.Text?.Trim(), out var initialStock) || initialStock < 0)
        {
            ShowError("Stok awal tidak valid.");
            return;
        }

        var product = new Product
        {
            Name = name,
            Category = category,
            Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
            Unit = unit,
            SellPrice = sellPrice,
            CostPrice = costPrice
        };

        var expiry = DateOnly.FromDateTime(ExpiryDatePicker.Date.Date);

        _tcs.TrySetResult(new AddProductResult(product, initialStock, expiry));
        await Navigation.PopModalAsync(animated: false);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<AddProductResult?> ShowAsync()
    {
        var page = new AddProductPopupPage();
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
