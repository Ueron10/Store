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

    private static bool _schemaEnsured;

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

                // Pastikan tabel/kolom yang dibutuhkan sudah ada agar aplikasi tidak error saat query.
                // Ini juga membantu kasus "fitur belum dibuat" (mis. tabel belum dibuat di MySQL).
                EnsureSchema(_connection);

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

    #region Schema bootstrap / upgrade

    /// <summary>
    /// Membuat tabel/kolom yang dibutuhkan jika belum ada.
    /// Tujuan: aplikasi langsung jalan di MySQL kosong tanpa perlu setup manual yang rawan salah.
    /// </summary>
    private static void EnsureSchema(MySqlConnection conn)
    {
        // Jalankan sekali per proses.
        if (_schemaEnsured) return;

        lock (_lock)
        {
            if (_schemaEnsured) return;

            // CREATE TABLE jika belum ada
            ExecuteNonQuery(conn, @"
CREATE TABLE IF NOT EXISTS Product (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  Name VARCHAR(255) NOT NULL,
  Category VARCHAR(100) NOT NULL,
  Barcode VARCHAR(100) NULL,
  Unit VARCHAR(50) NOT NULL,
  SellPrice DECIMAL(18,2) NOT NULL,
  CostPrice DECIMAL(18,2) NOT NULL,
  DiscountPercent DECIMAL(5,2) NULL
);");

            ExecuteNonQuery(conn, @"
CREATE TABLE IF NOT EXISTS StockBatch (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  ProductId CHAR(36) NOT NULL,
  Quantity INT NOT NULL,
  PurchaseDate DATETIME NOT NULL,
  ExpiryDate DATE NOT NULL,
  UnitCost DECIMAL(18,2) NOT NULL,
  DiscountPercent DECIMAL(5,2) NULL
);");

            ExecuteNonQuery(conn, @"
CREATE TABLE IF NOT EXISTS StockMovement (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  ProductId CHAR(36) NOT NULL,
  Timestamp DATETIME NOT NULL,
  Type VARCHAR(50) NOT NULL,
  Quantity INT NOT NULL,
  Reason VARCHAR(255) NULL,
  UnitCost DECIMAL(18,2) NOT NULL,
  TotalCost DECIMAL(18,2) NOT NULL
);");

            ExecuteNonQuery(conn, @"
CREATE TABLE IF NOT EXISTS Expense (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  Timestamp DATETIME NOT NULL,
  Description VARCHAR(255) NOT NULL,
  Amount DECIMAL(18,2) NOT NULL,
  Type VARCHAR(50) NOT NULL
);");

            // SaleTransaction: simpan GrossAmount agar setelah restart laporan tetap benar
            ExecuteNonQuery(conn, @"
CREATE TABLE IF NOT EXISTS SaleTransaction (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  Timestamp DATETIME NOT NULL,
  PaymentMethod VARCHAR(50) NOT NULL,
  GrossAmount DECIMAL(18,2) NOT NULL,
  TotalCost DECIMAL(18,2) NOT NULL
);");

            ExecuteNonQuery(conn, @"
CREATE TABLE IF NOT EXISTS SaleItem (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  SaleId CHAR(36) NOT NULL,
  ProductId CHAR(36) NOT NULL,
  ProductName VARCHAR(255) NOT NULL,
  Quantity INT NOT NULL,
  UnitPrice DECIMAL(18,2) NOT NULL
);");

            ExecuteNonQuery(conn, @"
CREATE TABLE IF NOT EXISTS AppUser (
  Username VARCHAR(100) NOT NULL PRIMARY KEY,
  Password VARCHAR(255) NOT NULL,
  Role VARCHAR(50) NOT NULL,
  Email VARCHAR(255) NULL,
  Phone VARCHAR(50) NULL
);");

            // Upgrade kolom yang mungkin belum ada (untuk DB yang sudah dibuat versi lama)
            EnsureColumn(conn, table: "SaleTransaction", column: "GrossAmount", columnDefinition: "DECIMAL(18,2) NOT NULL DEFAULT 0");
            EnsureColumn(conn, table: "SaleItem", column: "SaleId", columnDefinition: "CHAR(36) NOT NULL");
            EnsureColumn(conn, table: "SaleItem", column: "Id", columnDefinition: "CHAR(36) NOT NULL");
            EnsureColumn(conn, table: "StockBatch", column: "PurchaseDate", columnDefinition: "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");
            EnsureColumn(conn, table: "StockBatch", column: "DiscountPercent", columnDefinition: "DECIMAL(5,2) NULL");

            _schemaEnsured = true;
        }
    }

    private static void ExecuteNonQuery(MySqlConnection conn, string sql)
    {
        using var cmd = new MySqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private static void EnsureColumn(MySqlConnection conn, string table, string column, string columnDefinition)
    {
        using var checkCmd = new MySqlCommand(@"
SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @Table
  AND COLUMN_NAME = @Column;", conn);
        checkCmd.Parameters.AddWithValue("@Table", table);
        checkCmd.Parameters.AddWithValue("@Column", column);

        var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
        if (exists) return;

        using var alter = new MySqlCommand($"ALTER TABLE `{table}` ADD COLUMN `{column}` {columnDefinition};", conn);
        alter.ExecuteNonQuery();
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
    Id, ProductId, Quantity, PurchaseDate, ExpiryDate, UnitCost, DiscountPercent
FROM StockBatch;", Connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var b = new StockBatch
                {
                    Id = ReadGuid(reader, "Id"),
                    ProductId = ReadGuid(reader, "ProductId"),
                    Quantity = reader.GetInt32("Quantity"),
                    PurchaseDate = reader.GetDateTime("PurchaseDate"),
                    ExpiryDate = DateOnly.FromDateTime(reader.GetDateTime("ExpiryDate")),
                    UnitCost = reader.GetDecimal("UnitCost"),
                    DiscountPercent = reader.IsDBNull(reader.GetOrdinal("DiscountPercent"))
                        ? null
                        : reader.GetDecimal("DiscountPercent")
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

            using (var cmd = new MySqlCommand(@"SELECT
    Id, Timestamp, PaymentMethod, GrossAmount, TotalCost
FROM SaleTransaction;", Connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var s = new SaleTransaction
                    {
                        Id = ReadGuid(reader, "Id"),
                        Timestamp = reader.GetDateTime("Timestamp"),
                        PaymentMethod = reader.GetString("PaymentMethod"),
                        TotalCost = reader.GetDecimal("TotalCost"),
                        Items = new List<SaleItem>()
                    };

                    // NOTE: GrossAmount adalah computed property dari Items, jadi tidak diset di model.
                    // Kolom GrossAmount tetap dibaca di query agar kompatibel dan untuk validasi schema.
                    list.Add(s);
                }
            }

            // Load detail item untuk semua transaksi (agar laporan/top products tetap benar setelah restart)
            var itemsBySaleId = GetSaleItemsBySaleIds(list.Select(s => s.Id).ToList());
            foreach (var sale in list)
            {
                if (itemsBySaleId.TryGetValue(sale.Id, out var items))
                {
                    sale.Items = items;
                }
            }

            return list;
        }
    }

    private static Dictionary<Guid, List<SaleItem>> GetSaleItemsBySaleIds(IReadOnlyList<Guid> saleIds)
    {
        var result = new Dictionary<Guid, List<SaleItem>>();
        if (saleIds.Count == 0) return result;

        // Buat parameter IN (@id0,@id1,...)
        var paramNames = new List<string>(saleIds.Count);
        for (int i = 0; i < saleIds.Count; i++)
        {
            paramNames.Add("@id" + i);
        }

        var sql = $@"SELECT
    Id, SaleId, ProductId, ProductName, Quantity, UnitPrice
FROM SaleItem
WHERE SaleId IN ({string.Join(",", paramNames)});";

        using var cmd = new MySqlCommand(sql, Connection);
        for (int i = 0; i < saleIds.Count; i++)
        {
            cmd.Parameters.AddWithValue(paramNames[i], saleIds[i].ToString());
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var saleId = ReadGuid(reader, "SaleId");
            var item = new SaleItem
            {
                Id = ReadGuid(reader, "Id"),
                SaleId = saleId,
                ProductId = ReadGuid(reader, "ProductId"),
                ProductName = reader.GetString("ProductName"),
                Quantity = reader.GetInt32("Quantity"),
                UnitPrice = reader.GetDecimal("UnitPrice")
            };

            if (!result.TryGetValue(saleId, out var list))
            {
                list = new List<SaleItem>();
                result[saleId] = list;
            }

            list.Add(item);
        }

        return result;
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

    /// <summary>
    /// Dipakai untuk fitur "Lupa Password" (assignment).
    /// Mengembalikan password jika username+email cocok.
    /// </summary>
    public static string? GetPasswordByUsernameAndEmail(string username, string email)
    {
        username = (username ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            return null;

        lock (_lock)
        {
            using var cmd = new MySqlCommand(@"SELECT Password
FROM AppUser
WHERE LOWER(Username) = LOWER(@Username)
  AND Email IS NOT NULL
  AND LOWER(Email) = LOWER(@Email)
LIMIT 1;", Connection);

            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);

            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : Convert.ToString(result);
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
(Id, ProductId, Quantity, PurchaseDate, ExpiryDate, UnitCost, DiscountPercent)
VALUES
(@Id, @ProductId, @Quantity, @PurchaseDate, @ExpiryDate, @UnitCost, @DiscountPercent);", Connection);

            cmd.Parameters.AddWithValue("@Id", batch.Id.ToString());
            cmd.Parameters.AddWithValue("@ProductId", batch.ProductId.ToString());
            cmd.Parameters.AddWithValue("@Quantity", batch.Quantity);
            cmd.Parameters.AddWithValue("@PurchaseDate", batch.PurchaseDate);
            cmd.Parameters.AddWithValue("@ExpiryDate", batch.ExpiryDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@UnitCost", batch.UnitCost);
            cmd.Parameters.AddWithValue("@DiscountPercent", (object?)batch.DiscountPercent ?? DBNull.Value);

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
    PurchaseDate = @PurchaseDate,
    ExpiryDate = @ExpiryDate,
    UnitCost = @UnitCost,
    DiscountPercent = @DiscountPercent
WHERE Id = @Id;", Connection);

            cmd.Parameters.AddWithValue("@Id", batch.Id.ToString());
            cmd.Parameters.AddWithValue("@ProductId", batch.ProductId.ToString());
            cmd.Parameters.AddWithValue("@Quantity", batch.Quantity);
            cmd.Parameters.AddWithValue("@PurchaseDate", batch.PurchaseDate);
            cmd.Parameters.AddWithValue("@ExpiryDate", batch.ExpiryDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@UnitCost", batch.UnitCost);
            cmd.Parameters.AddWithValue("@DiscountPercent", (object?)batch.DiscountPercent ?? DBNull.Value);

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
(Id, Timestamp, PaymentMethod, GrossAmount, TotalCost)
VALUES
(@Id, @Timestamp, @PaymentMethod, @GrossAmount, @TotalCost);", Connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Id", sale.Id.ToString());
                cmd.Parameters.AddWithValue("@Timestamp", sale.Timestamp);
                cmd.Parameters.AddWithValue("@PaymentMethod", sale.PaymentMethod);
                cmd.Parameters.AddWithValue("@GrossAmount", sale.GrossAmount);
                cmd.Parameters.AddWithValue("@TotalCost", sale.TotalCost);
                cmd.ExecuteNonQuery();
            }

            foreach (var item in sale.Items)
            {
                // Pastikan FK terset untuk persist
                if (item.SaleId == Guid.Empty)
                    item.SaleId = sale.Id;

                using var cmdItem = new MySqlCommand(@"INSERT INTO SaleItem
(Id, SaleId, ProductId, ProductName, Quantity, UnitPrice)
VALUES
(@Id, @SaleId, @ProductId, @ProductName, @Quantity, @UnitPrice);", Connection, transaction);

                cmdItem.Parameters.AddWithValue("@Id", item.Id.ToString());
                cmdItem.Parameters.AddWithValue("@SaleId", item.SaleId.ToString());
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
