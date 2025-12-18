using StoreProgram.Models;

namespace StoreProgram.Pages.Popups;

public partial class ProductSearchPopupPage : ContentPage
{
    private readonly TaskCompletionSource<Product?> _tcs = new();
    private readonly List<Product> _allProducts;
    private List<ProductSearchItem> _filtered;
    private INavigation? _hostNav;
    private bool _closing;

    public ProductSearchPopupPage(IEnumerable<Product> products)
    {
        InitializeComponent();

        _allProducts = products.OrderBy(p => p.Name).ToList();
        _filtered = _allProducts.Select(p => new ProductSearchItem(p)).ToList();

        Bind();

        // Fokus otomatis
        Dispatcher.Dispatch(() => SearchBar.Focus());
    }

    public Task<Product?> Result => _tcs.Task;

    private void Bind()
    {
        ProductsView.ItemsSource = _filtered;
        EmptyLabel.IsVisible = _filtered.Count == 0;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(q))
        {
            _filtered = _allProducts.Select(p => new ProductSearchItem(p)).ToList();
            Bind();
            return;
        }

        _filtered = _allProducts
            .Where(p =>
                (!string.IsNullOrWhiteSpace(p.Name) && p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.Barcode) && p.Barcode.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .Select(p => new ProductSearchItem(p))
            .ToList();

        Bind();
    }

    private async Task SafeCloseAsync(Product? result)
    {
        if (_closing)
            return;

        _closing = true;
        _tcs.TrySetResult(result);

        try
        {
            var nav = _hostNav ?? Navigation;
            await nav.PopModalAsync(animated: false);
        }
        catch
        {
            // ignore: avoid crashing when selection triggers close twice on some platforms
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
        => await SafeCloseAsync(result: null);

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_closing)
            return;

        try
        {
            if (e.CurrentSelection?.FirstOrDefault() is not ProductSearchItem vm)
                return;

            var product = vm.Product;

            // Clear selection first to prevent re-entry
            ProductsView.SelectedItem = null;

            // Defer close sedikit supaya selection event selesai dulu (hindari race)
            await Task.Delay(50);

            await SafeCloseAsync(product);
        }
        catch (Exception ex)
        {
            // Log for debugging
            System.Diagnostics.Debug.WriteLine($"[ProductSearch] Selection error: {ex}");
            
            // Close anyway supaya tidak stuck
            await SafeCloseAsync(null);
        }
    }

    public static async Task<Product?> ShowAsync(IEnumerable<Product> products)
    {
        var page = new ProductSearchPopupPage(products);

        // Lebih aman pakai Navigation dari CurrentPage daripada Shell.Current.Navigation,
        // supaya push/pop modal konsisten (menghindari PopModalAsync error -> Debugger.Break).
        var hostPage = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        var nav = hostPage?.Navigation;
        if (nav == null) return null;

        page._hostNav = nav;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
