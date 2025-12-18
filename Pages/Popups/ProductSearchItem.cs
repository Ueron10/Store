using StoreProgram.Models;

namespace StoreProgram.Pages.Popups;

public sealed class ProductSearchItem
{
    public ProductSearchItem(Product product)
    {
        Product = product;
    }

    public Product Product { get; }

    public string DisplayName => Product.Name;
    public string PriceText => $"Rp {Product.SellPrice:N0}";

    public string MetaText
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Product.Category))
                parts.Add(Product.Category);
            if (!string.IsNullOrWhiteSpace(Product.Barcode))
                parts.Add($"Barcode: {Product.Barcode}");
            return parts.Count == 0 ? "" : string.Join(" • ", parts);
        }
    }
}
