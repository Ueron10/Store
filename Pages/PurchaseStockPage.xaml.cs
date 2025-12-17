using StoreProgram.Models;
using StoreProgram.Pages.Popups;
using StoreProgram.Services;

namespace StoreProgram.Pages;

// Halaman ini belum dipakai di flow utama (StockManagementPage pakai prompt).
// Tapi code-behind ini diperlukan agar build tidak gagal (event handler di XAML).
[QueryProperty(nameof(ProductId), "productId")]
public partial class PurchaseStockPage : ContentPage
{
    public string? ProductId { get; set; }

    private Guid? _productGuid;

    public PurchaseStockPage()
    {
        InitializeComponent();

        PurchaseDatePicker.Date = DateTime.Today;
        ExpiryDatePicker.Date = DateTime.Today.AddMonths(6);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Guid.TryParse(ProductId, out var id))
        {
            _productGuid = id;
            var p = DataStore.Products.FirstOrDefault(x => x.Id == id);
            if (p != null)
                TitleLabel.Text = $"📥 Barang Masuk - {p.Name}";
        }
        else
        {
            _productGuid = null;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Kembali ke halaman sebelumnya (jika ada)
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            await Navigation.PopAsync();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_productGuid is null)
        {
            await InfoPopupPage.ShowAsync("Error", "ProductId belum ada. Buka halaman ini lewat route dengan parameter productId.");
            return;
        }

        if (!int.TryParse(QtyEntry.Text?.Trim(), out var qty) || qty <= 0)
        {
            await InfoPopupPage.ShowAsync("Error", "Jumlah tidak valid.");
            return;
        }

        if (!decimal.TryParse(UnitCostEntry.Text?.Trim(), out var unitCost) || unitCost <= 0)
        {
            await InfoPopupPage.ShowAsync("Error", "Harga modal tidak valid.");
            return;
        }

        var purchaseDate = PurchaseDatePicker.Date.Date;
        var expiryDate = DateOnly.FromDateTime(ExpiryDatePicker.Date.Date);

        // Timestamp pakai tanggal yang dipilih + jam sekarang biar enak untuk laporan harian.
        var ts = purchaseDate.Add(DateTime.Now.TimeOfDay);

        DataStore.AddPurchase(_productGuid.Value, qty, unitCost, expiryDate, "Restock", timestamp: ts);

        await InfoPopupPage.ShowAsync("Sukses", "Stok berhasil ditambahkan.");
        OnCancelClicked(sender, e);
    }
}
