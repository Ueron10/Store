using SQLite;

namespace StoreProgram.Models;

public enum ExpenseType
{
    Operational,
    Prive,
    DamagedLostStock
}

public class Expense
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ExpenseType Type { get; set; }
}
