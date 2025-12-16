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

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("Tambah Produk", "Nama produk:");
        if (string.IsNullOrWhiteSpace(name)) return;

        string category = await DisplayPromptAsync("Tambah Produk", "Kategori (makanan/minuman/sembako/dll):", initialValue: "makanan");
        string unit = await DisplayPromptAsync("Tambah Produk", "Satuan (pcs/btl/pak/dll):", initialValue: "pcs");

        string sellPriceText = await DisplayPromptAsync("Tambah Produk", "Harga jual per satuan (Rp):", keyboard: Keyboard.Numeric);
        string costPriceText = await DisplayPromptAsync("Tambah Produk", "Harga modal per satuan (Rp):", keyboard: Keyboard.Numeric);
        string initialStockText = await DisplayPromptAsync("Tambah Produk", "Stok awal:", keyboard: Keyboard.Numeric, initialValue: "0");
        string expiryText = await DisplayPromptAsync("Tambah Produk", "Tanggal kadaluarsa (yyyy-MM-dd):", initialValue: DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd"));

        if (!decimal.TryParse(sellPriceText, out var sellPrice) ||
            !decimal.TryParse(costPriceText, out var costPrice) ||
            !int.TryParse(initialStockText, out var initialStock) ||
            !DateOnly.TryParse(expiryText, out var expiry))
        {
            await DisplayAlert("Error", "Input tidak valid.", "OK");
            return;
        }

        var product = new Product
        {
            Name = name,
            Category = category ?? string.Empty,
            Unit = unit ?? "pcs",
            SellPrice = sellPrice,
            CostPrice = costPrice
        };

        DataStore.Products.Add(product);
        DatabaseService.InsertProduct(product);

        if (initialStock > 0)
        {
            DataStore.AddPurchase(product.Id, initialStock, costPrice, expiry, "Stok awal");
        }

        await DisplayAlert("Sukses", $"Produk '{name}' berhasil ditambahkan.", "OK");
        LoadProductsAndRender();
    }

    private async void OnProductMenuClicked(object sender, EventArgs e)
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
