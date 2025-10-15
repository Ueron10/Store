namespace StoreProgram.Pages;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage()
    {
        InitializeComponent();
    }

    private async void OnClearAllClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Konfirmasi", "Hapus semua notifikasi?", "Ya", "Batal");
        if (confirm)
        {
            await DisplayAlert("Sukses", "Semua notifikasi berhasil dihapus", "OK");
        }
    }

    private async void OnRestockClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Restock Produk", "Buka halaman manajemen stok untuk melakukan restock?", "Ya", "Batal");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//stock");
        }
    }

    private async void OnRemoveExpiredClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Hapus Produk Expired", "Hapus produk yang sudah expired dari stok?", "Ya", "Batal");
        if (confirm)
        {
            await DisplayAlert("Sukses", "Produk expired berhasil dihapus dari stok", "OK");
        }
    }

    private async void OnMarkAsReadClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Notifikasi", "Notifikasi ditandai sudah dibaca", "OK");
    }

    private async void OnViewTransactionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//financial");
    }
}