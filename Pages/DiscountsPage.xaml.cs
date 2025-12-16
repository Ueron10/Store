using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class DiscountsPage : ContentPage
{
    private class DiscountContext
    {
        public Product Product { get; set; } = null!;
        public Entry DiscountEntry { get; set; } = null!;
        public int TotalQty { get; set; }
        public DateOnly NearestExpiry { get; set; }
    }

    public DiscountsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildDiscountCards();
    }

    private void BuildDiscountCards()
    {
        DiscountsContainer.Children.Clear();

        var (nearing, _) = DataStore.GetExpiryAlerts();
        if (!nearing.Any())
        {
            DiscountsContainer.Children.Add(new Label
            {
                Text = "Tidak ada produk yang mendekati kadaluarsa.",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        var groups = nearing
            .GroupBy(b => b.ProductId)
            .Select(g => new
            {
                Product = DataStore.Products.FirstOrDefault(p => p.Id == g.Key),
                TotalQty = g.Sum(b => b.Quantity),
                NearestExpiry = g.Min(b => b.ExpiryDate)
            })
            .Where(x => x.Product != null)
            .OrderBy(x => x.NearestExpiry)
            .ToList();

        foreach (var item in groups)
        {
            var product = item.Product!;
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

            grid.Add(new Label
            {
                Text = $"Stok: {item.TotalQty} {product.Unit}",
                FontSize = 13,
                TextColor = Color.FromArgb("#6B7280")
            }, 0, 1);

            grid.Add(new Label
            {
                Text = $"Kadaluarsa: {item.NearestExpiry:dd MMM yyyy}",
                FontSize = 13,
                TextColor = Colors.OrangeRed
            }, 1, 1);

            var discountEntry = new Entry
            {
                Placeholder = "Diskon %",
                Keyboard = Keyboard.Numeric,
                WidthRequest = 80,
                BackgroundColor = Color.FromArgb("#F3F4F6"),
                Text = product.DiscountPercent?.ToString("0") ?? "0"
            };

            grid.Add(discountEntry, 0, 2);

            var saveButton = new Button
            {
                Text = "Simpan",
                BackgroundColor = Color.FromArgb("#6366F1"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(12, 6),
                FontSize = 13,
                HorizontalOptions = LayoutOptions.End
            };

            var ctx = new DiscountContext
            {
                Product = product,
                DiscountEntry = discountEntry,
                TotalQty = item.TotalQty,
                NearestExpiry = item.NearestExpiry
            };
            saveButton.CommandParameter = ctx;
            saveButton.Clicked += OnSaveDiscountClicked;

            grid.Add(saveButton, 1, 2);

            frame.Content = grid;
            DiscountsContainer.Children.Add(frame);
        }
    }

    private async void OnSaveDiscountClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not DiscountContext ctx)
            return;

        if (!decimal.TryParse(ctx.DiscountEntry.Text, out var percent))
        {
            await DisplayAlert("Error", "Diskon tidak valid.", "OK");
            return;
        }

        if (percent < 0) percent = 0;
        if (percent > 100) percent = 100;

        ctx.Product.DiscountPercent = percent;
        DatabaseService.UpdateProduct(ctx.Product);

        await DisplayAlert("Sukses", $"Diskon {percent:0}% disimpan untuk {ctx.Product.Name}.", "OK");
    }
}
