using StoreProgram.Pages;

namespace StoreProgram
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register page routes for navigation
            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("ownerdashboard", typeof(OwnerDashboardPage));
            Routing.RegisterRoute("employeedashboard", typeof(EmployeeDashboardPage));
            Routing.RegisterRoute("stock", typeof(StockManagementPage));
            Routing.RegisterRoute("financial", typeof(FinancialPage));
            Routing.RegisterRoute("reports", typeof(ReportsPage));
            Routing.RegisterRoute("notifications", typeof(NotificationsPage));
            Routing.RegisterRoute("stockopname", typeof(StockOpnamePage));
            Routing.RegisterRoute("discounts", typeof(DiscountsPage));
            Routing.RegisterRoute("users", typeof(UserManagementPage));
        }
    }
}
