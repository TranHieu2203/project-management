using System.Text;
using ProjectManagement.Reporting.Application.Queries.GetCostBreakdown;

namespace ProjectManagement.Reporting.Infrastructure.Services;

public class CsvExportService
{
    public byte[] Generate(CostBreakdownResult data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tên,Ước tính,Chính thức,% XN,Giờ");
        foreach (var item in data.Items)
        {
            sb.AppendLine($"{Escape(item.DimensionLabel)},{item.EstimatedCost:F0},{item.OfficialCost:F0},{item.ConfirmedPct:F1},{item.TotalHours:F1}");
        }
        // UTF-8 BOM for Excel Vietnamese compatibility
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
