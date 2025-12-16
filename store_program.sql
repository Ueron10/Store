-- store_program.sql
-- Schema MySQL untuk aplikasi StoreProgram (MAUI)
-- Jalankan file ini di MySQL (mis. via MySQL Workbench / phpMyAdmin / CLI).

-- 1) Database
CREATE DATABASE IF NOT EXISTS store_program
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE store_program;

-- 2) Tabel master produk
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

-- 3) Batch stok (FIFO berdasarkan ExpiryDate)
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

-- 4) Log pergerakan stok
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

-- 5) Pengeluaran
CREATE TABLE IF NOT EXISTS Expense (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  Timestamp DATETIME NOT NULL,
  Description VARCHAR(255) NOT NULL,
  Amount DECIMAL(18,2) NOT NULL,
  Type VARCHAR(50) NOT NULL,

  INDEX IX_Expense_Timestamp (Timestamp),
  INDEX IX_Expense_Type (Type)
) ENGINE=InnoDB;

-- 6) Header transaksi penjualan
-- GrossAmount disimpan agar laporan tetap konsisten meskipun item belum termuat (fallback).
CREATE TABLE IF NOT EXISTS SaleTransaction (
  Id CHAR(36) NOT NULL PRIMARY KEY,
  Timestamp DATETIME NOT NULL,
  PaymentMethod VARCHAR(50) NOT NULL,
  GrossAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
  TotalCost DECIMAL(18,2) NOT NULL,

  INDEX IX_SaleTransaction_Timestamp (Timestamp),
  INDEX IX_SaleTransaction_PaymentMethod (PaymentMethod)
) ENGINE=InnoDB;

-- 7) Detail item transaksi
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

-- 8) User aplikasi
CREATE TABLE IF NOT EXISTS AppUser (
  Username VARCHAR(100) NOT NULL PRIMARY KEY,
  Password VARCHAR(255) NOT NULL,
  Role VARCHAR(50) NOT NULL,
  Email VARCHAR(255) NULL,
  Phone VARCHAR(50) NULL,

  INDEX IX_AppUser_Role (Role)
) ENGINE=InnoDB;

-- (Opsional) Seed user owner. Ubah password sesuai kebutuhan.
-- INSERT INTO AppUser (Username, Password, Role, Email, Phone)
-- VALUES ('owner', 'owner123', 'Owner', NULL, NULL)
-- ON DUPLICATE KEY UPDATE Password=VALUES(Password), Role=VALUES(Role);
