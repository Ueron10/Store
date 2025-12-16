using SQLite;

namespace StoreProgram.Models;

public class Product
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // makanan, minuman, sembako, dll.
    public string? Barcode { get; set; }
    public string Unit { get; set; } = "pcs"; // pcs, btl, pak, dll.

    public decimal SellPrice { get; set; } // harga jual per unit
    public decimal CostPrice { get; set; }  // HPP per unit

    // Diskon khusus (misalnya untuk barang mendekati kadaluarsa)
    public decimal? DiscountPercent { get; set; } // 0-100, null berarti tidak ada diskon
}
