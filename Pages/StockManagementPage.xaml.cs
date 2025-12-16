using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class StockManagementPage : ContentPage
{
    private List<Product> _products = new();

    public StockManagementPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadProductsAndRender();
    }

    private void LoadProductsAndRender(string? filter = null)
    {
        _products = DataStore.Products
            .Where(p => string.IsNullOrWhiteSpace(filter) ||
                        p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        p.Category.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p.Name)
            .ToList();

        StockContainer.Children.Clear();

        foreach (var product in _products)
        {
            var stockQty = DataStore.GetCurrentStock(product.Id);

            // Tentukan status stok (AMAN / MENIPIS / KRITIS)
            string statusText;
            Color statusColor;
            if (stockQty <= 0)
            {
                statusText = "HABIS";
                statusColor = Colors.Red;
            }
            else if (stockQty <= 5)
            {
                statusText = "KRITIS";
                statusColor = Colors.Red;
            }
            else if (stockQty <= 10)
            {
                statusText = "MENIPIS";
                statusColor = Colors.Orange;
            }
            else
            {
                statusText = "AMAN";
                statusColor = Colors.Green;
            }

            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 10,
                Padding = 15,
                HasShadow = true
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition{ Width = 60 },
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Auto },
                    new ColumnDefinition{ Width = GridLength.Auto }
                },
                ColumnSpacing = 15
            };

            // Icon sederhana berdasarkan kategori
            var iconFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#512BD4"),
                CornerRadius = 10,
                Padding = 0,
                HasShadow = false,
                WidthRequest = 60,
                HeightRequest = 60,
                Content = new Label
                {
                    Text = product.Category.Contains("minum", StringComparison.OrdinalIgnoreCase) ? "💧" : "📦",
                    FontSize = 30,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            grid.Add(iconFrame, 0, 0);

            var infoStack = new StackLayout();
            infoStack.Children.Add(new Label
            {
                Text = product.Name,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#111827")
            });
            infoStack.Children.Add(new Label
            {
                Text = $"Rp {product.SellPrice:N0} per {product.Unit}",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            infoStack.Children.Add(new Label
            {
                Text = $"Stok: {stockQty} {product.Unit}",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = statusColor
            });

            grid.Add(infoStack, 1, 0);

            var statusFrame = new Frame
            {
                BackgroundColor = statusColor,
                CornerRadius = 15,
                Padding = new Thickness(8, 4),
                HasShadow = false,
                Content = new Label
                {
                    Text = statusText,
                    FontSize = 10,
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold
                }
            };
            grid.Add(statusFrame, 2, 0);

            var menuButton = new Button
            {
                Text = "⋮",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#6B7280"),
                FontSize = 20,
                BindingContext = product
            };
            menuButton.Clicked += OnProductMenuClicked;
            grid.Add(menuButton, 3, 0);

            frame.Content = grid;
            StockContainer.Children.Add(frame);
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ClearSearchButton.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);
        LoadProductsAndRender(e.NewTextValue);
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        ClearSearchButton.IsVisible = false;
        LoadProductsAndRender();
    }

    private void OnAddProductClicked(object sender, EventArgs e)
    {
        // Tampilkan popup form
        AddNameEntry.Text = string.Empty;
        AddCategoryEntry.Text = "makanan";
        AddBarcodeEntry.Text = string.Empty;
        AddUnitEntry.Text = "pcs";
        AddInitialStockEntry.Text = "0";
        AddSellPriceEntry.Text = string.Empty;
        AddCostPriceEntry.Text = string.Empty;
        AddExpiryDatePicker.Date = DateTime.Today.AddMonths(6);

        AddProductOverlay.IsVisible = true;

        // Pastikan entry fokus setelah overlay tampil
        Dispatcher.Dispatch(() => AddNameEntry.Focus());
    }

    private void OnAddProductOverlayBackgroundTapped(object sender, TappedEventArgs e)
    {
        // Klik area gelap (backdrop) => tutup popup
        AddProductOverlay.IsVisible = false;
    }

    private void OnCancelAddProductClicked(object sender, EventArgs e)
    {
        AddProductOverlay.IsVisible = false;
    }

    private async void OnSaveAddProductClicked(object sender, EventArgs e)
    {
        string name = AddNameEntry.Text?.Trim() ?? string.Empty;
        string category = AddCategoryEntry.Text?.Trim() ?? string.Empty;
        string unit = AddUnitEntry.Text?.Trim() ?? "pcs";
        string? barcode = AddBarcodeEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(unit))
        {
            await DisplayAlert("Error", "Nama/Kategori/Satuan wajib diisi.", "OK");
            return;
        }

        if (!decimal.TryParse(AddSellPriceEntry.Text?.Trim(), out var sellPrice) || sellPrice <= 0 ||
            !decimal.TryParse(AddCostPriceEntry.Text?.Trim(), out var costPrice) || costPrice <= 0)
        {
            await DisplayAlert("Error", "Harga jual / harga modal tidak valid.", "OK");
            return;
        }

        if (!int.TryParse(AddInitialStockEntry.Text?.Trim(), out var initialStock) || initialStock < 0)
        {
            await DisplayAlert("Error", "Stok awal tidak valid.", "OK");
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

        DataStore.Products.Add(product);
        DatabaseService.InsertProduct(product);

        if (initialStock > 0)
        {
            var expiry = DateOnly.FromDateTime(AddExpiryDatePicker.Date);
            DataStore.AddPurchase(product.Id, initialStock, costPrice, expiry, "Stok awal", timestamp: DateTime.Now);
        }

        AddProductOverlay.IsVisible = false;
        await DisplayAlert("Sukses", $"Produk '{name}' berhasil ditambahkan.", "OK");
        LoadProductsAndRender(SearchEntry.Text);
    }

    private async void OnExportStockPdfClicked(object sender, EventArgs e)
    {
        try
        {
            var filePath = await ReportExportService.ExportStockPdfAsync();
            await TryOpenFileAsync(filePath);
            await DisplayAlert("Export PDF", $"PDF stok tersimpan:\n{filePath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Export PDF", $"Gagal export PDF stok: {ex.Message}", "OK");
        }
    }

    private async void OnExportStockExcelClicked(object sender, EventArgs e)
    {
        try
        {
            var filePath = await ReportExportService.ExportStockExcelAsync();
            await TryOpenFileAsync(filePath);
            await DisplayAlert("Export Excel", $"Excel stok tersimpan:\n{filePath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Export Excel", $"Gagal export Excel stok: {ex.Message}", "OK");
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

    private async void OnProductMenuClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Product product)
            return;

        string action = await DisplayActionSheet($"Aksi untuk {product.Name}", "Batal", null,
            "Riwayat Stok",
            "Barang Masuk (Pembelian)",
            "Penyesuaian Stok (Rusak/Kadaluarsa/Hilang)",
            "Stok Opname");

        switch (action)
        {
            case "Riwayat Stok":
                await ShowStockHistory(product);
                break;
            case "Barang Masuk (Pembelian)":
                await HandlePurchase(product);
                break;
            case "Penyesuaian Stok (Rusak/Kadaluarsa/Hilang)":
                await HandleAdjustment(product);
                break;
            case "Stok Opname":
                await HandleStockOpname(product);
                break;
        }
    }

    private async Task ShowStockHistory(Product product)
    {
        var movements = DataStore.StockMovements
            .Where(m => m.ProductId == product.Id)
            .OrderByDescending(m => m.Timestamp)
            .Take(10)
            .ToList();

        if (!movements.Any())
        {
            await DisplayAlert("Riwayat Stok", "Belum ada riwayat pergerakan stok.", "OK");
            return;
        }

        var lines = movements.Select(m =>
            $"{m.Timestamp:dd/MM/yyyy HH:mm} - {m.Type} - Qty: {m.Quantity} - Alasan: {m.Reason}");

        await DisplayAlert("Riwayat Stok", string.Join("\n", lines), "OK");
    }

    private async Task HandlePurchase(Product product)
    {
        string qtyText = await DisplayPromptAsync("Barang Masuk", $"Jumlah {product.Unit} yang dibeli:", keyboard: Keyboard.Numeric);
        if (!int.TryParse(qtyText, out var qty) || qty <= 0) return;

        string costText = await DisplayPromptAsync("Barang Masuk", "Harga modal per satuan (Rp):", keyboard: Keyboard.Numeric, initialValue: product.CostPrice.ToString());
        if (!decimal.TryParse(costText, out var unitCost)) return;

        string expiryText = await DisplayPromptAsync("Barang Masuk", "Tanggal kadaluarsa (yyyy-MM-dd):", initialValue: DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd"));
        if (!DateOnly.TryParse(expiryText, out var expiry)) return;

        DataStore.AddPurchase(product.Id, qty, unitCost, expiry, "Restock");
        await DisplayAlert("Sukses", "Stok berhasil ditambahkan.", "OK");
        LoadProductsAndRender(SearchEntry.Text);
    }

    private async Task HandleAdjustment(Product product)
    {
        string reason = await DisplayActionSheet("Alasan penyesuaian", "Batal", null, "Kadaluarsa", "Rusak", "Hilang");
        if (string.IsNullOrEmpty(reason) || reason == "Batal") return;

        string qtyText = await DisplayPromptAsync("Penyesuaian Stok", "Jumlah yang keluar (pcs):", keyboard: Keyboard.Numeric);
        if (!int.TryParse(qtyText, out var qty) || qty <= 0) return;

        DataStore.AdjustStockForDamage(product.Id, qty, reason);
        await DisplayAlert("Sukses", $"Penyesuaian stok ({reason}) berhasil.", "OK");
        LoadProductsAndRender(SearchEntry.Text);
    }

    private async Task HandleStockOpname(Product product)
    {
        int systemQty = DataStore.GetCurrentStock(product.Id);
        string physicalText = await DisplayPromptAsync("Stok Opname",
            $"Stok sistem: {systemQty} {product.Unit}\nMasukkan stok fisik hasil hitung:",
            keyboard: Keyboard.Numeric,
            initialValue: systemQty.ToString());

        if (!int.TryParse(physicalText, out var physicalQty)) return;

        DataStore.StockOpname(product.Id, physicalQty);

        int diff = physicalQty - systemQty;
        string message = $"Stok Sistem: {systemQty}\nStok Fisik: {physicalQty}\nSelisih: {diff}";
        await DisplayAlert("Stok Opname", message + "\n\nPenyesuaian telah disimpan.", "OK");
        LoadProductsAndRender(SearchEntry.Text);
    }
}
