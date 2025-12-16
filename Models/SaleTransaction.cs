using SQLite;

namespace StoreProgram.Models;

public class SaleItem
{
    // Disimpan ke database agar laporan tetap benar setelah aplikasi ditutup/dibuka lagi
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key ke SaleTransaction
    public Guid SaleId { get; set; }

    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class SaleTransaction
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<SaleItem> Items { get; set; } = new();
    public string PaymentMethod { get; set; } = "Tunai"; // Tunai atau QRIS

    public decimal GrossAmount => Items.Sum(i => i.LineTotal);
    public decimal TotalCost { get; set; } // total HPP dari stok yang terjual
    public decimal GrossProfit => GrossAmount - TotalCost;
}
