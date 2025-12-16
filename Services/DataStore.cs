using StoreProgram.Models;

namespace StoreProgram.Services;

public static class DataStore
{
    public static List<Product> Products { get; } = new();
    public static List<StockBatch> StockBatches { get; } = new();
    public static List<StockMovement> StockMovements { get; } = new();
    public static List<SaleTransaction> Sales { get; } = new();
    public static List<Expense> Expenses { get; } = new();
    public static List<AppUser> Users { get; } = new();

    public static ExpiryAlertSetting ExpirySetting { get; } = new();

    public static AppUser? CurrentUser { get; private set; }

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        // Load semua data dari database (user murni dari database, tanpa seed hardcode)
        var dbProducts = DatabaseService.GetAllProducts();
        var dbBatches = DatabaseService.GetAllStockBatches();
        var dbMovements = DatabaseService.GetAllStockMovements();
        var dbExpenses = DatabaseService.GetAllExpenses();
        var dbSales = DatabaseService.GetAllSales();
        var dbUsers = DatabaseService.GetAllUsers();

        Products.AddRange(dbProducts);
        StockBatches.AddRange(dbBatches);
        StockMovements.AddRange(dbMovements);
        Expenses.AddRange(dbExpenses);
        Sales.AddRange(dbSales);
        Users.AddRange(dbUsers);

        _initialized = true;
    }

    public static void SetCurrentUser(AppUser user) => CurrentUser = user;

    public static int GetCurrentStock(Guid productId)
    {
        return StockBatches.Where(b => b.ProductId == productId).Sum(b => b.Quantity);
    }

    public static StockBatch AddPurchase(Guid productId, int quantity, decimal unitCost, DateOnly expiryDate, string reason)
    {
        var batch = new StockBatch
        {
            ProductId = productId,
            Quantity = quantity,
            UnitCost = unitCost,
            ExpiryDate = expiryDate
        };
        StockBatches.Add(batch);
        DatabaseService.InsertStockBatch(batch);

        var movement = new StockMovement
        {
            ProductId = productId,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = unitCost * quantity,
            Type = StockMovementType.PurchaseIn,
            Reason = reason
        };
        StockMovements.Add(movement);
        DatabaseService.InsertStockMovement(movement);

        // Catat sebagai pengeluaran operasional pembelian stok
        var expense = new Expense
        {
            Description = $"Pembelian stok {GetProductName(productId)}",
            Amount = movement.TotalCost,
            Type = ExpenseType.Operational
        };
        Expenses.Add(expense);
        DatabaseService.InsertExpense(expense);

        return batch;
    }

    private static string GetProductName(Guid productId)
        => Products.FirstOrDefault(p => p.Id == productId)?.Name ?? "Produk";

    /// <summary>
    /// Proses penjualan 1 jenis produk sederhana (sesuai UI FinancialPage sekarang).
    /// Jika ke depan ingin multi-item, bisa dikembangkan dari sini.
    /// </summary>
    public static SaleTransaction ProcessSingleItemSale(Guid productId, int quantity, string paymentMethod)
    {
        var product = Products.First(p => p.Id == productId);

        // Hitung harga jual (perhatikan diskon jika ada)
        decimal unitPrice = product.SellPrice;
        if (product.DiscountPercent is > 0 and <= 100)
        {
            unitPrice = unitPrice * (100 - product.DiscountPercent.Value) / 100m;
        }

        // Kurangi stok dengan FIFO dari batch yang paling dekat kadaluarsa
        int remaining = quantity;
        decimal totalCost = 0;

        var batches = StockBatches
            .Where(b => b.ProductId == productId && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToList();

        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            int take = Math.Min(remaining, batch.Quantity);
            batch.Quantity -= take;
            DatabaseService.UpdateStockBatch(batch);
            remaining -= take;

            totalCost += take * batch.UnitCost;

            var movement = new StockMovement
            {
                ProductId = productId,
                Quantity = take,
                UnitCost = batch.UnitCost,
                TotalCost = take * batch.UnitCost,
                Type = StockMovementType.SaleOut,
                Reason = "Penjualan"
            };
            StockMovements.Add(movement);
            DatabaseService.InsertStockMovement(movement);
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException("Stok tidak mencukupi untuk penjualan ini.");
        }

        var item = new SaleItem
        {
            ProductId = productId,
            ProductName = product.Name,
            Quantity = quantity,
            UnitPrice = unitPrice
        };

        var sale = new SaleTransaction
        {
            Items = new List<SaleItem> { item },
            PaymentMethod = paymentMethod,
            TotalCost = totalCost
        };

        Sales.Add(sale);
        DatabaseService.InsertSale(sale);
        return sale;
    }

    public static void AdjustStockForDamage(Guid productId, int quantity, string reason)
    {
        if (quantity <= 0) return;

        int remaining = quantity;
        decimal totalCost = 0;

        var batches = StockBatches
            .Where(b => b.ProductId == productId && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToList();

        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            int take = Math.Min(remaining, batch.Quantity);
            batch.Quantity -= take;
            DatabaseService.UpdateStockBatch(batch);
            remaining -= take;

            totalCost += take * batch.UnitCost;

            var movement = new StockMovement
            {
                ProductId = productId,
                Quantity = take,
                UnitCost = batch.UnitCost,
                TotalCost = take * batch.UnitCost,
                Type = StockMovementType.AdjustmentOut,
                Reason = reason
            };
            StockMovements.Add(movement);
            DatabaseService.InsertStockMovement(movement);
        }

        if (totalCost > 0)
        {
            var expense = new Expense
            {
                Description = $"Kerugian stok {GetProductName(productId)} ({reason})",
                Amount = totalCost,
                Type = ExpenseType.DamagedLostStock
            };
            Expenses.Add(expense);
            DatabaseService.InsertExpense(expense);
        }
    }

    public static void StockOpname(Guid productId, int physicalQuantity)
    {
        int systemQty = GetCurrentStock(productId);
        int diff = physicalQuantity - systemQty;

        if (diff == 0) return;

        if (diff < 0)
        {
            // Kehilangan stok
            AdjustStockForDamage(productId, -diff, "Selisih stok opname");
        }
        else
        {
            // Ada stok fisik lebih banyak dari sistem, tambahkan sebagai adjustment in
            var product = Products.First(p => p.Id == productId);
            var batch = new StockBatch
            {
                ProductId = productId,
                Quantity = diff,
                UnitCost = product.CostPrice,
                ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(12))
            };
            StockBatches.Add(batch);
            DatabaseService.InsertStockBatch(batch);

            var movement = new StockMovement
            {
                ProductId = productId,
                Quantity = diff,
                UnitCost = batch.UnitCost,
                TotalCost = diff * batch.UnitCost,
                Type = StockMovementType.StockOpnameAdjustment,
                Reason = "Penyesuaian stok opname (kelebihan)"
            };
            StockMovements.Add(movement);
            DatabaseService.InsertStockMovement(movement);
        }
    }

    public static void AddOperationalExpense(string description, decimal amount)
    {
        if (amount <= 0) return;

        var expense = new Expense
        {
            Description = description,
            Amount = amount,
            Type = ExpenseType.Operational
        };
        Expenses.Add(expense);
        DatabaseService.InsertExpense(expense);
    }

    public static void AddPrive(string description, decimal amount)
    {
        if (amount <= 0) return;

        var expense = new Expense
        {
            Description = description,
            Amount = amount,
            Type = ExpenseType.Prive
        };
        Expenses.Add(expense);
        DatabaseService.InsertExpense(expense);
    }

    public static FinancialSummary GetSummary(DateRange range)
    {
        var salesInRange = Sales.Where(s => s.Timestamp >= range.Start && s.Timestamp <= range.End).ToList();
        var expensesInRange = Expenses.Where(e => e.Timestamp >= range.Start && e.Timestamp <= range.End).ToList();

        var summary = new FinancialSummary
        {
            TotalSales = salesInRange.Sum(s => s.GrossAmount),
            TotalCostOfGoods = salesInRange.Sum(s => s.TotalCost),
            OperationalExpenses = expensesInRange.Where(e => e.Type == ExpenseType.Operational).Sum(e => e.Amount),
            DamagedLostExpenses = expensesInRange.Where(e => e.Type == ExpenseType.DamagedLostStock).Sum(e => e.Amount),
            PriveExpenses = expensesInRange.Where(e => e.Type == ExpenseType.Prive).Sum(e => e.Amount),
            TotalTransactions = salesInRange.Count
        };

        // Top products by quantity sold
        var productSales = salesInRange
            .SelectMany(s => s.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new TopProduct
            {
                ProductId = g.Key,
                ProductName = g.First().ProductName,
                QuantitySold = g.Sum(i => i.Quantity),
                TotalSales = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToList();

        summary.TopProducts = productSales;
        return summary;
    }

    public static (List<StockBatch> nearingExpiry, List<StockBatch> expired) GetExpiryAlerts()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var maxDay = today.AddDays(ExpirySetting.DaysBeforeExpiryMax);
        var minDay = today.AddDays(ExpirySetting.DaysBeforeExpiryWarning);

        var expired = StockBatches
            .Where(b => b.Quantity > 0 && b.ExpiryDate < today)
            .ToList();

        var nearing = StockBatches
            .Where(b => b.Quantity > 0 && b.ExpiryDate >= minDay && b.ExpiryDate <= maxDay)
            .ToList();

        return (nearing, expired);
    }
}
