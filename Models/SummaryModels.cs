namespace StoreProgram.Models;

public record DateRange(DateTime Start, DateTime End);

public class FinancialSummary
{
    public decimal TotalSales { get; set; }

    /// <summary>
    /// HPP yang dipakai untuk hitung gross profit.
    /// NOTE: Jika ada penjualan di bawah harga modal (mis. karena diskon batch),
    /// selisihnya dicatat sebagai ExpenseType.DiscountLoss dan HPP di sini sudah dikurangi selisih itu
    /// supaya NetProfit tidak double-count.
    /// </summary>
    public decimal TotalCostOfGoods { get; set; }

    public decimal GrossProfit => TotalSales - TotalCostOfGoods;

    public decimal OperationalExpenses { get; set; }
    public decimal DamagedLostExpenses { get; set; }
    public decimal DiscountLossExpenses { get; set; }
    public decimal PriveExpenses { get; set; }

    public decimal NetProfit => GrossProfit - (OperationalExpenses + DamagedLostExpenses + DiscountLossExpenses);

    public int TotalTransactions { get; set; }

    public List<TopProduct> TopProducts { get; set; } = new();
}

public class TopProduct
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalSales { get; set; }
}
