using ClosedXML.Excel;
using Microsoft.Maui.Storage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StoreProgram.Models;

using QContainer = QuestPDF.Infrastructure.IContainer;
using PdfColors = QuestPDF.Helpers.Colors;

namespace StoreProgram.Services;

public static class ReportExportService
{
    static ReportExportService()
    {
        // Menghindari exception license di versi baru QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static Task<string> ExportExcelAsync(DateRange range, string periodName)
    {
        var data = BuildReportData(range);

        var filename = $"Laporan_{periodName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var path = Path.Combine(FileSystem.Current.AppDataDirectory, filename);

        using var wb = new XLWorkbook();

        // Sheet: Ringkasan
        {
            var ws = wb.Worksheets.Add("Ringkasan");

            ws.Cell(1, 1).Value = "Periode";
            ws.Cell(1, 2).Value = periodName;
            ws.Cell(2, 1).Value = "Tanggal";
            ws.Cell(2, 2).Value = $"{data.Range.Start:dd MMM yyyy HH:mm} - {data.Range.End:dd MMM yyyy HH:mm}";

            ws.Cell(4, 1).Value = "Total Penjualan";
            ws.Cell(4, 2).Value = data.Summary.TotalSales;
            ws.Cell(5, 1).Value = "Total Pengeluaran";
            ws.Cell(5, 2).Value = data.TotalExpenses;
            ws.Cell(6, 1).Value = "Keuntungan Bersih";
            ws.Cell(6, 2).Value = data.Summary.NetProfit;
            ws.Cell(7, 1).Value = "Total Transaksi";
            ws.Cell(7, 2).Value = data.Summary.TotalTransactions;

            ws.Range(1, 1, 2, 1).Style.Font.Bold = true;
            ws.Range(4, 1, 7, 1).Style.Font.Bold = true;

            ws.Range(4, 2, 6, 2).Style.NumberFormat.Format = "\"Rp\" #,##0";
            ws.Columns().AdjustToContents();
        }

        // Sheet: Top Produk
        {
            var ws = wb.Worksheets.Add("Top Produk");

            ws.Cell(1, 1).Value = "Rank";
            ws.Cell(1, 2).Value = "Produk";
            ws.Cell(1, 3).Value = "Qty Terjual";
            ws.Cell(1, 4).Value = "Total";
            ws.Range(1, 1, 1, 4).Style.Font.Bold = true;

            int row = 2;
            int rank = 1;
            foreach (var p in data.Summary.TopProducts)
            {
                ws.Cell(row, 1).Value = rank++;
                ws.Cell(row, 2).Value = p.ProductName;
                ws.Cell(row, 3).Value = p.QuantitySold;
                ws.Cell(row, 4).Value = p.TotalSales;
                row++;
            }

            ws.Column(4).Style.NumberFormat.Format = "\"Rp\" #,##0";
            ws.Columns().AdjustToContents();
        }

        // Sheet: Transaksi
        {
            var ws = wb.Worksheets.Add("Transaksi");

            ws.Cell(1, 1).Value = "Waktu";
            ws.Cell(1, 2).Value = "Pembayaran";
            ws.Cell(1, 3).Value = "Item";
            ws.Cell(1, 4).Value = "Total";
            ws.Range(1, 1, 1, 4).Style.Font.Bold = true;

            int row = 2;
            foreach (var sale in data.Sales.OrderByDescending(s => s.Timestamp))
            {
                ws.Cell(row, 1).Value = sale.Timestamp;
                ws.Cell(row, 1).Style.DateFormat.Format = "dd MMM yyyy HH:mm";

                ws.Cell(row, 2).Value = sale.PaymentMethod;
                ws.Cell(row, 3).Value = FormatItems(sale);

                ws.Cell(row, 4).Value = sale.GrossAmount;
                row++;
            }

            ws.Column(4).Style.NumberFormat.Format = "\"Rp\" #,##0";
            ws.Columns().AdjustToContents();
            ws.Column(3).Width = Math.Min(80, ws.Column(3).Width + 10);
        }

        wb.SaveAs(path);
        return Task.FromResult(path);
    }

    public static Task<string> ExportPdfAsync(DateRange range, string periodName)
    {
        var data = BuildReportData(range);

        var filename = $"Laporan_{periodName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var path = Path.Combine(FileSystem.Current.AppDataDirectory, filename);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Laporan Penjualan").FontSize(18).SemiBold();
                    col.Item().Text($"Periode: {periodName}").FontSize(12).FontColor(PdfColors.Grey.Darken2);
                    col.Item().Text($"Tanggal: {data.Range.Start:dd MMM yyyy HH:mm} - {data.Range.End:dd MMM yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor(PdfColors.Grey.Darken2);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Element(e => BuildSummaryBox(e, data));
                    col.Item().Element(e => BuildTopProductsTable(e, data));
                    col.Item().Element(e => BuildTransactionsTable(e, data));
                });

                page.Footer().AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(9).FontColor(PdfColors.Grey.Darken2))
                    .Text(x =>
                    {
                        x.Span("Generated: ");
                        x.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                    });
            });
        }).GeneratePdf(path);

        return Task.FromResult(path);
    }

    private static void BuildSummaryBox(QContainer container, ReportData data)
    {
        container
            .Background(PdfColors.Grey.Lighten4)
            .Border(1)
            .BorderColor(PdfColors.Grey.Lighten2)
            .Padding(12)
            .Column(col =>
            {
                col.Item().Text("Ringkasan").SemiBold();

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Penjualan");
                    r.ConstantItem(140).AlignRight().Text($"Rp {data.Summary.TotalSales:N0}").SemiBold();
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Pengeluaran");
                    r.ConstantItem(140).AlignRight().Text($"Rp {data.TotalExpenses:N0}");
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Keuntungan Bersih");
                    r.ConstantItem(140).AlignRight().Text($"Rp {data.Summary.NetProfit:N0}").SemiBold();
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Transaksi");
                    r.ConstantItem(140).AlignRight().Text(data.Summary.TotalTransactions.ToString());
                });
            });
    }

    private static void BuildTopProductsTable(QContainer container, ReportData data)
    {
        container.Column(col =>
        {
            col.Item().Text("Produk Terlaris").SemiBold();

            if (data.Summary.TopProducts.Count == 0)
            {
                col.Item().Text("Belum ada data.").FontColor(PdfColors.Grey.Darken2);
                return;
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(30);
                    cols.RelativeColumn(4);
                    cols.ConstantColumn(70);
                    cols.ConstantColumn(90);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeaderCell).Text("#");
                    h.Cell().Element(HeaderCell).Text("Produk");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Total");
                });

                int rank = 1;
                foreach (var p in data.Summary.TopProducts)
                {
                    table.Cell().Element(BodyCell).Text(rank++.ToString());
                    table.Cell().Element(BodyCell).Text(p.ProductName);
                    table.Cell().Element(BodyCell).AlignRight().Text(p.QuantitySold.ToString());
                    table.Cell().Element(BodyCell).AlignRight().Text($"Rp {p.TotalSales:N0}");
                }

                static QContainer HeaderCell(QContainer c) => c
                    .Background(PdfColors.Grey.Lighten3)
                    .PaddingVertical(6)
                    .PaddingHorizontal(6)
                    .BorderBottom(1)
                    .BorderColor(PdfColors.Grey.Lighten2)
                    .DefaultTextStyle(x => x.SemiBold().FontSize(10));

                static QContainer BodyCell(QContainer c) => c
                    .PaddingVertical(6)
                    .PaddingHorizontal(6)
                    .BorderBottom(1)
                    .BorderColor(PdfColors.Grey.Lighten3)
                    .DefaultTextStyle(x => x.FontSize(10));
            });
        });
    }

    private static void BuildTransactionsTable(QContainer container, ReportData data)
    {
        container.Column(col =>
        {
            col.Item().Text("Transaksi").SemiBold();

            if (data.Sales.Count == 0)
            {
                col.Item().Text("Belum ada transaksi di periode ini.").FontColor(PdfColors.Grey.Darken2);
                return;
            }

            var rows = data.Sales
                .OrderByDescending(s => s.Timestamp)
                .Take(50)
                .ToList();

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(90);
                    cols.ConstantColumn(60);
                    cols.RelativeColumn(4);
                    cols.ConstantColumn(90);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeaderCell).Text("Waktu");
                    h.Cell().Element(HeaderCell).Text("Bayar");
                    h.Cell().Element(HeaderCell).Text("Item");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Total");
                });

                foreach (var sale in rows)
                {
                    table.Cell().Element(BodyCell).Text(sale.Timestamp.ToString("dd/MM HH:mm"));
                    table.Cell().Element(BodyCell).Text(sale.PaymentMethod);
                    table.Cell().Element(BodyCell).Text(FormatItems(sale));
                    table.Cell().Element(BodyCell).AlignRight().Text($"Rp {sale.GrossAmount:N0}");
                }

                static QContainer HeaderCell(QContainer c) => c
                    .Background(PdfColors.Grey.Lighten3)
                    .PaddingVertical(6)
                    .PaddingHorizontal(6)
                    .BorderBottom(1)
                    .BorderColor(PdfColors.Grey.Lighten2)
                    .DefaultTextStyle(x => x.SemiBold().FontSize(10));

                static QContainer BodyCell(QContainer c) => c
                    .PaddingVertical(6)
                    .PaddingHorizontal(6)
                    .BorderBottom(1)
                    .BorderColor(PdfColors.Grey.Lighten3)
                    .DefaultTextStyle(x => x.FontSize(9));
            });

            if (data.Sales.Count > rows.Count)
                col.Item().Text($"(Ditampilkan {rows.Count} transaksi terakhir dari {data.Sales.Count} transaksi)")
                    .FontSize(9)
                    .FontColor(PdfColors.Grey.Darken2);
        });
    }

    private static ReportData BuildReportData(DateRange range)
    {
        var summary = DataStore.GetSummary(range);

        var sales = DataStore.Sales
            .Where(s => s.Timestamp >= range.Start && s.Timestamp <= range.End)
            .ToList();

        var expenses = DataStore.Expenses
            .Where(e => e.Timestamp >= range.Start && e.Timestamp <= range.End)
            .ToList();

        var totalExpenses = expenses
            .Where(e => e.Type is ExpenseType.Operational or ExpenseType.DamagedLostStock)
            .Sum(e => e.Amount);

        return new ReportData(range, summary, sales, totalExpenses);
    }

    private static string FormatItems(SaleTransaction sale)
    {
        if (sale.Items == null || sale.Items.Count == 0)
            return "-";

        // Batasi biar tidak kepanjangan di PDF/Excel
        var parts = sale.Items
            .Select(i => $"{i.ProductName} x{i.Quantity}")
            .ToList();

        var text = string.Join(", ", parts);
        return text.Length > 120 ? text[..117] + "..." : text;
    }

    private record ReportData(DateRange Range, FinancialSummary Summary, List<SaleTransaction> Sales, decimal TotalExpenses);
}
