namespace StoreProgram.Pages;

public partial class StockManagementPage : ContentPage
{
    public StockManagementPage()
    {
        InitializeComponent();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ClearSearchButton.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);
        
        // In a real app, you would filter the stock list based on search text
        // For now, this is just a UI mockup
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        ClearSearchButton.IsVisible = false;
    }

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        // Show add product form
        string result = await DisplayPromptAsync("Tambah Produk", "Masukkan nama produk:");
        if (!string.IsNullOrEmpty(result))
        {
            await DisplayAlert("Sukses", $"Produk '{result}' berhasil ditambahkan!", "OK");
        }
    }

    private async void OnProductMenuClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Pilih Aksi", "Batal", null, 
            "Edit Produk", "Update Stok", "Hapus Produk", "Lihat Detail");

        switch (action)
        {
            case "Edit Produk":
                await DisplayAlert("Edit", "Fitur edit produk", "OK");
                break;
            case "Update Stok":
                await DisplayAlert("Update Stok", "Fitur update stok", "OK");
                break;
            case "Hapus Produk":
                bool confirm = await DisplayAlert("Konfirmasi", "Hapus produk ini?", "Ya", "Tidak");
                if (confirm)
                {
                    await DisplayAlert("Sukses", "Produk berhasil dihapus", "OK");
                }
                break;
            case "Lihat Detail":
                await DisplayAlert("Detail", "Detail lengkap produk", "OK");
                break;
        }
    }
}