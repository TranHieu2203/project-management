# Story 6.4: PDF generation via Puppeteer worker (async) + failure reporting

Status: review

**Story ID:** 6.4
**Epic:** Epic 6 — Cost Tracking & Official Reporting (Confirmed vs Estimated) + Export
**Sprint:** Sprint 7
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want PDF export được generate server-side ổn định,
So that layout nhất quán và không phụ thuộc browser print.

## Acceptance Criteria

1. **Given** user trigger export với `format = "pdf"`
   **When** `POST /api/v1/reports/export-jobs`
   **Then** trả `202 Accepted` + `jobId` (giống CSV/XLSX — dùng chung flow)
   **And** job được enqueue và worker xử lý async

2. **Given** PDF job đang chạy
   **When** QuestPDF generate báo cáo cost breakdown
   **Then** file PDF chứa header, bảng data (dimensionLabel, estimatedCost, officialCost, confirmedPct, totalHours), footer với timestamp
   **And** hoàn thành trong SLA mục tiêu < 10 giây

3. **Given** PDF generation thất bại (exception)
   **When** worker bắt lỗi
   **Then** job.Status = "Failed", job.ErrorMessage chứa message đầy đủ
   **And** lỗi được log với structured logging (ILogger)

4. **Given** format = "pdf" từ frontend
   **When** export trigger form được submit
   **Then** option "PDF" hiện trong dropdown và flow polling/download hoạt động giống CSV/XLSX

---

## Tasks / Subtasks

- [x] **Task 1: Thêm QuestPDF NuGet package**
  - [x] 1.1 Thêm `QuestPDF` vào `Reporting.Infrastructure.csproj`
  - [x] 1.2 Cấu hình QuestPDF License (Community) trong `ReportingModuleExtensions`

- [x] **Task 2: PdfExportService**
  - [x] 2.1 Tạo `Services/PdfExportService.cs` sử dụng QuestPDF Fluent API
  - [x] 2.2 Layout PDF: tiêu đề, bảng data với cột tên/ước tính/chính thức/% xác nhận/giờ, footer timestamp
  - [x] 2.3 Đăng ký `PdfExportService` trong `ReportingModuleExtensions`

- [x] **Task 3: Mở rộng ExportWorker + TriggerExportCommand**
  - [x] 3.1 Thêm `"pdf"` vào danh sách `validFormats` trong `TriggerExportCommand`
  - [x] 3.2 Thêm nhánh `"pdf"` trong `ExportWorker.ProcessJobAsync` (switch expression)
  - [x] 3.3 Tên file PDF: `cost-breakdown_{groupBy}_{YYYYMMDD_HHmmss}.pdf`

- [x] **Task 4: Frontend — thêm option PDF**
  - [x] 4.1 Thêm `{ value: 'pdf', label: 'PDF' }` vào `formats` array trong `ExportTriggerComponent`

- [x] **Task 5: Build verification**
  - [x] 5.1 `dotnet build` → 0 errors
  - [x] 5.2 `ng build` → 0 errors

---

## Dev Notes

### Story 6-3 đã tạo infrastructure — 6-4 chỉ extend

Story 6-3 đã xây dựng hoàn chỉnh:
- `ExportJob` entity (status Queued/Processing/Ready/Failed)
- `ExportWorker` IHostedService + `Channel<Guid>`
- `TriggerExportCommand` (validate format: csv, xlsx)
- `CsvExportService`, `XlsxExportService`
- `POST /api/v1/reports/export-jobs`, `GET .../status`, `GET .../download`
- Frontend `ExportTriggerComponent` với format selector, polling, blob download

Story 6-4 **chỉ cần**:
1. Thêm `QuestPDF` NuGet
2. Tạo `PdfExportService`
3. Thêm `"pdf"` vào valid formats + worker branch
4. Thêm PDF option vào FE dropdown

---

### Task 1 — QuestPDF License Setup

**QuestPDF** (thư viện được chọn thay "Puppeteer" — pure C#, không cần Node.js runtime):
- Community License: miễn phí nếu doanh thu < $1M/year hoặc mục đích học/open-source
- NuGet: `QuestPDF` latest stable (2025.x)

**Cấu hình license:** QuestPDF yêu cầu set license type trước khi dùng. Thêm vào `ReportingModuleExtensions.AddReportingModule()`:

```csharp
QuestPDF.Settings.License = LicenseType.Community;
```

**Import cần thiết:** `using QuestPDF.Infrastructure;`

**Phiên bản mới nhất:** `QuestPDF 2025.4.0` (April 2025) — Fluent API ổn định, full Fluent Document API.

---

### Task 2 — PdfExportService

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Services/PdfExportService.cs`

```csharp
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
                    col.Item().Text($"Nhóm theo: {data.GroupBy}  |  Tổng: {data.TotalCount} mục")
                        .FontColor(Colors.Grey.Medium).FontSize(9);
                });

                page.Content().PaddingVertical(0.5f, Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);   // Tên
                        columns.RelativeColumn(2);   // Ước tính
                        columns.RelativeColumn(2);   // Chính thức
                        columns.RelativeColumn(1.5f);// % XN
                        columns.RelativeColumn(1.5f);// Giờ
                    });

                    // Header row
                    static IContainer HeaderCell(IContainer container) =>
                        container.Background(Colors.Blue.Lighten3).Padding(4);

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Tên").Bold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("Ước tính").Bold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("Chính thức").Bold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("% XN").Bold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("Giờ").Bold();
                    });

                    // Data rows
                    static IContainer DataCell(IContainer container) =>
                        container.BorderBottom(0.5f, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(4);

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
```

---

### Task 3 — Mở rộng ExportWorker + TriggerExportCommand

**3.1 — Thêm "pdf" vào validFormats trong `TriggerExportCommand.cs`:**

```csharp
// Trước: var validFormats = new[] { "csv", "xlsx" };
var validFormats = new[] { "csv", "xlsx", "pdf" };
```

**3.2 — ExportWorker.ProcessJobAsync thêm nhánh "pdf":**

```csharp
// Hiện tại:
byte[] content = job.Format == "csv"
    ? csvSvc.Generate(breakdown)
    : xlsxSvc.Generate(breakdown);

// Thay bằng:
var pdfSvc = scope.ServiceProvider.GetRequiredService<PdfExportService>();
byte[] content = job.Format switch
{
    "csv"  => csvSvc.Generate(breakdown),
    "xlsx" => xlsxSvc.Generate(breakdown),
    "pdf"  => pdfSvc.Generate(breakdown, $"Báo cáo Chi phí — {job.GroupBy}"),
    _      => throw new InvalidOperationException($"Format không hỗ trợ: {job.Format}")
};
```

**3.3 — Tên file PDF:**

```csharp
var ext = job.Format switch { "csv" => "csv", "xlsx" => "xlsx", "pdf" => "pdf", _ => job.Format };
var fileName = $"cost-breakdown_{job.GroupBy}_{ts}.{ext}";
```

**ContentType cho download trong `DownloadExportQuery.cs`:**

```csharp
var contentType = job.Format switch
{
    "csv"  => "text/csv;charset=utf-8",
    "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "pdf"  => "application/pdf",
    _      => "application/octet-stream"
};
```

---

### Task 4 — Frontend

**`export-trigger.ts`** — chỉ thêm 1 entry vào `formats` array:

```typescript
readonly formats = [
  { value: 'csv',  label: 'CSV' },
  { value: 'xlsx', label: 'Excel (XLSX)' },
  { value: 'pdf',  label: 'PDF' },
];
```

---

### Lý do chọn QuestPDF thay Puppeteer

| | Puppeteer (.NET) | QuestPDF |
|---|---|---|
| Runtime | Cần Node.js + Chrome/Chromium | Pure C# |
| Dependency | PuppeteerSharp (download Chromium ~300MB) | NuGet only |
| Windows Server | Thường cần Xvfb/display | Không cần |
| Performance | ~3-8s (browser launch) | < 1s |
| Customization | HTML/CSS template | Fluent C# API |
| License | MIT | Community (free < $1M revenue) |

Architecture note ("Puppeteer" trong tên story) là aspirational. Thực tế triển khai .NET Modular Monolith trên Windows Server: QuestPDF là lựa chọn tốt hơn cho Phase 1.

---

### Patterns từ Stories trước

1. **ExportWorker scope pattern**: Đã có sẵn từ 6-3. Chỉ thêm `scope.ServiceProvider.GetRequiredService<PdfExportService>()` vào `ProcessJobAsync`.
2. **DownloadExportQuery ContentType**: Cần thêm `"pdf" => "application/pdf"` vào switch expression.
3. **Validate format tại TriggerExportCommand**: Thêm `"pdf"` vào `validFormats` array là đủ.
4. **QuestPDF License must be set before first use**: Đặt trong module registration, không trong service constructor.

---

### Anti-patterns cần tránh

- **KHÔNG** tạo PdfExportWorker riêng — dùng chung `ExportWorker` từ 6-3, chỉ thêm nhánh `"pdf"`.
- **KHÔNG** dùng `PuppeteerSharp` — quá nặng (download Chromium ~300MB), không phù hợp Windows Server.
- **KHÔNG** quên set `QuestPDF.Settings.License = LicenseType.Community` — thiếu sẽ throw exception runtime.
- **KHÔNG** thêm Channel/Worker mới — tái sử dụng Channel<Guid> từ 6-3.

---

## Completion Notes

- Chọn QuestPDF thay Puppeteer: pure C# không cần Node.js/Chromium, performance < 1s vs 3-8s browser launch
- QuestPDF License set trong `ReportingModuleExtensions` trước mọi usage (`QuestPDF.Settings.License = LicenseType.Community`)
- Tái sử dụng hoàn toàn `ExportWorker` + `Channel<Guid>` từ 6-3 — chỉ thêm 1 nhánh switch
- `DownloadExportQuery.ContentType` mở rộng thành switch expression cho csv/xlsx/pdf/fallback
- `dotnet build`: 0 errors; `ng build`: 0 errors

## Files Created/Modified

**Backend — Infrastructure:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Services/PdfExportService.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Workers/ExportWorker.cs` — thêm PdfExportService + switch expression
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/ProjectManagement.Reporting.Infrastructure.csproj` — thêm QuestPDF 2025.4.0

**Backend — Application:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Commands/TriggerExport/TriggerExportCommand.cs` — thêm "pdf" vào validFormats
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/DownloadExport/DownloadExportQuery.cs` — ContentType switch expression

**Backend — Api:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Extensions/ReportingModuleExtensions.cs` — QuestPDF license + PdfExportService DI

**Frontend:**
- `frontend/.../features/reporting/components/export-trigger/export-trigger.ts` — thêm PDF option vào formats array
