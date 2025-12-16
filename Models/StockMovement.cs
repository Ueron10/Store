using SQLite;

namespace StoreProgram.Models;

public enum StockMovementType
{
    PurchaseIn,
    SaleOut,
    AdjustmentOut,
    AdjustmentIn,
    StockOpnameAdjustment
}

public class StockMovement
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}
