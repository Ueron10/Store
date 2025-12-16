using SQLite;

namespace StoreProgram.Models;

public class StockBatch
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }

    // Tanggal barang masuk (batch ini dibuat/diinput).
    public DateTime PurchaseDate { get; set; } = DateTime.Now;

    // Nama batch di UI bisa pakai tanggal kadaluarsa.
    public DateOnly ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }

    // Diskon khusus per batch (berdasarkan expiry). Jika null/0 => tidak ada diskon.
    public decimal? DiscountPercent { get; set; }
}
