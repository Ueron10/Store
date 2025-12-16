using SQLite;

namespace StoreProgram.Models;

public class StockBatch
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }
}
