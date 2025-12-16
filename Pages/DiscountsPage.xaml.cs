using StoreProgram.Models;
using StoreProgram.Services;

namespace StoreProgram.Pages;

public partial class DiscountsPage : ContentPage
{
    private class DiscountContext
    {
        public StockBatch Batch { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public Entry DiscountEntry { get; set; } = null!;
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
                Text = "Tidak ada produk yang mendekati kadaluarsa (≤ 14 hari).",
                FontSize = 14,
                TextColor = Color.FromArgb("#6B7280")
            });
            return;
        }

        foreach (var batch in nearing.OrderBy(b => b.ExpiryDate))
        {
            var product = DataStore.Products.FirstOrDefault(p => p.Id == batch.ProductId);
            if (product == null) continue;

            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 12,
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
                Text = $"Batch: {batch.Quantity} {product.Unit}",
                FontSize = 13,
                TextColor = Color.FromArgb("#6B7280")
            }, 0, 1);

            grid.Add(new Label
            {
                Text = $"Kadaluarsa: {batch.ExpiryDate:dd MMM yyyy}",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#991B1B") // DangerText
            }, 1, 1);

            // Harga setelah diskon (preview)
            decimal percentNow = batch.DiscountPercent is > 0 and <= 100 ? batch.DiscountPercent.Value : 0m;
            decimal discountedPrice = product.SellPrice * (100 - percentNow) / 100m;

            var priceLabel = new Label
            {
                Text = $"Harga: Rp {product.SellPrice:N0}  →  Rp {discountedPrice:N0}",
                FontSize = 12,
                TextColor = Color.FromArgb("#111827")
            };
            grid.Add(priceLabel, 0, 2);

            var discountEntry = new Entry
            {
                Placeholder = "Diskon %",
                Keyboard = Keyboard.Numeric,
                WidthRequest = 70,
                BackgroundColor = Color.FromArgb("#E5E7EB"),
                TextColor = Color.FromArgb("#111827"),
                Text = percentNow.ToString("0")
            };

            var saveButton = new Button
            {
                Text = "Simpan",
                BackgroundColor = Color.FromArgb("#E5E7EB"),
                TextColor = Color.FromArgb("#111827"),
                BorderColor = Color.FromArgb("#CBD5E1"),
                BorderWidth = 1,
                CornerRadius = 10,
                Padding = new Thickness(12, 6),
                FontSize = 13,
                HorizontalOptions = LayoutOptions.End
            };

            var actionRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 8,
                HorizontalOptions = LayoutOptions.End,
                Children = { discountEntry, saveButton }
            };
            grid.Add(actionRow, 1, 2);

            var ctx = new DiscountContext
            {
                Batch = batch,
                Product = product,
                DiscountEntry = discountEntry
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

        ctx.Batch.DiscountPercent = percent;
        DatabaseService.UpdateStockBatch(ctx.Batch);

        await DisplayAlert("Sukses", $"Diskon {percent:0}% disimpan untuk {ctx.Product.Name} (batch exp {ctx.Batch.ExpiryDate:dd MMM yyyy}).", "OK");

        // Refresh kartu agar harga preview ikut berubah
        BuildDiscountCards();
    }
}
