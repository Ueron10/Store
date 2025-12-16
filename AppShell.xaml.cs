using StoreProgram.Pages;

namespace StoreProgram
{
    public partial class AppShell : Shell
    {
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
        }
    }
}
