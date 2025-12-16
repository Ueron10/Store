using MySql.Data.MySqlClient;
using StoreProgram.Models;

namespace StoreProgram.Services;

public static class DatabaseService
{
    // TODO: sesuaikan connection string dengan konfigurasi MySQL kamu
    private const string ConnectionString =
        "Server=localhost;Port=3306;Database=store_program;User Id=root;Password=;";

    private static MySqlConnection? _connection;
    private static readonly object _lock = new();

    private static MySqlConnection Connection
    {
        get
        {
            if (_connection != null)
                return _connection;

            try
            {
                _connection = new MySqlConnection(ConnectionString);
                _connection.Open();
                return _connection;
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal konek ke MySQL: " + ex.Message, ex);
            }
        }
    }

    #region Helper mapping

    private static ExpenseType ReadExpenseType(MySqlDataReader reader, string column)
        => Enum.Parse<ExpenseType>(reader.GetString(column));

    private static StockMovementType ReadStockMovementType(MySqlDataReader reader, string column)
        => Enum.Parse<StockMovementType>(reader.GetString(column));

    private static Guid ReadGuid(MySqlDataReader reader, string column)
    {
        var value = reader[column];
        return value switch
        {
            Guid g => g,
            string s => Guid.Parse(s),
            byte[] bytes => new Guid(bytes),
            _ => Guid.Parse(Convert.ToString(value)!)
        };
    }

    #endregion

    public static IList<Product> GetAllProducts()
    {
        lock (_lock)
        {
            var list = new List<Product>();

            using var cmd = new MySqlCommand(@"SELECT
    Id, Name, Category, Barcode, Unit,
    SellPrice, CostPrice, DiscountPercent
FROM Product;", Connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var p = new Product
                {
                    Id = ReadGuid(reader, "Id"),
                    Name = reader.GetString("Name"),
                    Category = reader.GetString("Category"),
                    Barcode = reader.IsDBNull(reader.GetOrdinal("Barcode"))
                        ? null
                        : reader.GetString("Barcode"),
                    Unit = reader.GetString("Unit"),
                    SellPrice = reader.GetDecimal("SellPrice"),
                    CostPrice = reader.GetDecimal("CostPrice"),
                    DiscountPercent = reader.IsDBNull(reader.GetOrdinal("DiscountPercent"))
                        ? null
                        : reader.GetDecimal("DiscountPercent")
                };
                list.Add(p);
            }

            return list;
        }
    }

    public static IList<StockBatch> GetAllStockBatches()
    {
        lock (_lock)
        {
            var list = new List<StockBatch>();

            using var cmd = new MySqlCommand(@"SELECT
    Id, ProductId, Quantity, ExpiryDate, UnitCost
FROM StockBatch;", Connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var b = new StockBatch
                {
                    Id = ReadGuid(reader, "Id"),
                    ProductId = ReadGuid(reader, "ProductId"),
                    Quantity = reader.GetInt32("Quantity"),
                    ExpiryDate = DateOnly.FromDateTime(reader.GetDateTime("ExpiryDate")),
                    UnitCost = reader.GetDecimal("UnitCost")
                };
                list.Add(b);
            }

            return list;
        }
    }

    public static IList<StockMovement> GetAllStockMovements()
    {
        lock (_lock)
        {
            var list = new List<StockMovement>();

            using var cmd = new MySqlCommand(@"SELECT
    Id, ProductId, Timestamp, Type, Quantity, Reason, UnitCost, TotalCost
FROM StockMovement;", Connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var m = new StockMovement
                {
                    Id = ReadGuid(reader, "Id"),
                    ProductId = ReadGuid(reader, "ProductId"),
                    Timestamp = reader.GetDateTime("Timestamp"),
                    Type = ReadStockMovementType(reader, "Type"),
                    Quantity = reader.GetInt32("Quantity"),
                    Reason = reader.IsDBNull(reader.GetOrdinal("Reason"))
                        ? null
                        : reader.GetString("Reason"),
                    UnitCost = reader.GetDecimal("UnitCost"),
                    TotalCost = reader.GetDecimal("TotalCost")
                };
                list.Add(m);
            }

            return list;
        }
    }

    public static IList<Expense> GetAllExpenses()
    {
        lock (_lock)
        {
            var list = new List<Expense>();

            using var cmd = new MySqlCommand(@"SELECT
    Id, Timestamp, Description, Amount, Type
FROM Expense;", Connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var e = new Expense
                {
                    Id = ReadGuid(reader, "Id"),
                    Timestamp = reader.GetDateTime("Timestamp"),
                    Description = reader.GetString("Description"),
                    Amount = reader.GetDecimal("Amount"),
                    Type = ReadExpenseType(reader, "Type")
                };
                list.Add(e);
            }

            return list;
        }
    }

    public static IList<SaleTransaction> GetAllSales()
    {
        lock (_lock)
        {
            var list = new List<SaleTransaction>();

            using var cmd = new MySqlCommand(@"SELECT
    Id, Timestamp, PaymentMethod, TotalCost
FROM SaleTransaction;", Connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var s = new SaleTransaction
                {
                    Id = ReadGuid(reader, "Id"),
                    Timestamp = reader.GetDateTime("Timestamp"),
                    PaymentMethod = reader.GetString("PaymentMethod"),
                    TotalCost = reader.GetDecimal("TotalCost"),
                    Items = new List<SaleItem>() // detail item tidak di-load (sama seperti implementasi SQLite lama)
                };
                list.Add(s);
            }

            return list;
        }
    }

    public static IList<AppUser> GetAllUsers()
    {
        lock (_lock)
        {
            var list = new List<AppUser>();

            using var cmd = new MySqlCommand(@"SELECT
    Username, Password, Role, Email, Phone
FROM AppUser;", Connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var u = new AppUser
                {
                    Username = reader.GetString("Username"),
                    Password = reader.GetString("Password"),
                    Role = reader.GetString("Role"),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                        ? null
                        : reader.GetString("Email"),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone"))
                        ? null
                        : reader.GetString("Phone")
                };
                list.Add(u);
            }

            return list;
        }
    }

    public static void InsertProduct(Product product)
    {
        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"INSERT INTO Product
(Id, Name, Category, Barcode, Unit, SellPrice, CostPrice, DiscountPercent)
VALUES
(@Id, @Name, @Category, @Barcode, @Unit, @SellPrice, @CostPrice, @DiscountPercent);", Connection);

            cmd.Parameters.AddWithValue("@Id", product.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Barcode", (object?)product.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Unit", product.Unit);
            cmd.Parameters.AddWithValue("@SellPrice", product.SellPrice);
            cmd.Parameters.AddWithValue("@CostPrice", product.CostPrice);
            cmd.Parameters.AddWithValue("@DiscountPercent", (object?)product.DiscountPercent ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }

    public static void UpdateProduct(Product product)
    {
        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"UPDATE Product SET
    Name = @Name,
    Category = @Category,
    Barcode = @Barcode,
    Unit = @Unit,
    SellPrice = @SellPrice,
    CostPrice = @CostPrice,
    DiscountPercent = @DiscountPercent
WHERE Id = @Id;", Connection);

            cmd.Parameters.AddWithValue("@Id", product.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Barcode", (object?)product.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Unit", product.Unit);
            cmd.Parameters.AddWithValue("@SellPrice", product.SellPrice);
            cmd.Parameters.AddWithValue("@CostPrice", product.CostPrice);
            cmd.Parameters.AddWithValue("@DiscountPercent", (object?)product.DiscountPercent ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }

    public static void InsertStockBatch(StockBatch batch)
    {
        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"INSERT INTO StockBatch
(Id, ProductId, Quantity, ExpiryDate, UnitCost)
VALUES
(@Id, @ProductId, @Quantity, @ExpiryDate, @UnitCost);", Connection);

            cmd.Parameters.AddWithValue("@Id", batch.Id.ToString());
            cmd.Parameters.AddWithValue("@ProductId", batch.ProductId.ToString());
            cmd.Parameters.AddWithValue("@Quantity", batch.Quantity);
            cmd.Parameters.AddWithValue("@ExpiryDate", batch.ExpiryDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@UnitCost", batch.UnitCost);

            cmd.ExecuteNonQuery();
        }
    }

    public static void UpdateStockBatch(StockBatch batch)
    {
        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"UPDATE StockBatch SET
    ProductId = @ProductId,
    Quantity = @Quantity,
    ExpiryDate = @ExpiryDate,
    UnitCost = @UnitCost
WHERE Id = @Id;", Connection);

            cmd.Parameters.AddWithValue("@Id", batch.Id.ToString());
            cmd.Parameters.AddWithValue("@ProductId", batch.ProductId.ToString());
            cmd.Parameters.AddWithValue("@Quantity", batch.Quantity);
            cmd.Parameters.AddWithValue("@ExpiryDate", batch.ExpiryDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@UnitCost", batch.UnitCost);

            cmd.ExecuteNonQuery();
        }
    }

    public static void InsertStockMovement(StockMovement movement)
    {
        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"INSERT INTO StockMovement
(Id, ProductId, Timestamp, Type, Quantity, Reason, UnitCost, TotalCost)
VALUES
(@Id, @ProductId, @Timestamp, @Type, @Quantity, @Reason, @UnitCost, @TotalCost);", Connection);

            cmd.Parameters.AddWithValue("@Id", movement.Id.ToString());
            cmd.Parameters.AddWithValue("@ProductId", movement.ProductId.ToString());
            cmd.Parameters.AddWithValue("@Timestamp", movement.Timestamp);
            cmd.Parameters.AddWithValue("@Type", movement.Type.ToString());
            cmd.Parameters.AddWithValue("@Quantity", movement.Quantity);
            cmd.Parameters.AddWithValue("@Reason", (object?)movement.Reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UnitCost", movement.UnitCost);
            cmd.Parameters.AddWithValue("@TotalCost", movement.TotalCost);

            cmd.ExecuteNonQuery();
        }
    }

    public static void InsertExpense(Expense expense)
    {
        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"INSERT INTO Expense
(Id, Timestamp, Description, Amount, Type)
VALUES
(@Id, @Timestamp, @Description, @Amount, @Type);", Connection);

            cmd.Parameters.AddWithValue("@Id", expense.Id.ToString());
            cmd.Parameters.AddWithValue("@Timestamp", expense.Timestamp);
            cmd.Parameters.AddWithValue("@Description", expense.Description);
            cmd.Parameters.AddWithValue("@Amount", expense.Amount);
            cmd.Parameters.AddWithValue("@Type", expense.Type.ToString());

            cmd.ExecuteNonQuery();
        }
    }

    public static void InsertSale(SaleTransaction sale)
    {
        lock (_lock)
        {
            using var transaction = Connection.BeginTransaction();

            using (var cmd = new MySqlCommand(@"INSERT INTO SaleTransaction
(Id, Timestamp, PaymentMethod, TotalCost)
VALUES
(@Id, @Timestamp, @PaymentMethod, @TotalCost);", Connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Id", sale.Id.ToString());
                cmd.Parameters.AddWithValue("@Timestamp", sale.Timestamp);
                cmd.Parameters.AddWithValue("@PaymentMethod", sale.PaymentMethod);
                cmd.Parameters.AddWithValue("@TotalCost", sale.TotalCost);
                cmd.ExecuteNonQuery();
            }

            foreach (var item in sale.Items)
            {
                using var cmdItem = new MySqlCommand(@"INSERT INTO SaleItem
(ProductId, ProductName, Quantity, UnitPrice)
VALUES
(@ProductId, @ProductName, @Quantity, @UnitPrice);", Connection, transaction);

                cmdItem.Parameters.AddWithValue("@ProductId", item.ProductId.ToString());
                cmdItem.Parameters.AddWithValue("@ProductName", item.ProductName);
                cmdItem.Parameters.AddWithValue("@Quantity", item.Quantity);
                cmdItem.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);

                cmdItem.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    public static void InsertUser(AppUser user)
    {
        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"INSERT INTO AppUser
(Username, Password, Role, Email, Phone)
VALUES
(@Username, @Password, @Role, @Email, @Phone)
ON DUPLICATE KEY UPDATE
    Password = VALUES(Password),
    Role = VALUES(Role),
    Email = VALUES(Email),
    Phone = VALUES(Phone);", Connection);

            cmd.Parameters.AddWithValue("@Username", user.Username);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@Role", user.Role);
            cmd.Parameters.AddWithValue("@Email", (object?)user.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }

    public static void DeleteUser(string username)
    {
        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"DELETE FROM AppUser WHERE Username = @Username;", Connection);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.ExecuteNonQuery();
        }
    }
}
