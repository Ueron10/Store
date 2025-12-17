using StoreProgram.Models;
using StoreProgram.Pages.Popups;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class StockOpnamePage : ContentPage
{
    private class StockOpnameContext
    {
        public Guid ProductId { get; set; }
        public int SystemQty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }

    public StockOpnamePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildRows();
    }

    private void BuildRows()
    {
        StockOpnameContainer.Children.Clear();

        foreach (var product in DataStore.Products.OrderBy(p => p.Name))
        {
            int systemQty = DataStore.GetCurrentStock(product.Id);

            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 10,
                Padding = 15,
                HasShadow = true
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition{ Height = GridLength.Auto },
                    new RowDefinition{ Height = GridLength.Auto },
                    new RowDefinition{ Height = GridLength.Auto }
                },
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Auto }
                },
                RowSpacing = 4,
                ColumnSpacing = 10
            };

            var nameLabel = new Label
            {
                Text = product.Name,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#111827")
            };
            grid.Add(nameLabel, 0, 0);
            Grid.SetColumnSpan(nameLabel, 2);

            var systemLabel = new Label
            {
                Text = $"Stok Sistem: {systemQty} {product.Unit}",
                FontSize = 13,
                TextColor = Color.FromArgb("#6B7280")
            };
            grid.Add(systemLabel, 0, 1);
            Grid.SetColumnSpan(systemLabel, 2);

            var hintLabel = new Label
            {
                Text = "Klik 'Sesuaikan' untuk memasukkan stok fisik.",
                FontSize = 12,
                TextColor = Color.FromArgb("#6B7280")
            };
            grid.Add(hintLabel, 0, 2);

            var button = new Button
            {
                Text = "Sesuaikan",
                BackgroundColor = Color.FromArgb("#10B981"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(12, 6),
                FontSize = 13,
                HorizontalOptions = LayoutOptions.End
            };

            var ctx = new StockOpnameContext
            {
                ProductId = product.Id,
                SystemQty = systemQty,
                Unit = product.Unit,
                ProductName = product.Name
            };
            button.CommandParameter = ctx;
            button.Clicked += OnApplyStockOpnameClicked;

            grid.Add(button, 1, 2);

            frame.Content = grid;
            StockOpnameContainer.Children.Add(frame);
        }
    }

    private async void OnApplyStockOpnameClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not StockOpnameContext ctx)
            return;

        var physicalQty = await StockOpnamePopupPage.ShowAsync(ctx.ProductName, ctx.Unit, ctx.SystemQty);
        if (physicalQty == null) return;

        DataStore.StockOpname(ctx.ProductId, physicalQty.Value);

        int diff = physicalQty.Value - ctx.SystemQty;
        string message = $"Produk: {ctx.ProductName}\nStok Sistem: {ctx.SystemQty} {ctx.Unit}\nStok Fisik: {physicalQty.Value} {ctx.Unit}\nSelisih: {diff}";
        await InfoPopupPage.ShowAsync("Stok Opname", message + "\n\nPenyesuaian telah disimpan.");

        // Refresh tampilan
        BuildRows();
    }
}
