# Story 6.3: Export jobs (CSV/XLSX/PDF) + progress + artifact download

Status: review

**Story ID:** 6.3
**Epic:** Epic 6 — Cost Tracking & Official Reporting (Confirmed vs Estimated) + Export
**Sprint:** Sprint 7
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want export báo cáo ra file (async) với tiến trình rõ,
So that tôi chia sẻ với cấp trên và đối soát dễ dàng.

## Acceptance Criteria

1. **Given** user trigger `POST /api/v1/reports/export-jobs` với `format` (csv|xlsx), `groupBy`, filters tùy chọn
   **When** yêu cầu được nhận
   **Then** trả `202 Accepted` với `{ jobId, status: "Queued" }` — không block HTTP thread

2. **Given** jobId hợp lệ
   **When** `GET /api/v1/reports/export-jobs/{jobId}`
   **Then** trả `{ jobId, status, format, fileName, errorMessage?, createdAt, completedAt? }`
   **And** status cycle: `Queued → Processing → Ready | Failed`

3. **Given** status = Ready
   **When** `GET /api/v1/reports/export-jobs/{jobId}/download`
   **Then** trả file content với đúng `Content-Type` và `Content-Disposition: attachment; filename=...`
   **And** file chỉ accessible bởi user đã trigger (membership-scope)

4. **Given** `format = "csv"` hoặc `"xlsx"`
   **When** worker generate file
   **Then** CSV dùng UTF-8 với BOM (Excel-compatible); XLSX dùng ClosedXML
   **And** tên file: `cost-breakdown_{groupBy}_{YYYYMMDD_HHmmss}.{ext}`

5. **Given** app restart khi job ở trạng thái Queued
   **When** app khởi động lại
   **Then** worker load lại Queued jobs từ DB vào Channel và tiếp tục xử lý

6. **Given** format không hợp lệ (không phải csv/xlsx)
   **When** gọi trigger endpoint
   **Then** trả `ProblemDetails 400`

---

## Tasks / Subtasks

- [x] **Task 1: Domain — ExportJob entity**
  - [x] 1.1 Tạo `Reporting.Domain/Entities/ExportJob.cs` + `ExportJobStatus` enum
  - [x] 1.2 ExportJob chứa: Id, TriggeredBy (Guid), Format, GroupBy, FilterParams (string JSON), Status, FileName, FileContent (byte[]?), ErrorMessage, CreatedAt, CompletedAt

- [x] **Task 2: Reporting.Infrastructure — DbContext + Migration**
  - [x] 2.1 Tạo `IReportingDbContext` trong Reporting.Application (interface với `DbSet<ExportJob>`)
  - [x] 2.2 Tạo `ReportingDbContext` trong Reporting.Infrastructure/Persistence với schema `"reporting"`
  - [x] 2.3 Tạo `ExportJobConfiguration` (EF Fluent API)
  - [x] 2.4 Tạo migration `20260426140000_InitialReporting` (bảng `export_jobs`)
  - [x] 2.5 Đăng ký `ReportingDbContext` trong `ReportingModuleExtensions`
  - [x] 2.6 Thêm `ReportingDbContext` migration vào `Program.cs` (trong block `autoMigrate`)

- [x] **Task 3: Application — Commands + Queries**
  - [x] 3.1 Tạo `Commands/TriggerExport/TriggerExportCommand.cs` + handler: INSERT ExportJob (Queued) → enqueue vào Channel
  - [x] 3.2 Tạo `Queries/GetExportJob/GetExportJobQuery.cs` + handler: đọc ExportJob từ DB, verify TriggeredBy = CurrentUserId
  - [x] 3.3 `DownloadExportQuery` handler: đọc FileContent, trả `(byte[], string contentType, string fileName)`, verify ownership

- [x] **Task 4: Infrastructure — Channel + ExportWorker**
  - [x] 4.1 Đăng ký `Channel<Guid>` (exportJobId) trong `ReportingModuleExtensions` (`Channel.CreateBounded<Guid>(100)`)
  - [x] 4.2 Tạo `Workers/ExportWorker.cs` implements `IHostedService`
    - Startup: load `Queued` jobs từ DB vào Channel (recovery)
    - Loop: dequeue jobId → fetch job → set Processing → generate file (CSV/XLSX) → set Ready + FileContent
    - Catch: set Failed + ErrorMessage
  - [x] 4.3 Tạo `Services/CsvExportService.cs` (generate CSV từ CostBreakdownResult)
  - [x] 4.4 Tạo `Services/XlsxExportService.cs` (generate XLSX via ClosedXML)
  - [x] 4.5 Thêm `ClosedXML` NuGet package vào `Reporting.Infrastructure.csproj`

- [x] **Task 5: API — Controller endpoints**
  - [x] 5.1 Thêm 3 endpoints vào `ReportingController.cs`:
    - `POST /api/v1/reports/export-jobs` → `TriggerExportCommand` → 202
    - `GET /api/v1/reports/export-jobs/{jobId}` → `GetExportJobQuery` → 200
    - `GET /api/v1/reports/export-jobs/{jobId}/download` → `DownloadExportQuery` → File

- [x] **Task 6: Frontend — ExportJobService + polling component**
  - [x] 6.1 Thêm `ExportJobDto` interface vào `cost-report.model.ts`
  - [x] 6.2 Thêm methods vào `reporting-api.service.ts`: `triggerExport()`, `getExportJobStatus()`, `downloadExport()`
  - [x] 6.3 Tạo `components/export-trigger/export-trigger.ts` + `export-trigger.html`:
    - Form: format selector (csv/xlsx) + groupBy + optional month
    - Button "Export" → trigger → hiện spinner + jobId
    - Poll mỗi 2s với RxJS `interval(2000)` + `takeUntil(destroy$)` + `takeUntil(status=Ready/Failed)`
    - Khi Ready: hiện "Download" link (gọi `downloadExport()` → tạo blob URL)
    - Khi Failed: hiện errorMessage
  - [x] 6.4 Thêm route `{ path: 'export', loadComponent: ExportTriggerComponent }` vào `reporting.routes.ts`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors (pre-existing MSB3277 warnings only)
  - [x] 7.2 `ng build` → 0 errors

---

## Dev Notes

### Reporting module hiện tại (6-1 + 6-2 đã tạo)

Module đã có: Domain, Application (2 queries), Infrastructure (packages only, NO DbContext), Api (controller + extensions).

Story 6-3 **lần đầu tiên** tạo:
- `ReportingDbContext` + migration (schema `"reporting"`)
- `IReportingDbContext` interface
- `Channel<Guid>` + `ExportWorker` IHostedService
- ClosedXML package

---

### Task 1 — ExportJob Entity

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Domain/Entities/ExportJob.cs`

```csharp
namespace ProjectManagement.Reporting.Domain.Entities;

public class ExportJob
{
    public Guid Id { get; private set; }
    public Guid TriggeredBy { get; private set; }
    public string Format { get; private set; } = string.Empty;      // "csv" | "xlsx"
    public string GroupBy { get; private set; } = string.Empty;     // "vendor"|"project"|"resource"|"month"
    public string FilterParams { get; private set; } = string.Empty; // JSON: { month?, vendorId?, projectId?, resourceId? }
    public string Status { get; private set; } = string.Empty;      // use ExportJobStatus constants
    public string? FileName { get; private set; }
    public byte[]? FileContent { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static ExportJob Create(Guid triggeredBy, string format, string groupBy, string filterParams)
        => new()
        {
            Id = Guid.NewGuid(),
            TriggeredBy = triggeredBy,
            Format = format,
            GroupBy = groupBy,
            FilterParams = filterParams,
            Status = ExportJobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
        };

    public void MarkProcessing() => Status = ExportJobStatus.Processing;

    public void MarkReady(string fileName, byte[] content)
    {
        Status = ExportJobStatus.Ready;
        FileName = fileName;
        FileContent = content;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = ExportJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
```

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Domain/Entities/ExportJobStatus.cs`

```csharp
namespace ProjectManagement.Reporting.Domain.Entities;

public static class ExportJobStatus
{
    public const string Queued = "Queued";
    public const string Processing = "Processing";
    public const string Ready = "Ready";
    public const string Failed = "Failed";
}
```

---

### Task 2 — IReportingDbContext + ReportingDbContext + Migration

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Application/Common/Interfaces/IReportingDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Application.Common.Interfaces;

public interface IReportingDbContext
{
    DbSet<ExportJob> ExportJobs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/ReportingDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Domain.Entities;
using ProjectManagement.Reporting.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext : DbContext, IReportingDbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("reporting");
        modelBuilder.ApplyConfiguration(new ExportJobConfiguration());
    }
}
```

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/Configurations/ExportJobConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> b)
    {
        b.ToTable("export_jobs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TriggeredBy).HasColumnName("triggered_by");
        b.Property(x => x.Format).HasColumnName("format").HasMaxLength(10).IsRequired();
        b.Property(x => x.GroupBy).HasColumnName("group_by").HasMaxLength(20).IsRequired();
        b.Property(x => x.FilterParams).HasColumnName("filter_params").HasColumnType("text").IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(500);
        b.Property(x => x.FileContent).HasColumnName("file_content").HasColumnType("bytea");
        b.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");

        b.HasIndex(x => x.TriggeredBy).HasDatabaseName("ix_export_jobs_triggered_by");
        b.HasIndex(x => x.Status).HasDatabaseName("ix_export_jobs_status");
    }
}
```

**Migration file:** `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Migrations/20260426140000_InitialReporting.cs`

Tạo thủ công (không dùng dotnet ef tools vì cross-context complexity). Pattern giống `20260426130000_AddImportJob_TimeTracking.cs`:

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Reporting.Infrastructure.Migrations
{
    public partial class InitialReporting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "reporting");

            migrationBuilder.CreateTable(
                name: "export_jobs",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    group_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    filter_params = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    file_content = table.Column<byte[]>(type: "bytea", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_export_jobs", x => x.id); });

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_triggered_by",
                schema: "reporting",
                table: "export_jobs",
                column: "triggered_by");

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_status",
                schema: "reporting",
                table: "export_jobs",
                column: "status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "export_jobs", schema: "reporting");
        }
    }
}
```

**Migration snapshot file** cũng cần tạo:
`src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Migrations/ReportingDbContextModelSnapshot.cs`

Pattern: copy từ một snapshot khác (e.g., TimeTrackingDbContextModelSnapshot), thay class name, namespace, schema, table.

**ReportingModuleExtensions.cs** — cập nhật để đăng ký DbContext + Channel + Worker:

```csharp
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Reporting.Api.Controllers;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Application.Queries.GetCostSummary;
using ProjectManagement.Reporting.Infrastructure.Persistence;
using ProjectManagement.Reporting.Infrastructure.Workers;

namespace ProjectManagement.Reporting.Api.Extensions;

public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings:Default"]!;

        services.AddDbContext<ReportingDbContext>(opts =>
            opts.UseNpgsql(connectionString));
        services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<ReportingDbContext>());

        services.AddSingleton(Channel.CreateBounded<Guid>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        }));
        services.AddSingleton(sp => sp.GetRequiredService<Channel<Guid>>().Writer);
        services.AddSingleton(sp => sp.GetRequiredService<Channel<Guid>>().Reader);

        services.AddHostedService<ExportWorker>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetCostSummaryHandler).Assembly));

        mvc.AddApplicationPart(typeof(ReportingController).Assembly);
        return services;
    }
}
```

**Program.cs** — thêm migration (trong block `autoMigrate`):

```csharp
using ProjectManagement.Reporting.Infrastructure.Persistence;
// ...
var reportingDb = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
await reportingDb.Database.MigrateAsync();
```

---

### Task 3 — Commands + Queries

**TriggerExportCommand:**

```csharp
// File: Application/Commands/TriggerExport/TriggerExportCommand.cs
public sealed record TriggerExportCommand(
    Guid CurrentUserId,
    string Format,            // "csv" | "xlsx"
    string GroupBy,
    string? Month,
    Guid? VendorId,
    Guid? ProjectId,
    Guid? ResourceId
) : IRequest<TriggerExportResult>;

public sealed record TriggerExportResult(Guid JobId, string Status);
```

**TriggerExportHandler logic:**

```csharp
// Validate
var validFormats = new[] { "csv", "xlsx" };
if (!validFormats.Contains(command.Format.ToLowerInvariant()))
    throw new ArgumentException($"Format '{command.Format}' không hợp lệ. Chấp nhận: csv, xlsx.");

var validGroupBy = new[] { "vendor", "project", "resource", "month" };
if (!validGroupBy.Contains(command.GroupBy.ToLowerInvariant()))
    throw new ArgumentException($"GroupBy '{command.GroupBy}' không hợp lệ.");

// Build filterParams JSON
var filterParams = JsonSerializer.Serialize(new {
    command.Month, command.VendorId, command.ProjectId, command.ResourceId
});

var job = ExportJob.Create(command.CurrentUserId, command.Format.ToLowerInvariant(),
    command.GroupBy.ToLowerInvariant(), filterParams);

_db.ExportJobs.Add(job);
await _db.SaveChangesAsync(ct);

// Enqueue (non-blocking write)
await _channel.WriteAsync(job.Id, ct);

return new TriggerExportResult(job.Id, job.Status);
```

**GetExportJobQuery:**

```csharp
public sealed record GetExportJobQuery(Guid CurrentUserId, Guid JobId) : IRequest<ExportJobDto>;
public sealed record ExportJobDto(
    Guid JobId, string Status, string Format, string GroupBy,
    string? FileName, string? ErrorMessage, DateTime CreatedAt, DateTime? CompletedAt);

// Handler: verify TriggeredBy == CurrentUserId → throw NotFoundException (404) nếu không tìm thấy hoặc không phải owner
```

**DownloadExportQuery:**

```csharp
public sealed record DownloadExportQuery(Guid CurrentUserId, Guid JobId)
    : IRequest<DownloadExportResult>;

public sealed record DownloadExportResult(byte[] Content, string ContentType, string FileName);

// Handler:
// - Tìm job, verify TriggeredBy == CurrentUserId
// - Nếu Status != Ready → throw InvalidOperationException("Export chưa sẵn sàng")
// - ContentType: "text/csv;charset=utf-8" hoặc "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
```

---

### Task 4 — ExportWorker

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Workers/ExportWorker.cs`

```csharp
public class ExportWorker : BackgroundService
{
    private readonly ChannelReader<Guid> _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExportWorker> _logger;

    public ExportWorker(ChannelReader<Guid> reader, IServiceScopeFactory scopeFactory, ILogger<ExportWorker> logger)
    {
        _reader = reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Recovery: load Queued jobs từ DB vào Channel lúc startup
        await RecoverPendingJobsAsync(stoppingToken);

        // Process loop
        await foreach (var jobId in _reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(jobId, stoppingToken);
        }
    }

    private async Task RecoverPendingJobsAsync(CancellationToken ct)
    {
        // Phải dùng scope vì DbContext là scoped
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var queuedIds = await db.ExportJobs
            .Where(j => j.Status == ExportJobStatus.Queued)
            .Select(j => j.Id)
            .ToListAsync(ct);

        foreach (var id in queuedIds)
        {
            // GetRequiredService<Channel<Guid>>() instead of ChannelReader to get Writer
            var writer = scope.ServiceProvider.GetRequiredService<ChannelWriter<Guid>>();
            await writer.WriteAsync(id, ct);
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var job = await db.ExportJobs.FindAsync([jobId], ct);
        if (job is null) return;

        try
        {
            job.MarkProcessing();
            await db.SaveChangesAsync(ct);

            // Re-run breakdown query using stored params
            var filter = JsonSerializer.Deserialize<ExportFilterParams>(job.FilterParams)!;
            var breakdown = await mediator.Send(new GetCostBreakdownQuery(
                job.TriggeredBy, job.GroupBy, filter.Month,
                filter.VendorId, filter.ProjectId, filter.ResourceId,
                page: 1, pageSize: 10000), ct);   // export ALL rows, không phân trang

            byte[] content;
            if (job.Format == "csv")
            {
                var svc = scope.ServiceProvider.GetRequiredService<CsvExportService>();
                content = svc.Generate(breakdown);
            }
            else
            {
                var svc = scope.ServiceProvider.GetRequiredService<XlsxExportService>();
                content = svc.Generate(breakdown);
            }

            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var ext = job.Format == "csv" ? "csv" : "xlsx";
            var fileName = $"cost-breakdown_{job.GroupBy}_{ts}.{ext}";

            job.MarkReady(fileName, content);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export job {JobId} failed", jobId);
            job.MarkFailed(ex.Message);
            await db.SaveChangesAsync(ct);
        }
    }
}

// Internal record để deserialize FilterParams JSON
internal sealed record ExportFilterParams(string? Month, Guid? VendorId, Guid? ProjectId, Guid? ResourceId);
```

**Lưu ý quan trọng về DI trong BackgroundService:**
- `BackgroundService` là singleton scope
- `ReportingDbContext` là scoped — PHẢI dùng `IServiceScopeFactory.CreateAsyncScope()` mỗi lần xử lý
- KHÔNG inject `ReportingDbContext` trực tiếp vào `ExportWorker`

---

### Task 4.3 — CsvExportService

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Services/CsvExportService.cs`

```csharp
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
        // UTF-8 BOM để Excel hiển thị đúng tiếng Việt
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
```

---

### Task 4.4 — XlsxExportService (ClosedXML)

**ClosedXML NuGet:** `<PackageReference Include="ClosedXML" Version="0.104.2" />`
Thêm vào `Reporting.Infrastructure.csproj`.

```csharp
using ClosedXML.Excel;

public class XlsxExportService
{
    public byte[] Generate(CostBreakdownResult data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Chi phí");

        // Header row
        ws.Cell(1, 1).Value = "Tên";
        ws.Cell(1, 2).Value = "Ước tính";
        ws.Cell(1, 3).Value = "Chính thức";
        ws.Cell(1, 4).Value = "% XN";
        ws.Cell(1, 5).Value = "Giờ";
        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data rows
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
```

Đăng ký trong `ReportingModuleExtensions`:
```csharp
services.AddTransient<CsvExportService>();
services.AddTransient<XlsxExportService>();
```

---

### Task 5 — Controller Endpoints

Thêm vào `ReportingController.cs`:

```csharp
using ProjectManagement.Reporting.Application.Commands.TriggerExport;
using ProjectManagement.Reporting.Application.Queries.GetExportJob;
using ProjectManagement.Reporting.Application.Queries.DownloadExport;

/// <summary>Trigger async export (CSV/XLSX). Returns 202 + jobId.</summary>
[HttpPost("export-jobs")]
public async Task<IActionResult> TriggerExport([FromBody] TriggerExportRequest req, CancellationToken ct)
{
    var result = await _mediator.Send(
        new TriggerExportCommand(_currentUser.UserId, req.Format, req.GroupBy,
            req.Month, req.VendorId, req.ProjectId, req.ResourceId), ct);
    return Accepted(new { result.JobId, result.Status });
}

/// <summary>Poll export job status.</summary>
[HttpGet("export-jobs/{jobId:guid}")]
public async Task<IActionResult> GetExportJob(Guid jobId, CancellationToken ct)
{
    var result = await _mediator.Send(new GetExportJobQuery(_currentUser.UserId, jobId), ct);
    return Ok(result);
}

/// <summary>Download file when status = Ready.</summary>
[HttpGet("export-jobs/{jobId:guid}/download")]
public async Task<IActionResult> DownloadExport(Guid jobId, CancellationToken ct)
{
    var result = await _mediator.Send(new DownloadExportQuery(_currentUser.UserId, jobId), ct);
    return File(result.Content, result.ContentType, result.FileName);
}
```

**Request DTO** (in Api project or Application):
```csharp
public sealed record TriggerExportRequest(
    string Format,       // "csv" | "xlsx"
    string GroupBy,      // "vendor"|"project"|"resource"|"month"
    string? Month,
    Guid? VendorId,
    Guid? ProjectId,
    Guid? ResourceId);
```

---

### Task 6 — Frontend

**Thêm vào `cost-report.model.ts`:**

```typescript
export interface ExportJobDto {
  jobId: string;
  status: 'Queued' | 'Processing' | 'Ready' | 'Failed';
  format: string;
  groupBy: string;
  fileName: string | null;
  errorMessage: string | null;
  createdAt: string;
  completedAt: string | null;
}
```

**Thêm vào `reporting-api.service.ts`:**

```typescript
triggerExport(format: string, groupBy: string, month?: string): Observable<{ jobId: string; status: string }> {
  return this.http.post<{ jobId: string; status: string }>(
    '/api/v1/reports/export-jobs',
    { format, groupBy, month: month || null, vendorId: null, projectId: null, resourceId: null }
  );
}

getExportJobStatus(jobId: string): Observable<ExportJobDto> {
  return this.http.get<ExportJobDto>(`/api/v1/reports/export-jobs/${jobId}`);
}

downloadExport(jobId: string): Observable<Blob> {
  return this.http.get(`/api/v1/reports/export-jobs/${jobId}/download`, { responseType: 'blob' });
}
```

**ExportTriggerComponent — polling pattern (NO NgRx):**

```typescript
@Component({
  selector: 'app-export-trigger',
  standalone: true,
  imports: [AsyncPipe, NgIf, NgFor, ReactiveFormsModule, MatButtonModule, MatCardModule,
    MatProgressSpinnerModule, MatFormFieldModule, MatSelectModule, MatInputModule],
  templateUrl: './export-trigger.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExportTriggerComponent implements OnDestroy {
  private readonly api = inject(ReportingApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();
  private stopPolling$ = new Subject<void>();

  readonly fb = inject(FormBuilder);
  readonly form = this.fb.nonNullable.group({
    format: ['csv'],
    groupBy: ['vendor'],
    month: [''],
  });

  readonly formats = [{ value: 'csv', label: 'CSV' }, { value: 'xlsx', label: 'Excel (XLSX)' }];
  readonly dimensions = [
    { value: 'vendor', label: 'Theo Vendor' },
    { value: 'project', label: 'Theo Project' },
    { value: 'resource', label: 'Theo Nhân sự' },
    { value: 'month', label: 'Theo Tháng' },
  ];

  loading = false;
  jobStatus: ExportJobDto | null = null;
  jobId: string | null = null;

  trigger(): void {
    if (this.loading) return;
    const { format, groupBy, month } = this.form.getRawValue();
    this.loading = true;
    this.jobStatus = null;
    this.stopPolling$.next(); // cancel previous poll

    this.api.triggerExport(format, groupBy, month || undefined)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ jobId }) => {
          this.jobId = jobId;
          this.startPolling(jobId);
          this.cdr.markForCheck();
        },
        error: () => { this.loading = false; this.cdr.markForCheck(); }
      });
  }

  private startPolling(jobId: string): void {
    interval(2000).pipe(
      startWith(0),
      switchMap(() => this.api.getExportJobStatus(jobId)),
      takeUntil(this.stopPolling$),
      takeUntil(this.destroy$),
    ).subscribe(status => {
      this.jobStatus = status;
      if (status.status === 'Ready' || status.status === 'Failed') {
        this.loading = false;
        this.stopPolling$.next();
      }
      this.cdr.markForCheck();
    });
  }

  download(): void {
    if (!this.jobId) return;
    this.api.downloadExport(this.jobId).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = this.jobStatus?.fileName ?? 'export';
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopPolling$.complete();
  }
}
```

**Import cần thêm vào component:**
```typescript
import { Subject, interval } from 'rxjs';
import { startWith, switchMap, takeUntil } from 'rxjs/operators';
import { ChangeDetectorRef } from '@angular/core';
import { ExportJobDto } from '../../models/cost-report.model';
import { ReportingApiService } from '../../services/reporting-api.service';
```

**Template `export-trigger.html`:**

```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Xuất Báo cáo</mat-card-title>
  </mat-card-header>
  <mat-card-content>
    <form [formGroup]="form" (ngSubmit)="trigger()"
          style="display:flex;gap:12px;align-items:flex-end;flex-wrap:wrap;margin-bottom:16px">
      <mat-form-field style="min-width:140px">
        <mat-label>Định dạng</mat-label>
        <mat-select formControlName="format">
          <mat-option *ngFor="let f of formats" [value]="f.value">{{ f.label }}</mat-option>
        </mat-select>
      </mat-form-field>
      <mat-form-field style="min-width:180px">
        <mat-label>Nhóm theo</mat-label>
        <mat-select formControlName="groupBy">
          <mat-option *ngFor="let d of dimensions" [value]="d.value">{{ d.label }}</mat-option>
        </mat-select>
      </mat-form-field>
      <mat-form-field>
        <mat-label>Tháng (tùy chọn)</mat-label>
        <input matInput type="month" formControlName="month">
      </mat-form-field>
      <button mat-raised-button color="primary" type="submit" [disabled]="loading">
        Xuất file
      </button>
      <mat-spinner *ngIf="loading" diameter="24"></mat-spinner>
    </form>

    <div *ngIf="jobStatus">
      <p>Trạng thái: <strong>{{ jobStatus.status }}</strong></p>
      <p *ngIf="jobStatus.status === 'Ready'">
        <button mat-stroked-button color="accent" (click)="download()">
          Tải xuống {{ jobStatus.fileName }}
        </button>
      </p>
      <p *ngIf="jobStatus.status === 'Failed'" style="color:#c62828">
        Lỗi: {{ jobStatus.errorMessage }}
      </p>
    </div>
  </mat-card-content>
</mat-card>
```

---

### Patterns từ Stories trước

1. **IHostedService + scope**: KHÔNG inject scoped DbContext trực tiếp vào BackgroundService/Worker — luôn dùng `IServiceScopeFactory.CreateAsyncScope()` mỗi iteration.
2. **Migration thủ công**: `dotnet ef migrations add` không khả thi khi cross-context — tạo file migration thủ công, giống pattern của `20260426130000_AddImportJob_TimeTracking.cs`. Cần cả `ReportingDbContextModelSnapshot.cs`.
3. **Channel<T> Bounded**: Dùng `BoundedChannelOptions` với `FullMode = Wait` để back-pressure.
4. **GlobalExceptionMiddleware**: `ArgumentException` → 400 đã xử lý từ 6-2. `InvalidOperationException` cần thêm nếu muốn trả 409/400 thay vì 500 khi download chưa ready. Thêm case: `InvalidOperationException ioex => BuildProblem(StatusCodes.Status409Conflict, "Conflict", ioex.Message, "...")`
5. **Frontend blob download**: Tạo `URL.createObjectURL(blob)` + click() + revoke để trigger browser download.
6. **No NgRx cho polling flow**: Theo architecture note — vendor-import và export status chỉ dùng RxJS interval/takeUntil, không cần NgRx.
7. **`ChangeDetectorRef.markForCheck()`**: OnPush component + async subscribe bên ngoài async pipe → phải gọi thủ công.

---

### Anti-patterns cần tránh

- **KHÔNG** inject `ReportingDbContext` hay `IReportingDbContext` trực tiếp vào `ExportWorker` — luôn create scope.
- **KHÔNG** dùng `Channel.CreateUnbounded` — bounded channel với back-pressure là đúng.
- **KHÔNG** bỏ qua migration snapshot file — EF Core yêu cầu `*ModelSnapshot.cs` để generate migration kế tiếp.
- **KHÔNG** thêm `ReportingDbContext` vào `Reporting.Application.csproj` — Application không được biết về Infrastructure. `IReportingDbContext` ở Application, `ReportingDbContext` ở Infrastructure.
- **KHÔNG** leak export file của user A sang user B — luôn verify `TriggeredBy == CurrentUserId` trong GetExportJob và DownloadExport handlers.
- **KHÔNG** dùng `Page > 1` khi export — set `pageSize: 10000` trong ExportWorker để lấy toàn bộ data.
- **KHÔNG** store file path trên disk — store `byte[]` trong DB (`bytea`) để đơn giản hóa Phase 1 (không cần file system management).

---

## Completion Notes

- Reporting module lần đầu có DB: thêm `ReportingDbContext` schema `"reporting"`, migration thủ công (không dùng dotnet ef tools), đăng ký migration trong `Program.cs`
- EF Core version conflict (10.0.4 vs 10.0.7) nâng cấp thành compile error CS1705 khi `IReportingDbContext` interface dùng `DbSet<T>` từ 10.0.4 nhưng `ReportingDbContext` dùng 10.0.7 (via Npgsql). Fix: upgrade `Reporting.Application.csproj` lên 10.0.7
- `BackgroundService` (ExportWorker) là singleton — KHÔNG inject scoped `ReportingDbContext` trực tiếp, dùng `IServiceScopeFactory.CreateAsyncScope()` mỗi job
- `Channel<Guid>` bounded 100 + `SingleReader: true` — Writer/Reader đăng ký riêng biệt qua `.Writer` / `.Reader` properties
- Startup recovery: worker load `Queued` jobs từ DB vào channel trước khi bắt đầu listen
- `InvalidOperationException` → 409 thêm vào `GlobalExceptionMiddleware` (download khi chưa Ready)
- Frontend polling: NO NgRx — dùng `interval(2000)` + `startWith(0)` + `switchMap` + `takeUntil(stopPolling$)` trong OnPush component với `ChangeDetectorRef.markForCheck()`
- `dotnet build`: 0 errors; `ng build`: 0 errors

## Files Created/Modified

**Backend — Domain:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Domain/Entities/ExportJob.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Domain/Entities/ExportJobStatus.cs` — mới

**Backend — Application:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Common/Interfaces/IReportingDbContext.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Commands/TriggerExport/TriggerExportCommand.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetExportJob/GetExportJobQuery.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/DownloadExport/DownloadExportQuery.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/ProjectManagement.Reporting.Application.csproj` — upgrade EF Core 10.0.4 → 10.0.7

**Backend — Infrastructure:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/ReportingDbContext.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/Configurations/ExportJobConfiguration.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Migrations/20260426140000_InitialReporting.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Migrations/ReportingDbContextModelSnapshot.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Workers/ExportWorker.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Services/CsvExportService.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Services/XlsxExportService.cs` — mới
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/ProjectManagement.Reporting.Infrastructure.csproj` — thêm ClosedXML 0.104.2

**Backend — Api:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/ReportingController.cs` — thêm 3 endpoints + `TriggerExportRequest` record
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Extensions/ReportingModuleExtensions.cs` — thêm DbContext + Channel + Worker + Services DI

**Backend — Host:**
- `src/Host/ProjectManagement.Host/Program.cs` — thêm `ReportingDbContext` migration

**Backend — Shared:**
- `src/Shared/ProjectManagement.Shared.Infrastructure/Middleware/GlobalExceptionMiddleware.cs` — thêm `InvalidOperationException` → 409

**Frontend:**
- `frontend/.../features/reporting/models/cost-report.model.ts` — thêm `ExportJobDto`
- `frontend/.../features/reporting/services/reporting-api.service.ts` — thêm 3 methods export
- `frontend/.../features/reporting/components/export-trigger/export-trigger.ts` — mới
- `frontend/.../features/reporting/components/export-trigger/export-trigger.html` — mới
- `frontend/.../features/reporting/reporting.routes.ts` — thêm `export` route
