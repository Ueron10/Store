namespace StoreProgram.Pages;

public partial class FinancialPage : ContentPage
{
    public FinancialPage()
    {
        InitializeComponent();
    }

    private void OnNewTransactionClicked(object sender, EventArgs e)
    {
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
        // Mock price calculation based on selected product
        var prices = new Dictionary<int, int>
        {
            [0] = 3000,  // Indomie
            [1] = 4000,  // Aqua
            [2] = 12000, // Roti Tawar
            [3] = 8500,  // Susu
            [4] = 2500   // Kopi
        };

        if (ProductPicker.SelectedIndex >= 0 && int.TryParse(QuantityEntry.Text, out int quantity))
        {
            int price = prices.GetValueOrDefault(ProductPicker.SelectedIndex, 0);
            int total = price * quantity;
            TotalLabel.Text = $"Rp {total:N0}";
        }
        else
        {
            TotalLabel.Text = "Rp 0";
        }
    }

    private async void OnProcessTransactionClicked(object sender, EventArgs e)
    {
        if (ProductPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Error", "Pilih produk terlebih dahulu!", "OK");
            return;
        }

        if (!int.TryParse(QuantityEntry.Text, out int quantity) || quantity <= 0)
        {
            await DisplayAlert("Error", "Masukkan jumlah yang valid!", "OK");
            return;
        }

        string paymentMethod = CashRadio.IsChecked ? "Tunai" : "QRIS";
        string productName = ProductPicker.Items[ProductPicker.SelectedIndex].Split('-')[0].Trim();
        
        bool confirm = await DisplayAlert("Konfirmasi Transaksi", 
            $"Produk: {productName}\nJumlah: {quantity}\nTotal: {TotalLabel.Text}\nPembayaran: {paymentMethod}\n\nProses transaksi?", 
            "Ya", "Batal");

        if (confirm)
        {
            await DisplayAlert("Sukses", "Transaksi berhasil diproses!", "OK");
            OnCloseTransactionFormClicked(sender, e);
        }
    }

    private async void OnAddExpenseClicked(object sender, EventArgs e)
    {
        string description = await DisplayPromptAsync("Input Pengeluaran", "Deskripsi pengeluaran:");
        if (!string.IsNullOrEmpty(description))
        {
            string amount = await DisplayPromptAsync("Input Pengeluaran", "Jumlah pengeluaran (Rp):", keyboard: Keyboard.Numeric);
            if (!string.IsNullOrEmpty(amount))
            {
                await DisplayAlert("Sukses", $"Pengeluaran '{description}' sebesar Rp {amount} berhasil dicatat!", "OK");
            }
        }
    }

    private async void OnViewReceiptClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Struk Transaksi", "Menampilkan detail struk transaksi", "OK");
    }

    private async void OnEditExpenseClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Edit Pengeluaran", "Fitur edit pengeluaran", "OK");
    }
}