Param(
  [string]$OutDir = "reports",
  [datetime]$Today = (Get-Date)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Ensure-Dir([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType Directory -Path $Path | Out-Null
  }
}

function Write-CsvUtf8([object[]]$Rows, [string]$Path) {
  # Export-Csv default encoding differs across PS versions; force UTF-8.
  $csv = $Rows | ConvertTo-Csv -NoTypeInformation

  $fullPath = [System.IO.Path]::GetFullPath($Path)
  $parent = [System.IO.Path]::GetDirectoryName($fullPath)
  if (-not [string]::IsNullOrWhiteSpace($parent)) {
    Ensure-Dir $parent
  }

  [System.IO.File]::WriteAllLines($fullPath, $csv, (New-Object System.Text.UTF8Encoding($true)))
}

# =============================
# Dataset: mirror store_program_with_seed_testing.sql
# =============================

$todayDate = $Today.Date
$monthStart = Get-Date -Year $todayDate.Year -Month $todayDate.Month -Day 1

$products = @(
  [pscustomobject]@{ Id='55555555-5555-5555-5555-555555555555'; Name='Indomie Goreng';          Category='makanan'; Barcode='899999900005'; Unit='pcs'; SellPrice=3500;  CostPrice=2500; DiscountPercent=$null },
  [pscustomobject]@{ Id='44444444-4444-4444-4444-444444444444'; Name='Teh Botol 350ml';         Category='minuman'; Barcode='899999900004'; Unit='btl'; SellPrice=6000;  CostPrice=3500; DiscountPercent=$null },
  [pscustomobject]@{ Id='66666666-6666-6666-6666-666666666666'; Name='Susu UHT 250ml';          Category='minuman'; Barcode='899999900006'; Unit='pcs'; SellPrice=8000;  CostPrice=6000; DiscountPercent=$null },
  [pscustomobject]@{ Id='77777777-7777-7777-7777-777777777777'; Name='Yogurt Strawberry 100ml'; Category='minuman'; Barcode='899999900007'; Unit='pcs'; SellPrice=12000; CostPrice=8000; DiscountPercent=$null },
  [pscustomobject]@{ Id='88888888-8888-8888-8888-888888888888'; Name='Roti Tawar 400g';         Category='makanan'; Barcode='899999900008'; Unit='pak'; SellPrice=16000; CostPrice=11000; DiscountPercent=$null },
  [pscustomobject]@{ Id='99999999-9999-9999-9999-999999999999'; Name='Sosis Ayam 500g';         Category='makanan'; Barcode='899999900009'; Unit='pak'; SellPrice=28000; CostPrice=20000; DiscountPercent=$null },
  [pscustomobject]@{ Id='aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'; Name='Air Mineral 600ml';       Category='minuman'; Barcode='899999900010'; Unit='btl'; SellPrice=4000;  CostPrice=2000; DiscountPercent=$null },
  [pscustomobject]@{ Id='bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'; Name='Kopi Sachet';             Category='minuman'; Barcode='899999900011'; Unit='pcs'; SellPrice=2500;  CostPrice=1500; DiscountPercent=$null }
)

$prodById = @{}
foreach ($p in $products) { $prodById[$p.Id] = $p }

$batches = @(
  # Id, ProductId, Quantity, PurchaseDate, ExpiryDate, UnitCost, DiscountPercent
  [pscustomobject]@{ BatchId='c0000001-0000-0000-0000-000000000001'; ProductId=$products[0].Id; Quantity=40;  PurchaseDate=($todayDate.AddDays(-30).AddHours(8));  ExpiryDate=$todayDate.AddDays(10);  UnitCost=2500;  DiscountPercent=15 },
  [pscustomobject]@{ BatchId='c0000002-0000-0000-0000-000000000002'; ProductId=$products[0].Id; Quantity=80;  PurchaseDate=($todayDate.AddDays(-25).AddHours(8).AddMinutes(30)); ExpiryDate=$todayDate.AddDays(120); UnitCost=2500;  DiscountPercent=$null },
  [pscustomobject]@{ BatchId='c0000003-0000-0000-0000-000000000003'; ProductId=$products[1].Id; Quantity=90;  PurchaseDate=($todayDate.AddDays(-22).AddHours(9));  ExpiryDate=$todayDate.AddDays(180); UnitCost=3500;  DiscountPercent=$null },
  [pscustomobject]@{ BatchId='c0000004-0000-0000-0000-000000000004'; ProductId=$products[2].Id; Quantity=12;  PurchaseDate=($todayDate.AddDays(-18).AddHours(10)); ExpiryDate=$todayDate.AddDays(12);  UnitCost=6000;  DiscountPercent=20 },
  [pscustomobject]@{ BatchId='c0000005-0000-0000-0000-000000000005'; ProductId=$products[2].Id; Quantity=50;  PurchaseDate=($todayDate.AddDays(-16).AddHours(10).AddMinutes(30)); ExpiryDate=$todayDate.AddDays(200); UnitCost=6000;  DiscountPercent=$null },
  [pscustomobject]@{ BatchId='c0000006-0000-0000-0000-000000000006'; ProductId=$products[3].Id; Quantity=18;  PurchaseDate=($todayDate.AddDays(-14).AddHours(11)); ExpiryDate=$todayDate.AddDays(7);   UnitCost=8000;  DiscountPercent=30 },
  [pscustomobject]@{ BatchId='c0000007-0000-0000-0000-000000000007'; ProductId=$products[3].Id; Quantity=20;  PurchaseDate=($todayDate.AddDays(-12).AddHours(11).AddMinutes(30)); ExpiryDate=$todayDate.AddDays(90);  UnitCost=8000;  DiscountPercent=$null },
  [pscustomobject]@{ BatchId='c0000008-0000-0000-0000-000000000008'; ProductId=$products[4].Id; Quantity=6;   PurchaseDate=($todayDate.AddDays(-6).AddHours(13)); ExpiryDate=$todayDate.AddDays(-2); UnitCost=11000; DiscountPercent=50 },
  [pscustomobject]@{ BatchId='c0000009-0000-0000-0000-000000000009'; ProductId=$products[4].Id; Quantity=15;  PurchaseDate=($todayDate.AddDays(-5).AddHours(13).AddMinutes(30)); ExpiryDate=$todayDate.AddDays(3);   UnitCost=11000; DiscountPercent=25 },
  [pscustomobject]@{ BatchId='c0000010-0000-0000-0000-000000000010'; ProductId=$products[5].Id; Quantity=25;  PurchaseDate=($todayDate.AddDays(-9).AddHours(14)); ExpiryDate=$todayDate.AddDays(60);  UnitCost=20000; DiscountPercent=$null },
  [pscustomobject]@{ BatchId='c0000011-0000-0000-0000-000000000011'; ProductId=$products[6].Id; Quantity=120; PurchaseDate=($todayDate.AddDays(-8).AddHours(15)); ExpiryDate=$todayDate.AddDays(365); UnitCost=2000;  DiscountPercent=$null },
  [pscustomobject]@{ BatchId='c0000012-0000-0000-0000-000000000012'; ProductId=$products[7].Id; Quantity=150; PurchaseDate=($todayDate.AddDays(-7).AddHours(16)); ExpiryDate=$todayDate.AddDays(300); UnitCost=1500;  DiscountPercent=$null }
)

$movements = @(
  # PurchaseIn history
  [pscustomobject]@{ Id='d0000001-0000-0000-0000-000000000001'; ProductId=$products[0].Id; Timestamp=$todayDate.AddDays(-20).AddHours(9);  Type='PurchaseIn'; Quantity=150; Reason='Pembelian stok (testing)'; UnitCost=2500 },
  [pscustomobject]@{ Id='d0000002-0000-0000-0000-000000000002'; ProductId=$products[1].Id; Timestamp=$todayDate.AddDays(-18).AddHours(10); Type='PurchaseIn'; Quantity=100; Reason='Pembelian stok (testing)'; UnitCost=3500 },
  [pscustomobject]@{ Id='d0000003-0000-0000-0000-000000000003'; ProductId=$products[2].Id; Timestamp=$todayDate.AddDays(-15).AddHours(11); Type='PurchaseIn'; Quantity=80;  Reason='Pembelian stok (testing)'; UnitCost=6000 },
  [pscustomobject]@{ Id='d0000004-0000-0000-0000-000000000004'; ProductId=$products[3].Id; Timestamp=$todayDate.AddDays(-14).AddHours(12); Type='PurchaseIn'; Quantity=60;  Reason='Pembelian stok (testing)'; UnitCost=8000 },
  [pscustomobject]@{ Id='d0000005-0000-0000-0000-000000000005'; ProductId=$products[4].Id; Timestamp=$todayDate.AddDays(-10).AddHours(13); Type='PurchaseIn'; Quantity=40;  Reason='Pembelian stok (testing)'; UnitCost=11000 },
  [pscustomobject]@{ Id='d0000006-0000-0000-0000-000000000006'; ProductId=$products[5].Id; Timestamp=$todayDate.AddDays(-9).AddHours(14);  Type='PurchaseIn'; Quantity=30;  Reason='Pembelian stok (testing)'; UnitCost=20000 },
  [pscustomobject]@{ Id='d0000007-0000-0000-0000-000000000007'; ProductId=$products[6].Id; Timestamp=$todayDate.AddDays(-8).AddHours(15);  Type='PurchaseIn'; Quantity=150; Reason='Pembelian stok (testing)'; UnitCost=2000 },
  [pscustomobject]@{ Id='d0000008-0000-0000-0000-000000000008'; ProductId=$products[7].Id; Timestamp=$todayDate.AddDays(-7).AddHours(16);  Type='PurchaseIn'; Quantity=200; Reason='Pembelian stok (testing)'; UnitCost=1500 },

  # SaleOut history (optional in SQL but included there)
  [pscustomobject]@{ Id='fb000001-0000-0000-0000-000000000001'; ProductId=$products[3].Id; Timestamp=$todayDate.AddHours(9).AddMinutes(15);  Type='SaleOut'; Quantity=2; Reason='Penjualan (testing)'; UnitCost=8000 },
  [pscustomobject]@{ Id='fb000002-0000-0000-0000-000000000002'; ProductId=$products[2].Id; Timestamp=$todayDate.AddHours(9).AddMinutes(15);  Type='SaleOut'; Quantity=1; Reason='Penjualan (testing)'; UnitCost=6000 },
  [pscustomobject]@{ Id='fb000003-0000-0000-0000-000000000003'; ProductId=$products[0].Id; Timestamp=$todayDate.AddHours(13).AddMinutes(20); Type='SaleOut'; Quantity=5; Reason='Penjualan (testing)'; UnitCost=2500 },
  [pscustomobject]@{ Id='fb000004-0000-0000-0000-000000000004'; ProductId=$products[1].Id; Timestamp=$todayDate.AddHours(13).AddMinutes(20); Type='SaleOut'; Quantity=3; Reason='Penjualan (testing)'; UnitCost=3500 },
  [pscustomobject]@{ Id='fb000005-0000-0000-0000-000000000005'; ProductId=$products[4].Id; Timestamp=$todayDate.AddHours(19).AddMinutes(5);  Type='SaleOut'; Quantity=2; Reason='Penjualan (testing)'; UnitCost=11000 },
  [pscustomobject]@{ Id='fb000006-0000-0000-0000-000000000006'; ProductId=$products[6].Id; Timestamp=$todayDate.AddHours(19).AddMinutes(5);  Type='SaleOut'; Quantity=4; Reason='Penjualan (testing)'; UnitCost=2000 }
)

# Add TotalCost column
$movements = $movements | ForEach-Object {
  $_ | Add-Member -NotePropertyName TotalCost -NotePropertyValue ([decimal]$_.UnitCost * [decimal]$_.Quantity) -Force
  $_
}

# =============================
# Build report rows
# =============================

$movementRows = foreach ($m in ($movements | Sort-Object Timestamp)) {
  $p = $prodById[$m.ProductId]
  $direction = if ($m.Type -eq 'PurchaseIn') { 'Masuk' } else { 'Keluar' }
  [pscustomobject]@{
    Tanggal = $m.Timestamp.ToString('yyyy-MM-dd')
    Jam = $m.Timestamp.ToString('HH:mm')
    Produk = $p.Name
    Kategori = $p.Category
    Tipe = $m.Type
    Arah = $direction
    Qty = $m.Quantity
    Satuan = $p.Unit
    UnitCost = [decimal]$m.UnitCost
    TotalCost = [decimal]$m.TotalCost
    Alasan = $m.Reason
  }
}

$batchRows = foreach ($b in ($batches | Sort-Object ExpiryDate, PurchaseDate)) {
  $p = $prodById[$b.ProductId]
  $status = if ($b.ExpiryDate -lt $todayDate) { 'EXPIRED' } elseif ($b.ExpiryDate -le $todayDate.AddDays(14)) { 'NEAR_EXPIRY' } else { 'OK' }
  [pscustomobject]@{
    Produk = $p.Name
    BatchId = $b.BatchId
    PurchaseDate = ([datetime]$b.PurchaseDate).ToString('yyyy-MM-dd HH:mm')
    ExpiryDate = ([datetime]$b.ExpiryDate).ToString('yyyy-MM-dd')
    StatusExpiry = $status
    Qty = $b.Quantity
    Satuan = $p.Unit
    UnitCost = [decimal]$b.UnitCost
    DiscountPercent = $b.DiscountPercent
  }
}

$summaryRows = foreach ($p in ($products | Sort-Object Name)) {
  $in = 0
  foreach ($x in ($movements | Where-Object { $_.ProductId -eq $p.Id -and $_.Type -eq 'PurchaseIn' })) { $in += [int]$x.Quantity }

  $out = 0
  foreach ($x in ($movements | Where-Object { $_.ProductId -eq $p.Id -and $_.Type -eq 'SaleOut' })) { $out += [int]$x.Quantity }

  $stock = 0
  foreach ($x in ($batches | Where-Object { $_.ProductId -eq $p.Id })) { $stock += [int]$x.Quantity }

  $firstInObj = ($movements | Where-Object { $_.ProductId -eq $p.Id -and $_.Type -eq 'PurchaseIn' } | Sort-Object Timestamp | Select-Object -First 1)
  $lastInObj  = ($movements | Where-Object { $_.ProductId -eq $p.Id -and $_.Type -eq 'PurchaseIn' } | Sort-Object Timestamp | Select-Object -Last 1)
  $lastOutObj = ($movements | Where-Object { $_.ProductId -eq $p.Id -and $_.Type -eq 'SaleOut' } | Sort-Object Timestamp | Select-Object -Last 1)

  $firstIn = if ($firstInObj) { $firstInObj.Timestamp } else { $null }
  $lastIn  = if ($lastInObj)  { $lastInObj.Timestamp } else { $null }
  $lastOut = if ($lastOutObj) { $lastOutObj.Timestamp } else { $null }

  $inQty = if ($null -eq $in) { 0 } else { [int]$in }
  $outQty = if ($null -eq $out) { 0 } else { [int]$out }
  $stockQty = if ($null -eq $stock) { 0 } else { [int]$stock }

  [pscustomobject]@{
    Produk = $p.Name
    Kategori = $p.Category
    Satuan = $p.Unit
    TotalMasuk = $inQty
    TotalKeluar = $outQty
    StokSaatIni = $stockQty
    TglMasukPertama = if ($firstIn) { $firstIn.ToString('yyyy-MM-dd') } else { '' }
    TglMasukTerakhir = if ($lastIn) { $lastIn.ToString('yyyy-MM-dd') } else { '' }
    TglKeluarTerakhir = if ($lastOut) { $lastOut.ToString('yyyy-MM-dd') } else { '' }
    SellPrice = [decimal]$p.SellPrice
    CostPrice = [decimal]$p.CostPrice
    NilaiStokModal = [decimal]$p.CostPrice * [decimal]$stockQty
    NilaiStokJual = [decimal]$p.SellPrice * [decimal]$stockQty
  }
}

$totalProducts = $products.Count

$totalStock = 0
$totalStockValueCost = 0
$totalStockValueSell = 0
$totalMasukAll = 0
$totalKeluarAll = 0

foreach ($r in $summaryRows) {
  $totalStock += [int]$r.StokSaatIni
  $totalStockValueCost += [decimal]$r.NilaiStokModal
  $totalStockValueSell += [decimal]$r.NilaiStokJual
  $totalMasukAll += [int]$r.TotalMasuk
  $totalKeluarAll += [int]$r.TotalKeluar
}

$overallRow = [pscustomobject]@{
  Produk = 'TOTAL'
  Kategori = ''
  Satuan = ''
  TotalMasuk = $totalMasukAll
  TotalKeluar = $totalKeluarAll
  StokSaatIni = $totalStock
  TglMasukPertama = ''
  TglMasukTerakhir = ''
  TglKeluarTerakhir = ''
  SellPrice = ''
  CostPrice = ''
  NilaiStokModal = $totalStockValueCost
  NilaiStokJual = $totalStockValueSell
}

$summaryRowsWithTotal = @($summaryRows + $overallRow)

# =============================
# Write outputs (Excel-ready CSV)
# =============================

Ensure-Dir $OutDir
$resolvedOut = (Resolve-Path -LiteralPath $OutDir).Path

$pathSummary = Join-Path $resolvedOut 'barang_summary.csv'
$pathMovements = Join-Path $resolvedOut 'barang_masuk_keluar.csv'
$pathBatches = Join-Path $resolvedOut 'barang_batch.csv'
$pathInfo = Join-Path $resolvedOut 'README.txt'

Write-CsvUtf8 -Rows $summaryRowsWithTotal -Path $pathSummary
Write-CsvUtf8 -Rows $movementRows -Path $pathMovements
Write-CsvUtf8 -Rows $batchRows -Path $pathBatches

$info = @(
  "Laporan dibuat: $($Today.ToString('yyyy-MM-dd HH:mm:ss'))",
  "Basis data: mengikuti logic seed store_program_with_seed_testing.sql (tanggal dinamis berbasis hari eksekusi)",
  "File:",
  "- barang_summary.csv (rekap per produk + total keseluruhan)",
  "- barang_masuk_keluar.csv (riwayat barang masuk/keluar)",
  "- barang_batch.csv (detail batch + expiry/purchase date)"
)
[System.IO.File]::WriteAllLines($pathInfo, $info, (New-Object System.Text.UTF8Encoding($true)))

Write-Host "OK: CSV reports generated in $resolvedOut"