using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using StoreProgram.Models;
using StoreProgram.Pages.Popups;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class FinancialPage : ContentPage
{
    private List<Product> _products = new();
    private List<StockBatch> _batchesForSelectedProduct = new();

    public FinancialPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Auto refresh setiap halaman muncul (mis. setelah tambah produk / transaksi)
        LoadProducts();
        UpdateSummary();
        BuildRecentTransactions();
    }

    private void LoadProducts()
    {
        _products = DataStore.Products.ToList();

        ProductPicker.Items.Clear();
        foreach (var product in _products)
        {
            ProductPicker.Items.Add($"{product.Name} - Rp {product.SellPrice:N0}");
        }
    }

    private async void OnNewTransactionClicked(object sender, EventArgs e)
    {
        // Refresh data supaya produk baru langsung muncul
        LoadProducts();

        if (!_products.Any())
        {
            await InfoPopupPage.ShowAsync("Transaksi", "Belum ada produk. Tambahkan produk terlebih dahulu.");
            return;
        }

        var sale = await NewTransactionPopupPage.ShowAsync(_products);
        if (sale == null) return;

        await InfoPopupPage.ShowAsync("Sukses", $"Transaksi berhasil diproses! Total: Rp {sale.GrossAmount:N0}");
        UpdateSummary();
        BuildRecentTransactions();
    }

    private void OnCloseTransactionFormClicked(object sender, EventArgs e)
    {
        TransactionFormFrame.IsVisible = false;
        // Reset form
        ProductPicker.SelectedIndex = -1;
        BatchPicker.SelectedIndex = -1;
        BatchPicker.Items.Clear();
        _batchesForSelectedProduct.Clear();
        QuantityEntry.Text = "1";
        CashRadio.IsChecked = true;
    }

    private void OnDecreaseQuantityClicked(object sender, EventArgs e)
    {
        if (int.TryParse(QuantityEntry.Text, out int quantity) && quantity > 1)
        {
            QuantityEntry.Text = (quantity - 1).ToString();
            UpdateTotal();
        }
    }

    private void OnIncreaseQuantityClicked(object sender, EventArgs e)
    {
        if (int.TryParse(QuantityEntry.Text, out int quantity))
        {
            QuantityEntry.Text = (quantity + 1).ToString();
            UpdateTotal();
        }
    }

    private void UpdateTotal()
    {
        if (ProductPicker.SelectedIndex >= 0 && int.TryParse(QuantityEntry.Text, out int quantity))
        {
            if (ProductPicker.SelectedIndex >= 0 && ProductPicker.SelectedIndex < _products.Count)
            {
                var product = _products[ProductPicker.SelectedIndex];

                try
                {
                    Guid? batchId = GetSelectedBatchIdOrNull();
                    var preview = DataStore.PreviewSale(product.Id, quantity, batchId);
                    TotalLabel.Text = $"Rp {preview.TotalAmount:N0}";
                }
                catch
                {
                    TotalLabel.Text = "Stok tidak cukup";
                }
            }
            else
            {
                TotalLabel.Text = "Rp 0";
            }
        }
        else
        {
            TotalLabel.Text = "Rp 0";
        }
    }

    private void OnProductSelectionChanged(object? sender, EventArgs e)
    {
        LoadBatchesForSelectedProduct();
        UpdateTotal();
    }

    private void OnBatchSelectionChanged(object? sender, EventArgs e) => UpdateTotal();

    private void LoadBatchesForSelectedProduct()
    {
        BatchPicker.Items.Clear();
        _batchesForSelectedProduct.Clear();

        if (ProductPicker.SelectedIndex < 0 || ProductPicker.SelectedIndex >= _products.Count)
            return;

        var product = _products[ProductPicker.SelectedIndex];

        // Item 0 = Otomatis FIFO
        BatchPicker.Items.Add("Otomatis (FIFO)");

        var batches = DataStore.StockBatches
            .Where(b => b.ProductId == product.Id && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.PurchaseDate)
            .ToList();

        foreach (var b in batches)
        {
            var disc = b.DiscountPercent is > 0 and <= 100 ? b.DiscountPercent.Value : 0m;
            var label = $"Exp {b.ExpiryDate:dd MMM yyyy} • Masuk {b.PurchaseDate:dd MMM} • Stok {b.Quantity} • Diskon {disc:0}%";
            BatchPicker.Items.Add(label);
            _batchesForSelectedProduct.Add(b);
        }

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

    private void OnQuantityTextChanged(object? sender, TextChangedEventArgs e) => UpdateTotal();

    private async void OnProcessTransactionClicked(object sender, EventArgs e)
    {
        if (ProductPicker.SelectedIndex < 0 || ProductPicker.SelectedIndex >= _products.Count)
        {
            await InfoPopupPage.ShowAsync("Error", "Pilih produk terlebih dahulu!");
            return;
        }

        if (!int.TryParse(QuantityEntry.Text, out int quantity) || quantity <= 0)
        {
            await InfoPopupPage.ShowAsync("Error", "Masukkan jumlah yang valid!");
            return;
        }

        var product = _products[ProductPicker.SelectedIndex];

        string paymentMethod = CashRadio.IsChecked ? "Tunai" : "QRIS";
        string productName = product.Name;
        
        bool confirm = await ConfirmPopupPage.ShowAsync(
            title: "Konfirmasi Transaksi",
            message: $"Produk: {productName}\nJumlah: {quantity}\nTotal: {TotalLabel.Text}\nPembayaran: {paymentMethod}\n\nProses transaksi?",
            confirmText: "Proses",
            cancelText: "Batal");

        if (confirm)
        {
            try
            {
                var sale = DataStore.ProcessSingleItemSale(product.Id, quantity, paymentMethod, GetSelectedBatchIdOrNull());

                // Simulasi integrasi QRIS: tampilkan info tambahan jika metode QRIS
                if (paymentMethod == "QRIS")
                {
                    await InfoPopupPage.ShowAsync("QRIS", "Tampilkan kode QR ke pelanggan (simulasi).");
                }

                await InfoPopupPage.ShowAsync("Sukses", $"Transaksi berhasil diproses! Total: Rp {sale.GrossAmount:N0}");
                OnCloseTransactionFormClicked(sender, e);
                UpdateSummary();
                BuildRecentTransactions();
            }
            catch (Exception ex)
            {
                await InfoPopupPage.ShowAsync("Error", ex.Message);
            }
        }
    }

    private async void OnAddExpenseClicked(object sender, EventArgs e)
    {
        var result = await ExpensePopupPage.ShowAsync();
        if (result == null) return;

        DataStore.AddOperationalExpense(result.Description, result.Amount, result.Date);
        await InfoPopupPage.ShowAsync("Sukses", $"Pengeluaran '{result.Description}' sebesar Rp {result.Amount:N0} berhasil dicatat!");

        UpdateSummary();
        BuildRecentTransactions();
    }

    private void OnCancelExpenseClicked(object sender, EventArgs e)
    {
        ExpenseFormFrame.IsVisible = false;
    }

    private async void OnSaveExpenseClicked(object sender, EventArgs e)
    {
        string description = ExpenseDescriptionEntry.Text?.Trim() ?? string.Empty;
        string amountText = ExpenseAmountEntry.Text?.Trim() ?? string.Empty;
        var date = ExpenseDatePicker.Date.Date;

        if (string.IsNullOrWhiteSpace(description))
        {
            await InfoPopupPage.ShowAsync("Error", "Deskripsi pengeluaran harus diisi.");
            return;
        }

        if (!decimal.TryParse(amountText, out var amount) || amount <= 0)
        {
            await InfoPopupPage.ShowAsync("Error", "Jumlah pengeluaran tidak valid.");
            return;
        }

        DataStore.AddOperationalExpense(description, amount, date);
        await InfoPopupPage.ShowAsync("Sukses", $"Pengeluaran '{description}' sebesar Rp {amount:N0} berhasil dicatat!");

        ExpenseFormFrame.IsVisible = false;
        UpdateSummary();
        BuildRecentTransactions();
    }

    private async void OnPriveClicked(object sender, EventArgs e)
    {
        var result = await PrivePopupPage.ShowAsync();
        if (result == null) return;

        DataStore.AddPrive(result.Description, result.Amount, result.Date);
        await InfoPopupPage.ShowAsync("Sukses", $"Prive '{result.Description}' sebesar Rp {result.Amount:N0} berhasil dicatat!");

        UpdateSummary();
        BuildRecentTransactions();
    }

    private void OnCancelPriveClicked(object sender, EventArgs e)
    {
        PriveFormFrame.IsVisible = false;
    }

    private async void OnSavePriveClicked(object sender, EventArgs e)
    {
        string description = PriveDescriptionEntry.Text?.Trim() ?? string.Empty;
        string amountText = PriveAmountEntry.Text?.Trim() ?? string.Empty;
        var date = PriveDatePicker.Date.Date;

        if (string.IsNullOrWhiteSpace(description))
            description = "Prive pemilik";

        if (!decimal.TryParse(amountText, out var amount) || amount <= 0)
        {
            await InfoPopupPage.ShowAsync("Error", "Jumlah prive tidak valid.");
            return;
        }

        DataStore.AddPrive(description, amount, date);
        await InfoPopupPage.ShowAsync("Sukses", $"Prive '{description}' sebesar Rp {amount:N0} berhasil dicatat!");

        PriveFormFrame.IsVisible = false;
        UpdateSummary();
        BuildRecentTransactions();
    }

    private async void OnViewReceiptClicked(object sender, EventArgs e)
    {
        await InfoPopupPage.ShowAsync("Struk Transaksi", "Fitur struk (detail item) akan ditampilkan dari daftar transaksi terbaru.");
    }

    private async void OnEditExpenseClicked(object sender, EventArgs e)
    {
        await InfoPopupPage.ShowAsync("Edit Pengeluaran", "Fitur edit pengeluaran belum diimplementasikan.");
    }

    private async void OnExportFinancialPdfClicked(object sender, EventArgs e)
    {
        try
        {
            var today = DateTime.Today;
            var range = new DateRange(today, today.AddDays(1).AddTicks(-1));

            var filePath = await ReportExportService.ExportPdfAsync(range, "Keuangan_HariIni");
            await TryOpenFileAsync(filePath);
            await InfoPopupPage.ShowAsync("Export PDF", $"PDF keuangan tersimpan:\n{filePath}");
        }
        catch (Exception ex)
        {
            await InfoPopupPage.ShowAsync("Export PDF", $"Gagal export PDF keuangan: {ex.Message}");
        }
    }

    private async void OnExportFinancialExcelClicked(object sender, EventArgs e)
    {
        try
        {
            var today = DateTime.Today;
            var range = new DateRange(today, today.AddDays(1).AddTicks(-1));

            var filePath = await ReportExportService.ExportExcelAsync(range, "Keuangan_HariIni");
            await TryOpenFileAsync(filePath);
            await InfoPopupPage.ShowAsync("Export Excel", $"Excel keuangan tersimpan:\n{filePath}");
        }
        catch (Exception ex)
        {
            await InfoPopupPage.ShowAsync("Export Excel", $"Gagal export Excel keuangan: {ex.Message}");
        }
    }

    private static async Task TryOpenFileAsync(string path)
    {
        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(path)
            });
        }
        catch
        {
            // Ignore: sebagian platform tidak support open langsung.
        }
    }

    private void UpdateSummary()
    {
        var today = DateTime.Today;
        var range = new DateRange(today, today.AddDays(1).AddTicks(-1));
        var summary = DataStore.GetSummary(range);

        DailyIncomeLabel.Text = $"Rp {summary.TotalSales:N0}";
        var totalExpenses = summary.OperationalExpenses + summary.DamagedLostExpenses + summary.DiscountLossExpenses;
        DailyExpenseLabel.Text = $"Rp {totalExpenses:N0}";
        DailyProfitLabel.Text = $"Rp {summary.NetProfit:N0}";
    }

    private void BuildRecentTransactions()
    {
        RecentTransactionsContainer.Children.Clear();

        var today = DateTime.Today;
        var todaySales = DataStore.Sales
            .Where(s => s.Timestamp.Date == today)
            .OrderByDescending(s => s.Timestamp)
            .Take(10)
            .ToList();

        if (!todaySales.Any())
        {
            RecentTransactionsContainer.Children.Add(new Label
            {
                Text = "Belum ada data transaksi untuk ditampilkan.",
                FontSize = 12,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        foreach (var sale in todaySales)
        {
            string timeText = sale.Timestamp.ToString("HH:mm");

            string itemsText;
            if (sale.Items != null && sale.Items.Count > 0)
            {
                if (sale.Items.Count == 1)
                {
                    var item = sale.Items[0];
                    itemsText = $"{item.ProductName} x{item.Quantity}";
                }
                else
                {
                    var first = sale.Items[0];
                    itemsText = $"{first.ProductName} dan {sale.Items.Count - 1} item lain";
                }
            }
            else
            {
                itemsText = "Transaksi";
            }

            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F3F4F6"),
                CornerRadius = 10,
                Padding = 12,
                HasShadow = false
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Auto }
                },
                ColumnSpacing = 10
            };

            var left = new StackLayout();
            left.Children.Add(new Label
            {
                Text = itemsText,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#111827")
            });
            left.Children.Add(new Label
            {
                Text = $"{sale.PaymentMethod} • {timeText}",
                FontSize = 12,
                TextColor = Color.FromArgb("#6B7280")
            });

            grid.Add(left, 0, 0);

            grid.Add(new Label
            {
                Text = $"Rp {sale.GrossAmount:N0}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Green,
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            frame.Content = grid;
            RecentTransactionsContainer.Children.Add(frame);
        }
    }
}
