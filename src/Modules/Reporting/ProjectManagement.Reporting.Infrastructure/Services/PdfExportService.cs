using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ProjectManagement.Reporting.Application.Queries.GetCostBreakdown;

namespace ProjectManagement.Reporting.Infrastructure.Services;

public class PdfExportService
{
    public byte[] Generate(CostBreakdownResult data, string title)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).Bold().FontSize(14);
                    col.Item().Text(
                        $"Nhóm theo: {data.GroupBy}  |  Tổng: {data.TotalCount} mục")
                        .FontColor(Colors.Grey.Medium).FontSize(9);
                });

                page.Content().PaddingVertical(0.5f, Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                    });

                    static IContainer HeaderCell(IContainer c) =>
                        c.Background(Colors.Blue.Lighten3).Padding(4);

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Tên").Bold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("Ước tính").Bold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("Chính thức").Bold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("% XN").Bold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("Giờ").Bold();
                    });

                    static IContainer DataCell(IContainer c) =>
                        c.BorderBottom(0.5f, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(4);

                    foreach (var item in data.Items)
                    {
                        table.Cell().Element(DataCell).Text(item.DimensionLabel);
                        table.Cell().Element(DataCell).AlignRight().Text($"{item.EstimatedCost:N0}");
                        table.Cell().Element(DataCell).AlignRight().Text($"{item.OfficialCost:N0}").Bold();
                        table.Cell().Element(DataCell).AlignRight().Text($"{item.ConfirmedPct:F1}%");
                        table.Cell().Element(DataCell).AlignRight().Text($"{item.TotalHours:F1}h");
                    }
                });

                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span($"Xuất lúc {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC  |  Trang ");
                    txt.CurrentPageNumber();
                    txt.Span(" / ");
                    txt.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
