using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class FinancialPage : ContentPage
{
    private List<Product> _products = new();

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

    private void OnNewTransactionClicked(object sender, EventArgs e)
    {
        // Refresh picker supaya produk baru langsung muncul
        LoadProducts();

        TransactionFormFrame.IsVisible = true;
        UpdateTotal();
    }

    private void OnCloseTransactionFormClicked(object sender, EventArgs e)
    {
        TransactionFormFrame.IsVisible = false;
        // Reset form
        ProductPicker.SelectedIndex = -1;
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
                decimal unitPrice = product.SellPrice;

                if (product.DiscountPercent is > 0 and <= 100)
                {
                    unitPrice = unitPrice * (100 - product.DiscountPercent.Value) / 100m;
                }

                var total = unitPrice * quantity;
                TotalLabel.Text = $"Rp {total:N0}";
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

    private void OnProductSelectionChanged(object? sender, EventArgs e) => UpdateTotal();

    private void OnQuantityTextChanged(object? sender, TextChangedEventArgs e) => UpdateTotal();

    private async void OnProcessTransactionClicked(object sender, EventArgs e)
    {
        if (ProductPicker.SelectedIndex < 0 || ProductPicker.SelectedIndex >= _products.Count)
        {
            await DisplayAlert("Error", "Pilih produk terlebih dahulu!", "OK");
            return;
        }

        if (!int.TryParse(QuantityEntry.Text, out int quantity) || quantity <= 0)
        {
            await DisplayAlert("Error", "Masukkan jumlah yang valid!", "OK");
            return;
        }

        var product = _products[ProductPicker.SelectedIndex];

        string paymentMethod = CashRadio.IsChecked ? "Tunai" : "QRIS";
        string productName = product.Name;
        
        bool confirm = await DisplayAlert("Konfirmasi Transaksi", 
            $"Produk: {productName}\nJumlah: {quantity}\nTotal: {TotalLabel.Text}\nPembayaran: {paymentMethod}\n\nProses transaksi?", 
            "Ya", "Batal");

        if (confirm)
        {
            try
            {
                var sale = DataStore.ProcessSingleItemSale(product.Id, quantity, paymentMethod);

                // Simulasi integrasi QRIS: tampilkan info tambahan jika metode QRIS
                if (paymentMethod == "QRIS")
                {
                    await DisplayAlert("QRIS", "Tampilkan kode QR ke pelanggan (simulasi).", "OK");
                }

                await DisplayAlert("Sukses", $"Transaksi berhasil diproses! Total: Rp {sale.GrossAmount:N0}", "OK");
                OnCloseTransactionFormClicked(sender, e);
                UpdateSummary();
                BuildRecentTransactions();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    private async void OnAddExpenseClicked(object sender, EventArgs e)
    {
        string description = await DisplayPromptAsync("Input Pengeluaran", "Deskripsi pengeluaran:");
        if (!string.IsNullOrWhiteSpace(description))
        {
            string amountText = await DisplayPromptAsync("Input Pengeluaran", "Jumlah pengeluaran (Rp):", keyboard: Keyboard.Numeric);
            if (decimal.TryParse(amountText, out var amount) && amount > 0)
            {
                DataStore.AddOperationalExpense(description, amount);
                await DisplayAlert("Sukses", $"Pengeluaran '{description}' sebesar Rp {amount:N0} berhasil dicatat!", "OK");
                UpdateSummary();
                BuildRecentTransactions();
            }
        }
    }

    private async void OnPriveClicked(object sender, EventArgs e)
    {
        string description = await DisplayPromptAsync("Input Prive", "Keterangan pengambilan (opsional):", initialValue: "Prive pemilik");
        string amountText = await DisplayPromptAsync("Input Prive", "Jumlah yang diambil (Rp):", keyboard: Keyboard.Numeric);

        if (decimal.TryParse(amountText, out var amount) && amount > 0)
        {
            if (string.IsNullOrWhiteSpace(description))
                description = "Prive pemilik";

            DataStore.AddPrive(description, amount);
            await DisplayAlert("Sukses", $"Prive '{description}' sebesar Rp {amount:N0} berhasil dicatat!\n(Pengeluaran ini tidak dihitung sebagai biaya operasional.)", "OK");
            UpdateSummary();
            BuildRecentTransactions();
        }
    }

    private async void OnViewReceiptClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Struk Transaksi", "Fitur struk (detail item) akan ditampilkan dari daftar transaksi terbaru.", "OK");
    }

    private async void OnEditExpenseClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Edit Pengeluaran", "Fitur edit pengeluaran belum diimplementasikan.", "OK");
    }

    private void UpdateSummary()
    {
        var today = DateTime.Today;
        var range = new DateRange(today, today.AddDays(1).AddTicks(-1));
        var summary = DataStore.GetSummary(range);

        DailyIncomeLabel.Text = $"Rp {summary.TotalSales:N0}";
        var totalExpenses = summary.OperationalExpenses + summary.DamagedLostExpenses;
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
