-- store_program_with_seed_testing.sql
-- Schema + seed data lengkap untuk TESTING StoreProgram
-- Tujuan data:
-- - Ada stok expired & mendekati kadaluarsa (<= 14 hari dari hari ini)
-- - Ada batch yang sudah diskon (DiscountPercent IS NOT NULL) dan yang belum
-- - Ada transaksi penjualan tersebar (hari ini/jam berbeda, beberapa hari lalu, awal bulan)
--
-- Catatan:
-- - Script ini melakukan TRUNCATE (reset) agar hasil konsisten untuk testing.
-- - Jika tidak mau menghapus data, comment bagian "RESET DATA".

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
  DiscountPercent DECIMAL(5,2) NULL,
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

-- =============================
-- 2) RESET DATA (untuk testing)
-- =============================

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE SaleItem;
TRUNCATE TABLE SaleTransaction;
TRUNCATE TABLE StockMovement;
TRUNCATE TABLE StockBatch;
TRUNCATE TABLE Expense;
TRUNCATE TABLE Product;
TRUNCATE TABLE AppUser;
SET FOREIGN_KEY_CHECKS = 1;

-- =============================
-- 3) Seed data
-- =============================

-- 3.1 Users
INSERT INTO AppUser (Username, Password, Role, Email, Phone)
VALUES
  ('owner',    'owner123', 'Owner',    'owner@store.local',    '081234567890'),
  ('pegawai1', '123456',   'Employee', 'pegawai1@store.local', '081200000001'),
  ('kasir',    'kasir123', 'Employee', 'kasir@store.local',    '081200000002');

-- 3.2 Produk (UUID statis)
SET @P_MIE   = '55555555-5555-5555-5555-555555555555';
SET @P_TEH   = '44444444-4444-4444-4444-444444444444';
SET @P_SUSU  = '66666666-6666-6666-6666-666666666666';
SET @P_YOG   = '77777777-7777-7777-7777-777777777777';
SET @P_ROTI  = '88888888-8888-8888-8888-888888888888';
SET @P_SOSIS = '99999999-9999-9999-9999-999999999999';
SET @P_AIR   = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
SET @P_KOPI  = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

INSERT INTO Product (Id, Name, Category, Barcode, Unit, SellPrice, CostPrice, DiscountPercent)
VALUES
  (@P_MIE,   'Indomie Goreng',            'makanan',  '899999900005', 'pcs',  3500.00,  2500.00, NULL),
  (@P_TEH,   'Teh Botol 350ml',           'minuman',  '899999900004', 'btl',  6000.00,  3500.00, NULL),
  (@P_SUSU,  'Susu UHT 250ml',            'minuman',  '899999900006', 'pcs',  8000.00,  6000.00, NULL),
  (@P_YOG,   'Yogurt Strawberry 100ml',   'minuman',  '899999900007', 'pcs', 12000.00,  8000.00, NULL),
  (@P_ROTI,  'Roti Tawar 400g',           'makanan',  '899999900008', 'pak', 16000.00, 11000.00, NULL),
  (@P_SOSIS, 'Sosis Ayam 500g',           'makanan',  '899999900009', 'pak', 28000.00, 20000.00, NULL),
  (@P_AIR,   'Air Mineral 600ml',         'minuman',  '899999900010', 'btl',  4000.00,  2000.00, NULL),
  (@P_KOPI,  'Kopi Sachet',               'minuman',  '899999900011', 'pcs',  2500.00,  1500.00, NULL);

-- Helper tanggal dinamis
SET @TODAY = CURDATE();
SET @MONTH_START = DATE_FORMAT(CURDATE(), '%Y-%m-01');

-- 3.3 Stock batch
-- Target testing:
-- - Expired: ExpiryDate < @TODAY
-- - Mendekati kadaluarsa: @TODAY <= ExpiryDate <= @TODAY + 14 hari
-- - Diskon: DiscountPercent IS NOT NULL

SET @B_MIE_NEAR  = 'c0000001-0000-0000-0000-000000000001';
SET @B_MIE_FAR   = 'c0000002-0000-0000-0000-000000000002';
SET @B_TEH_FAR   = 'c0000003-0000-0000-0000-000000000003';
SET @B_SUSU_WARN = 'c0000004-0000-0000-0000-000000000004';
SET @B_SUSU_FAR  = 'c0000005-0000-0000-0000-000000000005';
SET @B_YOG_WARN  = 'c0000006-0000-0000-0000-000000000006';
SET @B_YOG_FAR   = 'c0000007-0000-0000-0000-000000000007';
SET @B_ROTI_EXP  = 'c0000008-0000-0000-0000-000000000008';
SET @B_ROTI_NEAR = 'c0000009-0000-0000-0000-000000000009';
SET @B_SOSIS_FAR = 'c0000010-0000-0000-0000-000000000010';
SET @B_AIR_FAR   = 'c0000011-0000-0000-0000-000000000011';
SET @B_KOPI_FAR  = 'c0000012-0000-0000-0000-000000000012';

INSERT INTO StockBatch (Id, ProductId, Quantity, ExpiryDate, UnitCost, DiscountPercent)
VALUES
  -- Indomie: ada 1 batch mendekati kadaluarsa (diskon), 1 batch jauh (tanpa diskon)
  (@B_MIE_NEAR,  @P_MIE,   40, DATE_ADD(@TODAY, INTERVAL 10 DAY), 2500.00, 15.00),
  (@B_MIE_FAR,   @P_MIE,   80, DATE_ADD(@TODAY, INTERVAL 120 DAY), 2500.00, NULL),

  -- Teh: batch jauh
  (@B_TEH_FAR,   @P_TEH,   90, DATE_ADD(@TODAY, INTERVAL 180 DAY), 3500.00, NULL),

  -- Susu: 1 batch warning (diskon), 1 batch jauh
  (@B_SUSU_WARN, @P_SUSU,  12, DATE_ADD(@TODAY, INTERVAL 12 DAY), 6000.00, 20.00),
  (@B_SUSU_FAR,  @P_SUSU,  50, DATE_ADD(@TODAY, INTERVAL 200 DAY), 6000.00, NULL),

  -- Yogurt: 1 batch warning (diskon), 1 batch jauh
  (@B_YOG_WARN,  @P_YOG,   18, DATE_ADD(@TODAY, INTERVAL 7 DAY),  8000.00, 30.00),
  (@B_YOG_FAR,   @P_YOG,   20, DATE_ADD(@TODAY, INTERVAL 90 DAY), 8000.00, NULL),

  -- Roti: 1 batch expired (diskon), 1 batch near expiry (diskon)
  (@B_ROTI_EXP,  @P_ROTI,   6, DATE_SUB(@TODAY, INTERVAL 2 DAY), 11000.00, 50.00),
  (@B_ROTI_NEAR, @P_ROTI,  15, DATE_ADD(@TODAY, INTERVAL 3 DAY), 11000.00, 25.00),

  -- Sosis/Air/Kopi: jauh
  (@B_SOSIS_FAR, @P_SOSIS, 25, DATE_ADD(@TODAY, INTERVAL 60 DAY), 20000.00, NULL),
  (@B_AIR_FAR,   @P_AIR,  120, DATE_ADD(@TODAY, INTERVAL 365 DAY), 2000.00, NULL),
  (@B_KOPI_FAR,  @P_KOPI, 150, DATE_ADD(@TODAY, INTERVAL 300 DAY), 1500.00, NULL);

-- 3.4 Stock movement (PurchaseIn) - "riwayat" stok
-- Angka quantity di sini adalah contoh pembelian historis, sedangkan StockBatch.Quantity dianggap stok saat ini.
INSERT INTO StockMovement (Id, ProductId, Timestamp, Type, Quantity, Reason, UnitCost, TotalCost)
VALUES
  ('d0000001-0000-0000-0000-000000000001', @P_MIE,   TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 20 DAY), '09:00:00'), 'PurchaseIn', 150, 'Pembelian stok (testing)', 2500.00, 150 * 2500.00),
  ('d0000002-0000-0000-0000-000000000002', @P_TEH,   TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 18 DAY), '10:00:00'), 'PurchaseIn', 100, 'Pembelian stok (testing)', 3500.00, 100 * 3500.00),
  ('d0000003-0000-0000-0000-000000000003', @P_SUSU,  TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 15 DAY), '11:00:00'), 'PurchaseIn',  80, 'Pembelian stok (testing)', 6000.00,  80 * 6000.00),
  ('d0000004-0000-0000-0000-000000000004', @P_YOG,   TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 14 DAY), '12:00:00'), 'PurchaseIn',  60, 'Pembelian stok (testing)', 8000.00,  60 * 8000.00),
  ('d0000005-0000-0000-0000-000000000005', @P_ROTI,  TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 10 DAY), '13:00:00'), 'PurchaseIn',  40, 'Pembelian stok (testing)',11000.00,  40 * 11000.00),
  ('d0000006-0000-0000-0000-000000000006', @P_SOSIS, TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 9 DAY),  '14:00:00'), 'PurchaseIn',  30, 'Pembelian stok (testing)',20000.00,  30 * 20000.00),
  ('d0000007-0000-0000-0000-000000000007', @P_AIR,   TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 8 DAY),  '15:00:00'), 'PurchaseIn', 150, 'Pembelian stok (testing)', 2000.00, 150 * 2000.00),
  ('d0000008-0000-0000-0000-000000000008', @P_KOPI,  TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 7 DAY),  '16:00:00'), 'PurchaseIn', 200, 'Pembelian stok (testing)', 1500.00, 200 * 1500.00);

-- 3.5 Expense contoh
INSERT INTO Expense (Id, Timestamp, Description, Amount, Type)
VALUES
  ('e0000001-0000-0000-0000-000000000001', TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 3 DAY), '10:00:00'), 'Biaya listrik toko', 180000.00, 'Operational'),
  ('e0000002-0000-0000-0000-000000000002', TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 2 DAY), '09:30:00'), 'Biaya internet',      150000.00, 'Operational'),
  ('e0000003-0000-0000-0000-000000000003', TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 1 DAY), '18:00:00'), 'Prive pemilik',       250000.00, 'Prive'),
  ('e0000004-0000-0000-0000-000000000004', TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 5 DAY), '17:00:00'), 'Kerugian stok roti (expired)', 66000.00, 'DamagedLostStock');

-- 3.6 Transaksi penjualan (header + item)
-- Buat transaksi tersebar agar Grafik Harian/Mingguan/Bulanan terisi.
-- Harga diskon ditunjukkan lewat UnitPrice yang lebih rendah.

SET @S01 = 'f0000001-0000-0000-0000-000000000001';
SET @S02 = 'f0000002-0000-0000-0000-000000000002';
SET @S03 = 'f0000003-0000-0000-0000-000000000003';
SET @S04 = 'f0000004-0000-0000-0000-000000000004';
SET @S05 = 'f0000005-0000-0000-0000-000000000005';
SET @S06 = 'f0000006-0000-0000-0000-000000000006';
SET @S07 = 'f0000007-0000-0000-0000-000000000007';
SET @S08 = 'f0000008-0000-0000-0000-000000000008';
SET @S09 = 'f0000009-0000-0000-0000-000000000009';
SET @S10 = 'f0000010-0000-0000-0000-000000000010';

-- UnitPrice diskon contoh:
-- Yogurt (30% off): 12000 -> 8400
-- Susu (20% off):   8000  -> 6400
-- Mie (15% off):    3500  -> 2975
-- Roti (25% off):   16000 -> 12000

INSERT INTO SaleTransaction (Id, Timestamp, PaymentMethod, GrossAmount, TotalCost)
VALUES
  -- Hari ini (3 transaksi beda jam)
  (@S01, TIMESTAMP(@TODAY, '09:15:00'), 'Tunai', (8400.00*2) + (6400.00*1), (8000.00*2) + (6000.00*1)),
  (@S02, TIMESTAMP(@TODAY, '13:20:00'), 'QRIS',  (2975.00*5) + (6000.00*3), (2500.00*5) + (3500.00*3)),
  (@S03, TIMESTAMP(@TODAY, '19:05:00'), 'Tunai', (12000.00*2) + (4000.00*4), (11000.00*2) + (2000.00*4)),

  -- Kemarin
  (@S04, TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 1 DAY), '11:10:00'), 'Tunai', (8000.00*2) + (2500.00*6), (6000.00*2) + (1500.00*6)),

  -- Minggu ini (beberapa hari lalu)
  (@S05, TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 2 DAY), '16:40:00'), 'QRIS',  (28000.00*1) + (6000.00*5), (20000.00*1) + (3500.00*5)),
  (@S06, TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 3 DAY), '09:05:00'), 'Tunai', (4000.00*10),              (2000.00*10)),
  (@S07, TIMESTAMP(DATE_SUB(@TODAY, INTERVAL 6 DAY), '20:30:00'), 'QRIS',  (2975.00*8) + (6400.00*2), (2500.00*8) + (6000.00*2)),

  -- Awal bulan (agar chart bulanan ada data)
  (@S08, TIMESTAMP(DATE_ADD(@MONTH_START, INTERVAL 2 DAY), '10:00:00'), 'Tunai', (6000.00*6) + (4000.00*6), (3500.00*6) + (2000.00*6)),
  (@S09, TIMESTAMP(DATE_ADD(@MONTH_START, INTERVAL 7 DAY), '14:25:00'), 'QRIS',  (8400.00*3) + (2500.00*10), (8000.00*3) + (1500.00*10)),
  (@S10, TIMESTAMP(DATE_ADD(@MONTH_START, INTERVAL 12 DAY), '18:15:00'), 'Tunai', (12000.00*1) + (28000.00*2), (11000.00*1) + (20000.00*2));

INSERT INTO SaleItem (Id, SaleId, ProductId, ProductName, Quantity, UnitPrice)
VALUES
  -- S01
  ('fa000001-0000-0000-0000-000000000001', @S01, @P_YOG,  'Yogurt Strawberry 100ml', 2, 8400.00),
  ('fa000002-0000-0000-0000-000000000002', @S01, @P_SUSU, 'Susu UHT 250ml',          1, 6400.00),

  -- S02
  ('fa000003-0000-0000-0000-000000000003', @S02, @P_MIE,  'Indomie Goreng',          5, 2975.00),
  ('fa000004-0000-0000-0000-000000000004', @S02, @P_TEH,  'Teh Botol 350ml',         3, 6000.00),

  -- S03
  ('fa000005-0000-0000-0000-000000000005', @S03, @P_ROTI, 'Roti Tawar 400g',         2, 12000.00),
  ('fa000006-0000-0000-0000-000000000006', @S03, @P_AIR,  'Air Mineral 600ml',       4, 4000.00),

  -- S04
  ('fa000007-0000-0000-0000-000000000007', @S04, @P_SUSU, 'Susu UHT 250ml',          2, 8000.00),
  ('fa000008-0000-0000-0000-000000000008', @S04, @P_KOPI, 'Kopi Sachet',             6, 2500.00),

  -- S05
  ('fa000009-0000-0000-0000-000000000009', @S05, @P_SOSIS,'Sosis Ayam 500g',         1, 28000.00),
  ('fa000010-0000-0000-0000-000000000010', @S05, @P_TEH,  'Teh Botol 350ml',         5, 6000.00),

  -- S06
  ('fa000011-0000-0000-0000-000000000011', @S06, @P_AIR,  'Air Mineral 600ml',      10, 4000.00),

  -- S07
  ('fa000012-0000-0000-0000-000000000012', @S07, @P_MIE,  'Indomie Goreng',          8, 2975.00),
  ('fa000013-0000-0000-0000-000000000013', @S07, @P_SUSU, 'Susu UHT 250ml',          2, 6400.00),

  -- S08
  ('fa000014-0000-0000-0000-000000000014', @S08, @P_TEH,  'Teh Botol 350ml',         6, 6000.00),
  ('fa000015-0000-0000-0000-000000000015', @S08, @P_AIR,  'Air Mineral 600ml',       6, 4000.00),

  -- S09
  ('fa000016-0000-0000-0000-000000000016', @S09, @P_YOG,  'Yogurt Strawberry 100ml', 3, 8400.00),
  ('fa000017-0000-0000-0000-000000000017', @S09, @P_KOPI, 'Kopi Sachet',            10, 2500.00),

  -- S10
  ('fa000018-0000-0000-0000-000000000018', @S10, @P_ROTI, 'Roti Tawar 400g',         1, 12000.00),
  ('fa000019-0000-0000-0000-000000000019', @S10, @P_SOSIS,'Sosis Ayam 500g',         2, 28000.00);

-- 3.7 Stock movement untuk penjualan (optional, biar riwayat makin lengkap)
INSERT INTO StockMovement (Id, ProductId, Timestamp, Type, Quantity, Reason, UnitCost, TotalCost)
VALUES
  ('fb000001-0000-0000-0000-000000000001', @P_YOG,  TIMESTAMP(@TODAY, '09:15:00'), 'SaleOut', 2, 'Penjualan (testing)', 8000.00, 2*8000.00),
  ('fb000002-0000-0000-0000-000000000002', @P_SUSU, TIMESTAMP(@TODAY, '09:15:00'), 'SaleOut', 1, 'Penjualan (testing)', 6000.00, 1*6000.00),
  ('fb000003-0000-0000-0000-000000000003', @P_MIE,  TIMESTAMP(@TODAY, '13:20:00'), 'SaleOut', 5, 'Penjualan (testing)', 2500.00, 5*2500.00),
  ('fb000004-0000-0000-0000-000000000004', @P_TEH,  TIMESTAMP(@TODAY, '13:20:00'), 'SaleOut', 3, 'Penjualan (testing)', 3500.00, 3*3500.00),
  ('fb000005-0000-0000-0000-000000000005', @P_ROTI, TIMESTAMP(@TODAY, '19:05:00'), 'SaleOut', 2, 'Penjualan (testing)',11000.00, 2*11000.00),
  ('fb000006-0000-0000-0000-000000000006', @P_AIR,  TIMESTAMP(@TODAY, '19:05:00'), 'SaleOut', 4, 'Penjualan (testing)', 2000.00, 4*2000.00);

-- Selesai.
