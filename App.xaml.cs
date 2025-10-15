namespace StoreProgram
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Set AppShell sebagai halaman utama
            MainPage = new AppShell();

            // Pindah ke halaman login setelah Shell aktif
            NavigateToLogin();
        }

        private async void NavigateToLogin()
        {
            // Tunggu 1 frame agar Shell benar-benar siap
            await Task.Delay(100);

            // Arahkan ke halaman login
            await Shell.Current.GoToAsync("//login");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage);
        }
    }
}
