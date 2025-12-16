Param(
  [string]$OutDir = "reports_real",
  [string]$Server = "localhost",
  [int]$Port = 3306,
  [string]$Database = "store_program",
  [string]$User = "root",
  [SecureString]$Password,
  [string]$MySqlDataDllPath = "bin\Debug\net9.0-windows10.0.19041.0\win10-x64\MySql.Data.dll"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Ensure-Dir([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType Directory -Path $Path | Out-Null
  }
}

function Write-CsvUtf8([object[]]$Rows, [string]$Path) {
  $csv = $Rows | ConvertTo-Csv -NoTypeInformation

  $fullPath = [System.IO.Path]::GetFullPath($Path)
  $parent = [System.IO.Path]::GetDirectoryName($fullPath)
  if (-not [string]::IsNullOrWhiteSpace($parent)) { Ensure-Dir $parent }

  [System.IO.File]::WriteAllLines($fullPath, $csv, (New-Object System.Text.UTF8Encoding($true)))
}

function SecureString-ToPlain([SecureString]$s) {
  if ($null -eq $s) { return "" }
  $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($s)
  try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
  finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

function Load-MySqlAssembly([string]$Path) {
  $full = [System.IO.Path]::GetFullPath($Path)
  if (-not (Test-Path -LiteralPath $full)) {
    throw "MySql.Data.dll tidak ditemukan di: $full. Pastikan project sudah di-build atau isi parameter -MySqlDataDllPath."
  }
  Add-Type -Path $full | Out-Null
}

function Invoke-MySqlQuery([string]$ConnStr, [string]$Sql) {
  $conn = New-Object MySql.Data.MySqlClient.MySqlConnection($ConnStr)
  try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql

    $da = New-Object MySql.Data.MySqlClient.MySqlDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    [void]$da.Fill($dt)
    return $dt
  }
  finally {
    if ($conn.State -ne [System.Data.ConnectionState]::Closed) { $conn.Close() }
    $conn.Dispose()
  }
}

function DataTable-ToObjects([System.Data.DataTable]$dt) {
  $rows = @()
  foreach ($r in $dt.Rows) {
    $o = [ordered]@{}
    foreach ($c in $dt.Columns) {
      $val = $r[$c.ColumnName]
      if ($val -eq [DBNull]::Value) { $val = $null }
      $o[$c.ColumnName] = $val
    }
    $rows += [pscustomobject]$o
  }
  return $rows
}

# ---- password prompt (kalau belum diisi) ----
if ($null -eq $Password) {
  $Password = Read-Host -AsSecureString "MySQL Password untuk user '$User' (kosong kalau tidak ada)"
}

Load-MySqlAssembly $MySqlDataDllPath

$plain = SecureString-ToPlain $Password

# NOTE: tidak mencetak password
$connStr = "Server=$Server;Port=$Port;Database=$Database;Uid=$User;Pwd=$plain;SslMode=None;Allow User Variables=True;"

Ensure-Dir $OutDir
$resolvedOut = (Resolve-Path -LiteralPath $OutDir).Path

# =============================
# 1) Barang masuk/keluar (StockMovement)
# =============================
$sqlMovements = @"
SELECT
  m.Timestamp AS TanggalWaktu,
  DATE(m.Timestamp) AS Tanggal,
  TIME_FORMAT(m.Timestamp, '%H:%i:%s') AS Jam,
  p.Name AS Produk,
  p.Category AS Kategori,
  m.Type AS Tipe,
  CASE
    WHEN m.Type IN ('PurchaseIn','AdjustmentIn','StockOpnameAdjustment') THEN 'Masuk'
    WHEN m.Type IN ('SaleOut','AdjustmentOut') THEN 'Keluar'
    ELSE 'Lainnya'
  END AS Arah,
  m.Quantity AS Qty,
  p.Unit AS Satuan,
  m.UnitCost AS UnitCost,
  m.TotalCost AS TotalCost,
  m.Reason AS Alasan
FROM StockMovement m
JOIN Product p ON p.Id = m.ProductId
ORDER BY m.Timestamp;
"@

$dtMov = Invoke-MySqlQuery -ConnStr $connStr -Sql $sqlMovements
$rowsMov = DataTable-ToObjects $dtMov
Write-CsvUtf8 -Rows $rowsMov -Path (Join-Path $resolvedOut 'barang_masuk_keluar.csv')

# =============================
# 2) Batch (StockBatch)
# =============================
$sqlBatches = @"
SELECT
  p.Name AS Produk,
  p.Category AS Kategori,
  b.Id AS BatchId,
  b.PurchaseDate AS PurchaseDate,
  DATE(b.ExpiryDate) AS ExpiryDate,
  CASE
    WHEN b.ExpiryDate < CURDATE() THEN 'EXPIRED'
    WHEN b.ExpiryDate <= DATE_ADD(CURDATE(), INTERVAL 14 DAY) THEN 'NEAR_EXPIRY'
    ELSE 'OK'
  END AS StatusExpiry,
  b.Quantity AS Qty,
  p.Unit AS Satuan,
  b.UnitCost AS UnitCost,
  b.DiscountPercent AS DiscountPercent
FROM StockBatch b
JOIN Product p ON p.Id = b.ProductId
ORDER BY p.Name, b.ExpiryDate, b.PurchaseDate;
"@

$dtBatch = Invoke-MySqlQuery -ConnStr $connStr -Sql $sqlBatches
$rowsBatch = DataTable-ToObjects $dtBatch
Write-CsvUtf8 -Rows $rowsBatch -Path (Join-Path $resolvedOut 'barang_batch.csv')

# =============================
# 3) Summary per produk + TOTAL
# =============================
$sqlSummary = @"
SELECT
  p.Id AS ProductId,
  p.Name AS Produk,
  p.Category AS Kategori,
  p.Unit AS Satuan,
  p.SellPrice AS SellPrice,
  p.CostPrice AS CostPrice,

  COALESCE(SUM(CASE WHEN m.Type IN ('PurchaseIn','AdjustmentIn','StockOpnameAdjustment') THEN m.Quantity ELSE 0 END), 0) AS TotalMasuk,
  COALESCE(SUM(CASE WHEN m.Type IN ('SaleOut','AdjustmentOut') THEN m.Quantity ELSE 0 END), 0) AS TotalKeluar,

  MIN(CASE WHEN m.Type IN ('PurchaseIn','AdjustmentIn','StockOpnameAdjustment') THEN m.Timestamp ELSE NULL END) AS TglMasukPertama,
  MAX(CASE WHEN m.Type IN ('PurchaseIn','AdjustmentIn','StockOpnameAdjustment') THEN m.Timestamp ELSE NULL END) AS TglMasukTerakhir,
  MAX(CASE WHEN m.Type IN ('SaleOut','AdjustmentOut') THEN m.Timestamp ELSE NULL END) AS TglKeluarTerakhir,

  COALESCE((SELECT SUM(b.Quantity) FROM StockBatch b WHERE b.ProductId = p.Id), 0) AS StokSaatIni
FROM Product p
LEFT JOIN StockMovement m ON m.ProductId = p.Id
GROUP BY p.Id, p.Name, p.Category, p.Unit, p.SellPrice, p.CostPrice
ORDER BY p.Name;
"@

$dtSum = Invoke-MySqlQuery -ConnStr $connStr -Sql $sqlSummary
$rowsSum = DataTable-ToObjects $dtSum

# Tambah nilai stok
$summaryWithValue = @()
foreach ($r in $rowsSum) {
  $stock = [int]$r.StokSaatIni
  $sell = [decimal]$r.SellPrice
  $cost = [decimal]$r.CostPrice

  $obj = [ordered]@{}
  foreach ($k in $r.PSObject.Properties.Name) { $obj[$k] = $r.$k }

  $obj['NilaiStokModal'] = $cost * $stock
  $obj['NilaiStokJual'] = $sell * $stock
  $summaryWithValue += [pscustomobject]$obj
}

# TOTAL keseluruhan
$totalMasuk = 0
$totalKeluar = 0
$totalStok = 0
$totalModal = 0
$totalJual = 0
foreach ($r in $summaryWithValue) {
  $totalMasuk += [int]$r.TotalMasuk
  $totalKeluar += [int]$r.TotalKeluar
  $totalStok += [int]$r.StokSaatIni
  $totalModal += [decimal]$r.NilaiStokModal
  $totalJual += [decimal]$r.NilaiStokJual
}

$summaryWithValue += [pscustomobject]@{
  ProductId = ''
  Produk = 'TOTAL'
  Kategori = ''
  Satuan = ''
  SellPrice = ''
  CostPrice = ''
  TotalMasuk = $totalMasuk
  TotalKeluar = $totalKeluar
  TglMasukPertama = ''
  TglMasukTerakhir = ''
  TglKeluarTerakhir = ''
  StokSaatIni = $totalStok
  NilaiStokModal = $totalModal
  NilaiStokJual = $totalJual
}

Write-CsvUtf8 -Rows $summaryWithValue -Path (Join-Path $resolvedOut 'barang_summary.csv')

# README
$info = @(
  "Laporan dibuat: $([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))",
  "Sumber data: MySQL database '$Database' di $Server:$Port",
  "File:",
  "- barang_masuk_keluar.csv (tgl barang masuk/keluar dari StockMovement)",
  "- barang_batch.csv (tgl barang masuk per batch dari StockBatch.PurchaseDate + expiry)",
  "- barang_summary.csv (rekap qty masuk/keluar, stok, nilai stok, total keseluruhan)"
)
[System.IO.File]::WriteAllLines((Join-Path $resolvedOut 'README.txt'), $info, (New-Object System.Text.UTF8Encoding($true)))

Write-Host "OK: CSV reports generated in $resolvedOut"