using Microsoft.Maui.ApplicationModel;
using StoreProgram.Services;

namespace StoreProgram
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Inisialisasi data in-memory (produk, stok awal, user, dll.)
            DataStore.Initialize();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);

            // Pindah ke halaman login setelah Shell aktif (tanpa pakai MainPage yang deprecated)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                await shell.GoToAsync("//login");
            });

            return window;
        }
    }
}
