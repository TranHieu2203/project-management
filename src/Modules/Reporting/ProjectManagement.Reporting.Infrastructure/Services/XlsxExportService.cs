using ClosedXML.Excel;
using ProjectManagement.Reporting.Application.Queries.GetCostBreakdown;

namespace ProjectManagement.Reporting.Infrastructure.Services;

public class XlsxExportService
{
    public byte[] Generate(CostBreakdownResult data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Chi phí");

        ws.Cell(1, 1).Value = "Tên";
        ws.Cell(1, 2).Value = "Ước tính";
        ws.Cell(1, 3).Value = "Chính thức";
        ws.Cell(1, 4).Value = "% XN";
        ws.Cell(1, 5).Value = "Giờ";

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

        for (int i = 0; i < data.Items.Count; i++)
        {
            var item = data.Items[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = item.DimensionLabel;
            ws.Cell(row, 2).Value = (double)item.EstimatedCost;
            ws.Cell(row, 3).Value = (double)item.OfficialCost;
            ws.Cell(row, 4).Value = (double)item.ConfirmedPct;
            ws.Cell(row, 5).Value = (double)item.TotalHours;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
