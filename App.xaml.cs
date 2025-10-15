namespace StoreProgram
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            
            // Navigate to login page on app start
            if (shell.Items == null)
                throw new InvalidOperationException("Shell.Items is null after InitializeComponent(). Check your XAML for errors.");


            shell.CurrentItem = shell.Items.FirstOrDefault(item => item.Route == "login");
            
            return new Window(shell);
        }
    }
}