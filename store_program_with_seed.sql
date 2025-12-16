-- store_program_with_seed.sql
-- Schema + data awal (seed) untuk aplikasi StoreProgram (MAUI)
-- Aman dijalankan di MySQL kosong. Untuk ulang dari nol, aktifkan bagian TRUNCATE di bawah.

CREATE DATABASE IF NOT EXISTS store_program
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE store_program;

-- =============================
-- 1) Schema
-- =============================

CREATE TABLE IF NOT EXISTS Product (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  Name VARCHAR(255) NOT NULL,
  Category VARCHAR(100) NOT NULL,
  Barcode VARCHAR(100) NULL,
  Unit VARCHAR(50) NOT NULL,
  SellPrice DECIMAL(18,2) NOT NULL,
  CostPrice DECIMAL(18,2) NOT NULL,
  DiscountPercent DECIMAL(5,2) NULL,
  INDEX IX_Product_Name (Name),
  INDEX IX_Product_Category (Category),
  INDEX IX_Product_Barcode (Barcode)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS StockBatch (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  ProductId CHAR(36) NOT NULL,
  Quantity INT NOT NULL,
  ExpiryDate DATE NOT NULL,
  UnitCost DECIMAL(18,2) NOT NULL,
  INDEX IX_StockBatch_ProductId (ProductId),
  INDEX IX_StockBatch_ExpiryDate (ExpiryDate),
  CONSTRAINT FK_StockBatch_Product
    FOREIGN KEY (ProductId) REFERENCES Product(Id)
    ON UPDATE RESTRICT ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS StockMovement (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  ProductId CHAR(36) NOT NULL,
  Timestamp DATETIME NOT NULL,
  Type VARCHAR(50) NOT NULL,
  Quantity INT NOT NULL,
  Reason VARCHAR(255) NULL,
  UnitCost DECIMAL(18,2) NOT NULL,
  TotalCost DECIMAL(18,2) NOT NULL,
  INDEX IX_StockMovement_ProductId (ProductId),
  INDEX IX_StockMovement_Timestamp (Timestamp),
  INDEX IX_StockMovement_Type (Type),
  CONSTRAINT FK_StockMovement_Product
    FOREIGN KEY (ProductId) REFERENCES Product(Id)
    ON UPDATE RESTRICT ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Expense (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  Timestamp DATETIME NOT NULL,
  Description VARCHAR(255) NOT NULL,
  Amount DECIMAL(18,2) NOT NULL,
  Type VARCHAR(50) NOT NULL,
  INDEX IX_Expense_Timestamp (Timestamp),
  INDEX IX_Expense_Type (Type)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS SaleTransaction (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  Timestamp DATETIME NOT NULL,
  PaymentMethod VARCHAR(50) NOT NULL,
  GrossAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
  TotalCost DECIMAL(18,2) NOT NULL,
  INDEX IX_SaleTransaction_Timestamp (Timestamp),
  INDEX IX_SaleTransaction_PaymentMethod (PaymentMethod)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS SaleItem (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  SaleId CHAR(36) NOT NULL,
  ProductId CHAR(36) NOT NULL,
  ProductName VARCHAR(255) NOT NULL,
  Quantity INT NOT NULL,
  UnitPrice DECIMAL(18,2) NOT NULL,
  INDEX IX_SaleItem_SaleId (SaleId),
  INDEX IX_SaleItem_ProductId (ProductId),
  CONSTRAINT FK_SaleItem_Sale
    FOREIGN KEY (SaleId) REFERENCES SaleTransaction(Id)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  CONSTRAINT FK_SaleItem_Product
    FOREIGN KEY (ProductId) REFERENCES Product(Id)
    ON UPDATE RESTRICT ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS AppUser (
  Username VARCHAR(100) NOT NULL PRIMARY KEY,
  Password VARCHAR(255) NOT NULL,
  Role VARCHAR(50) NOT NULL,
  Email VARCHAR(255) NULL,
  Phone VARCHAR(50) NULL,
  INDEX IX_AppUser_Role (Role)
) ENGINE=InnoDB;

-- Upgrade helper (kalau DB lama tidak punya kolom-kolom baru)
-- (Aman dijalankan walau kolom sudah ada)
SET @col_exists := (
  SELECT COUNT(*)
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'SaleTransaction'
    AND COLUMN_NAME = 'GrossAmount'
);
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `SaleTransaction` ADD COLUMN `GrossAmount` DECIMAL(18,2) NOT NULL DEFAULT 0;',
  'SELECT 1;'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================
-- 2) (Opsional) Reset data
-- =============================
-- Kalau kamu mau seed ulang dari nol, uncomment bagian ini.
-- Hati-hati: ini menghapus data.
--
-- SET FOREIGN_KEY_CHECKS = 0;
-- TRUNCATE TABLE SaleItem;
-- TRUNCATE TABLE SaleTransaction;
-- TRUNCATE TABLE StockMovement;
-- TRUNCATE TABLE StockBatch;
-- TRUNCATE TABLE Expense;
-- TRUNCATE TABLE Product;
-- TRUNCATE TABLE AppUser;
-- SET FOREIGN_KEY_CHECKS = 1;

-- =============================
-- 3) Seed data (akun, produk, stok, transaksi contoh)
-- =============================

-- 3.1 Users
INSERT INTO AppUser (Username, Password, Role, Email, Phone)
VALUES
  ('owner',    'owner123', 'Owner',    'owner@store.local',    '081234567890'),
  ('pegawai1', '123456',   'Employee', 'pegawai1@store.local', '081200000001')
ON DUPLICATE KEY UPDATE
  Password = VALUES(Password),
  Role = VALUES(Role),
  Email = VALUES(Email),
  Phone = VALUES(Phone);

-- 3.2 Produk (UUID dibuat statis supaya script bisa diulang tanpa duplikasi)
-- Format UUID: 8-4-4-4-12
SET @P_BERAS  = '11111111-1111-1111-1111-111111111111';
SET @P_GULA   = '22222222-2222-2222-2222-222222222222';
SET @P_MINYAK = '33333333-3333-3333-3333-333333333333';
SET @P_TEH    = '44444444-4444-4444-4444-444444444444';
SET @P_MIE    = '55555555-5555-5555-5555-555555555555';
SET @P_SUSU   = '66666666-6666-6666-6666-666666666666';

INSERT INTO Product (Id, Name, Category, Barcode, Unit, SellPrice, CostPrice, DiscountPercent)
VALUES
  (@P_BERAS,  'Beras Ramos 5kg',        'sembako', '899999900001', 'sak',  78000.00, 72000.00, NULL),
  (@P_GULA,   'Gula Pasir 1kg',         'sembako', '899999900002', 'kg',   17000.00, 15000.00, NULL),
  (@P_MINYAK, 'Minyak Goreng 1L',       'sembako', '899999900003', 'btl',  19000.00, 17000.00, NULL),
  (@P_TEH,    'Teh Botol 350ml',        'minuman', '899999900004', 'btl',   6000.00,  3500.00, NULL),
  (@P_MIE,    'Indomie Goreng',         'makanan', '899999900005', 'pcs',   3500.00,  2500.00, NULL),
  (@P_SUSU,   'Susu UHT 250ml',         'minuman', '899999900006', 'pcs',   8000.00,  6000.00, NULL)
ON DUPLICATE KEY UPDATE
  Name = VALUES(Name),
  Category = VALUES(Category),
  Barcode = VALUES(Barcode),
  Unit = VALUES(Unit),
  SellPrice = VALUES(SellPrice),
  CostPrice = VALUES(CostPrice),
  DiscountPercent = VALUES(DiscountPercent);

-- 3.3 Stok awal (batch)
-- Catatan: Quantity di batch sudah mencerminkan kondisi stok saat ini.
SET @B_BERAS  = 'aaaaaaa1-aaaa-aaaa-aaaa-aaaaaaaaaaa1';
SET @B_GULA   = 'aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaa2';
SET @B_MINYAK = 'aaaaaaa3-aaaa-aaaa-aaaa-aaaaaaaaaaa3';
SET @B_TEH    = 'aaaaaaa4-aaaa-aaaa-aaaa-aaaaaaaaaaa4';
SET @B_MIE    = 'aaaaaaa5-aaaa-aaaa-aaaa-aaaaaaaaaaa5';
SET @B_SUSU   = 'aaaaaaa6-aaaa-aaaa-aaaa-aaaaaaaaaaa6';

INSERT INTO StockBatch (Id, ProductId, Quantity, ExpiryDate, UnitCost)
VALUES
  (@B_BERAS,  @P_BERAS,  18, '2026-12-31', 72000.00),
  (@B_GULA,   @P_GULA,   45, '2026-10-31', 15000.00),
  (@B_MINYAK, @P_MINYAK, 40, '2026-08-31', 17000.00),
  (@B_TEH,    @P_TEH,    90, '2026-03-31',  3500.00),
  (@B_MIE,    @P_MIE,    85, '2026-06-30',  2500.00),
  (@B_SUSU,   @P_SUSU,   60, '2026-02-28',  6000.00)
ON DUPLICATE KEY UPDATE
  ProductId = VALUES(ProductId),
  Quantity = VALUES(Quantity),
  ExpiryDate = VALUES(ExpiryDate),
  UnitCost = VALUES(UnitCost);

-- 3.4 Stock movement (pembelian awal)
-- (Opsional tapi bagus untuk "Riwayat stok")
INSERT INTO StockMovement (Id, ProductId, Timestamp, Type, Quantity, Reason, UnitCost, TotalCost)
VALUES
  ('bbbbbbb1-bbbb-bbbb-bbbb-bbbbbbbbbbb1', @P_BERAS,  '2025-12-10 09:00:00', 'PurchaseIn',  20, 'Stok awal', 72000.00, 1440000.00),
  ('bbbbbbb2-bbbb-bbbb-bbbb-bbbbbbbbbbb2', @P_GULA,   '2025-12-10 09:10:00', 'PurchaseIn',  50, 'Stok awal', 15000.00,  750000.00),
  ('bbbbbbb3-bbbb-bbbb-bbbb-bbbbbbbbbbb3', @P_MINYAK, '2025-12-10 09:20:00', 'PurchaseIn',  40, 'Stok awal', 17000.00,  680000.00),
  ('bbbbbbb4-bbbb-bbbb-bbbb-bbbbbbbbbbb4', @P_TEH,    '2025-12-10 09:30:00', 'PurchaseIn', 100, 'Stok awal',  3500.00,  350000.00),
  ('bbbbbbb5-bbbb-bbbb-bbbb-bbbbbbbbbbb5', @P_MIE,    '2025-12-10 09:40:00', 'PurchaseIn', 100, 'Stok awal',  2500.00,  250000.00),
  ('bbbbbbb6-bbbb-bbbb-bbbb-bbbbbbbbbbb6', @P_SUSU,   '2025-12-10 09:50:00', 'PurchaseIn',  60, 'Stok awal',  6000.00,  360000.00);

-- 3.5 Expense contoh
INSERT INTO Expense (Id, Timestamp, Description, Amount, Type)
VALUES
  ('ccccccc1-cccc-cccc-cccc-ccccccccccc1', '2025-12-11 10:00:00', 'Biaya listrik toko', 150000.00, 'Operational'),
  ('ccccccc2-cccc-cccc-cccc-ccccccccccc2', '2025-12-12 13:00:00', 'Prive pemilik',     200000.00, 'Prive');

-- 3.6 Transaksi penjualan contoh + item
SET @S1 = 'ddddddd1-dddd-dddd-dddd-ddddddddddd1';
SET @S2 = 'ddddddd2-dddd-dddd-dddd-ddddddddddd2';

INSERT INTO SaleTransaction (Id, Timestamp, PaymentMethod, GrossAmount, TotalCost)
VALUES
  (@S1, '2025-12-15 14:05:00', 'Tunai',  3500.00 * 5,  2500.00 * 5),
  (@S2, '2025-12-15 15:30:00', 'QRIS',   6000.00 * 10, 3500.00 * 10)
ON DUPLICATE KEY UPDATE
  Timestamp = VALUES(Timestamp),
  PaymentMethod = VALUES(PaymentMethod),
  GrossAmount = VALUES(GrossAmount),
  TotalCost = VALUES(TotalCost);

INSERT INTO SaleItem (Id, SaleId, ProductId, ProductName, Quantity, UnitPrice)
VALUES
  ('eeeeeee1-eeee-eeee-eeee-eeeeeeeeeee1', @S1, @P_MIE, 'Indomie Goreng', 5, 3500.00),
  ('eeeeeee2-eeee-eeee-eeee-eeeeeeeeeee2', @S2, @P_TEH, 'Teh Botol 350ml', 10, 6000.00)
ON DUPLICATE KEY UPDATE
  SaleId = VALUES(SaleId),
  ProductId = VALUES(ProductId),
  ProductName = VALUES(ProductName),
  Quantity = VALUES(Quantity),
  UnitPrice = VALUES(UnitPrice);

-- 3.7 Stock movement untuk penjualan contoh (agar riwayat lengkap)
INSERT INTO StockMovement (Id, ProductId, Timestamp, Type, Quantity, Reason, UnitCost, TotalCost)
VALUES
  ('fffffff1-ffff-ffff-ffff-fffffffffff1', @P_MIE, '2025-12-15 14:05:00', 'SaleOut', 5, 'Penjualan', 2500.00, 12500.00),
  ('fffffff2-ffff-ffff-ffff-fffffffffff2', @P_TEH, '2025-12-15 15:30:00', 'SaleOut', 10, 'Penjualan', 3500.00, 35000.00);

-- Selesai.
