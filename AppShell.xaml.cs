using StoreProgram.Pages;
using StoreProgram.Services;

namespace StoreProgram
{
    public partial class AppShell : Shell
    {
        private bool _redirectingToLogin;

        public AppShell()
        {
            InitializeComponent();

            // NOTE:
            // Jangan register route yang namanya sama dengan Route milik TabBar/ShellContent di AppShell.xaml
            // (mis. "ownerdashboard", "employeedashboard", "stock", "financial", dll.) karena bisa bikin Shell
            // menganggap itu sebagai halaman biasa (push ke stack) bukan pindah tab.
            // Cukup register halaman yang *bukan* bagian dari TabBar.

            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("stockopname", typeof(StockOpnamePage));
            Routing.RegisterRoute("users", typeof(UserManagementPage));

            Navigating += OnShellNavigating;
        }

        private async void OnLogoutMenuClicked(object sender, EventArgs e)
        {
            await SessionManager.LogoutAsync();
        }

        private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
        {
            if (_redirectingToLogin) return;

            // Guard sederhana: kalau belum login, cegah akses ke route selain login.
            if (DataStore.CurrentUser == null)
            {
                var target = e.Target?.Location.OriginalString ?? string.Empty;

                // Allow menuju login.
                if (target.Contains("login", StringComparison.OrdinalIgnoreCase))
                    return;

                e.Cancel();
                _redirectingToLogin = true;
                try
                {
                    await GoToAsync("//login");
                }
                finally
                {
                    _redirectingToLogin = false;
                }
            }
        }
    }
}
