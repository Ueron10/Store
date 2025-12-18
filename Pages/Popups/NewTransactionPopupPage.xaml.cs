using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages.Popups;

public partial class NewTransactionPopupPage : ContentPage
{
    private readonly TaskCompletionSource<SaleTransaction?> _tcs = new();
    private readonly List<Product> _products;
    private readonly List<StockBatch> _batchesForSelectedProduct = new();
    private INavigation? _hostNav;
    private bool _closing;

    private Product? _selectedProduct;

    public NewTransactionPopupPage(IEnumerable<Product> products)
    {
        InitializeComponent();

        _products = products.OrderBy(p => p.Name).ToList();

        // default state
        _selectedProduct = null;
        SelectedProductEntry.Text = string.Empty;
        BatchPicker.ItemsSource = new List<string> { "Otomatis (FIFO)" };
        BatchPicker.SelectedIndex = 0;

        UpdateTotal();
    }

    public Task<SaleTransaction?> Result => _tcs.Task;

    private Product? SelectedProduct => _selectedProduct;

    private async void OnSelectProductClicked(object sender, EventArgs e)
    {
        var p = await ProductSearchPopupPage.ShowAsync(_products);
        if (p == null)
            return;

        _selectedProduct = p;
        SelectedProductEntry.Text = $"{p.Name} - Rp {p.SellPrice:N0}";

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
        {
            BatchPicker.ItemsSource = new List<string> { "Otomatis (FIFO)" };
            BatchPicker.SelectedIndex = 0;
            return;
        }

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

    private async Task SafeCloseAsync(SaleTransaction? result)
    {
        if (_closing)
            return;

        _closing = true;
        _tcs.TrySetResult(result);

        try
        {
            var nav = _hostNav ?? Navigation;
            await nav.PopModalAsync(animated: false);
        }
        catch
        {
            // ignore
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
        => await SafeCloseAsync(result: null);

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
            // Cegah double submit + beri feedback sederhana
            if (ConfirmButton != null)
            {
                ConfirmButton.IsEnabled = false;
                ConfirmButton.Text = "Memproses...";
            }

            var sale = DataStore.ProcessSingleItemSale(product.Id, qty, paymentMethod, GetSelectedBatchIdOrNull());

            // Tutup popup transaksi langsung setelah proses (biar tidak stuck).
            await SafeCloseAsync(sale);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            if (ConfirmButton != null)
            {
                ConfirmButton.IsEnabled = true;
                ConfirmButton.Text = "Konfirmasi";
            }
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

        var hostPage = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        var nav = hostPage?.Navigation;
        if (nav == null) return null;

        page._hostNav = nav;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
