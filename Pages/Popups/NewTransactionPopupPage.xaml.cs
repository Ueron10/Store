using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages.Popups;

public partial class NewTransactionPopupPage : ContentPage
{
    private readonly TaskCompletionSource<SaleTransaction?> _tcs = new();
    private readonly List<Product> _products;
    private readonly List<StockBatch> _batchesForSelectedProduct = new();

    public NewTransactionPopupPage(IEnumerable<Product> products)
    {
        InitializeComponent();

        _products = products.OrderBy(p => p.Name).ToList();
        ProductPicker.ItemsSource = _products.Select(p => $"{p.Name} - Rp {p.SellPrice:N0}").ToList();

        UpdateTotal();
    }

    public Task<SaleTransaction?> Result => _tcs.Task;

    private Product? SelectedProduct =>
        ProductPicker.SelectedIndex >= 0 && ProductPicker.SelectedIndex < _products.Count
            ? _products[ProductPicker.SelectedIndex]
            : null;

    private void OnProductChanged(object? sender, EventArgs e)
    {
        LoadBatches();
        UpdateTotal();
    }

    private void OnBatchChanged(object? sender, EventArgs e) => UpdateTotal();

    private void OnQtyChanged(object? sender, TextChangedEventArgs e) => UpdateTotal();

    private void OnDecreaseQty(object sender, EventArgs e)
    {
        if (int.TryParse(QtyEntry.Text, out var q) && q > 1)
            QtyEntry.Text = (q - 1).ToString();
    }

    private void OnIncreaseQty(object sender, EventArgs e)
    {
        if (int.TryParse(QtyEntry.Text, out var q))
            QtyEntry.Text = (q + 1).ToString();
    }

    private void LoadBatches()
    {
        BatchPicker.ItemsSource = null;
        _batchesForSelectedProduct.Clear();

        var product = SelectedProduct;
        if (product == null)
            return;

        var labels = new List<string> { "Otomatis (FIFO)" };

        var batches = DataStore.StockBatches
            .Where(b => b.ProductId == product.Id && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.PurchaseDate)
            .ToList();

        foreach (var b in batches)
        {
            var disc = b.DiscountPercent is > 0 and <= 100 ? b.DiscountPercent.Value : 0m;
            labels.Add($"Exp {b.ExpiryDate:dd MMM yyyy} • Masuk {b.PurchaseDate:dd MMM} • Stok {b.Quantity} • Diskon {disc:0}%");
            _batchesForSelectedProduct.Add(b);
        }

        BatchPicker.ItemsSource = labels;
        BatchPicker.SelectedIndex = 0;
    }

    private Guid? GetSelectedBatchIdOrNull()
    {
        // index 0 = otomatis FIFO
        if (BatchPicker.SelectedIndex <= 0)
            return null;

        int idx = BatchPicker.SelectedIndex - 1;
        if (idx < 0 || idx >= _batchesForSelectedProduct.Count)
            return null;

        return _batchesForSelectedProduct[idx].Id;
    }

    private void UpdateTotal()
    {
        try
        {
            var product = SelectedProduct;
            if (product == null)
            {
                TotalLabel.Text = "Rp 0";
                return;
            }

            if (!int.TryParse(QtyEntry.Text, out var qty) || qty <= 0)
            {
                TotalLabel.Text = "Rp 0";
                return;
            }

            var preview = DataStore.PreviewSale(product.Id, qty, GetSelectedBatchIdOrNull());
            TotalLabel.Text = $"Rp {preview.TotalAmount:N0}";
        }
        catch
        {
            TotalLabel.Text = "Stok tidak cukup";
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var product = SelectedProduct;
        if (product == null)
        {
            ShowError("Pilih produk terlebih dahulu.");
            return;
        }

        if (!int.TryParse(QtyEntry.Text, out var qty) || qty <= 0)
        {
            ShowError("Masukkan jumlah yang valid.");
            return;
        }

        string paymentMethod = CashRadio.IsChecked ? "Tunai" : "QRIS";

        // Konfirmasi akhir (popup custom)
        bool ok = await ConfirmPopupPage.ShowAsync(
            title: "Konfirmasi Transaksi",
            message: $"Produk: {product.Name}\nJumlah: {qty}\nTotal: {TotalLabel.Text}\nPembayaran: {paymentMethod}\n\nProses transaksi?",
            confirmText: "Proses",
            cancelText: "Batal");

        if (!ok) return;

        try
        {
            var sale = DataStore.ProcessSingleItemSale(product.Id, qty, paymentMethod, GetSelectedBatchIdOrNull());

            if (paymentMethod == "QRIS")
            {
                await InfoPopupPage.ShowAsync("QRIS", "Tampilkan kode QR ke pelanggan (simulasi).", okText: "OK");
            }

            _tcs.TrySetResult(sale);
            await Navigation.PopModalAsync(animated: false);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    public static async Task<SaleTransaction?> ShowAsync(IEnumerable<Product> products)
    {
        var page = new NewTransactionPopupPage(products);
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
