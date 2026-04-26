# Story 3.5: Vendor CSV import pipeline (upload → mapping → validate → apply) + job status polling

Status: review

**Story ID:** 3.5
**Epic:** Epic 3 — TimeEntry & Timesheet (2-tier: Grid + Vendor Import) + Status/Lock + Corrections
**Sprint:** Sprint 4
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want import timesheet vendor theo CSV/Excel với mapping template,
So that cuối tháng có nguồn vendor-confirmed làm báo cáo chính thức.

## Acceptance Criteria

1. **Given** PM upload file vendor
   **When** tạo import job
   **Then** server trả `jobId` + trạng thái job; có endpoint `GET /api/v1/import-jobs/{jobId}` để polling

2. **Given** dry-run validation
   **When** gọi validate
   **Then** preview lỗi theo row/column; `dry-run` không commit domain data
   **And** phân loại `blocking` vs `warning`; warning yêu cầu confirm trước khi apply

3. **Given** apply import
   **When** PM xác nhận apply (confirm warnings)
   **Then** tạo entries `VendorConfirmed` theo idempotency rule
   **And** re-upload cùng file (same SHA-256 hash) → rejected hoặc skipped (không double-count)

4. **Given** có lỗi
   **When** PM muốn download error report
   **Then** `GET /api/v1/import-jobs/{jobId}/errors/download` trả CSV của các dòng lỗi

---

## Tasks / Subtasks

- [x] **Task 1: Domain — ImportJob entity**
  - [x] 1.1 Tạo `ImportJob` entity (Id, VendorId, Status enum, FileHash, FileName, TotalRows, ErrorCount, EnteredBy, CreatedAt, CompletedAt)
  - [x] 1.2 Tạo `ImportJobError` entity (Id, ImportJobId, RowIndex, ColumnName, ErrorType, Message)
  - [x] 1.3 Tạo `ImportJobStatus` enum: Pending, Validating, ValidatedOk, ValidatedWithWarnings, ValidatedWithErrors, Applying, Completed, Failed

- [x] **Task 2: Infrastructure — EF config + migration**
  - [x] 2.1 Cập nhật `ITimeTrackingDbContext` + `TimeTrackingDbContext` thêm `DbSet<ImportJob>` + `DbSet<ImportJobError>`
  - [x] 2.2 Tạo `ImportJobConfiguration` + `ImportJobErrorConfiguration`
  - [x] 2.3 Tạo EF migration `AddImportJob_TimeTracking` (thủ công nếu Host bị lock)
  - [x] 2.4 Thêm `ImportJobId` (Guid?) + `RowFingerprint` (string?) vào `TimeEntry` entity + configuration + migration

- [x] **Task 3: Application — import commands/queries**
  - [x] 3.1 `StartImportJobCommand(VendorId, FileName, FileHash, FileContent, ColumnMapping, EnteredBy)` → trả `ImportJobDto`
  - [x] 3.2 `StartImportJobHandler`: parse CSV, validate rows, save job + errors, set status
  - [x] 3.3 `ApplyImportJobCommand(JobId, EnteredBy)` → trả `ImportJobDto`
  - [x] 3.4 `ApplyImportJobHandler`: check status ValidatedOk/ValidatedWithWarnings, tạo VendorConfirmed entries, idempotency check (RowFingerprint unique per job), update job status
  - [x] 3.5 `GetImportJobQuery(JobId)` + handler → `ImportJobDto`
  - [x] 3.6 `GetImportJobErrorsQuery(JobId)` + handler → `IReadOnlyList<ImportJobErrorDto>`

- [x] **Task 4: API — import endpoints**
  - [x] 4.1 Tạo `ImportJobsController` với:
    - `POST /api/v1/import-jobs` (multipart + JSON metadata)
    - `GET /api/v1/import-jobs/{jobId}`
    - `GET /api/v1/import-jobs/{jobId}/errors` (JSON list for UI preview)
    - `POST /api/v1/import-jobs/{jobId}/apply`
    - `GET /api/v1/import-jobs/{jobId}/errors/download` (CSV response)

- [x] **Task 5: Frontend — import UI**
  - [x] 5.1 Tạo `vendor-import` component: file input + column mapping form
  - [x] 5.2 Sau upload: hiển thị job status + error list (blocking/warning)
  - [x] 5.3 Polling job status (interval 3s khi job đang processing)
  - [x] 5.4 "Apply" button hiển thị khi status ValidatedOk hoặc ValidatedWithWarnings + confirm dialog
  - [x] 5.5 "Download Errors" link khi có lỗi
  - [x] 5.6 Thêm route `/time-tracking/import` + link navigation

- [x] **Task 6: Build verification**
  - [x] 6.1 `dotnet build TimeTracking.Api.csproj` → 0 errors
  - [x] 6.2 `ng build` → 0 errors

---

## Dev Notes

### Nguyên tắc thiết kế

- **VendorConfirmed chỉ được tạo qua import pipeline** — CreateTimeEntryHandler đã block entryType=VendorConfirmed từ Stories 3.1
- **Idempotency**: `RowFingerprint = SHA256(jobId + resourceId + projectId + date + hours)` stored on TimeEntry; unique index `(import_job_id, row_fingerprint)` prevents double-count
- **File hash idempotency**: reject nếu `ImportJob` với cùng `file_hash` đã `Completed` (optional: warn nếu ValidatedOk)
- **Sync processing cho MVP**: handler xử lý synchronously, set status = Validating → ValidatedOk/WithWarnings/WithErrors trong cùng 1 request (no background queue needed)
- **Column mapping**: client gửi `{ resourceIdColumn, projectIdColumn, dateColumn, hoursColumn, roleColumn, levelColumn, noteColumn }` — server dùng để parse CSV

### Task 1 Detail: ImportJob entity

```csharp
public class ImportJob
{
    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public ImportJobStatus Status { get; private set; }
    public int TotalRows { get; private set; }
    public int ErrorCount { get; private set; }
    public string EnteredBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public void SetValidationResult(ImportJobStatus status, int totalRows, int errorCount)
    {
        Status = status; TotalRows = totalRows; ErrorCount = errorCount;
    }
    public void MarkCompleted() { Status = ImportJobStatus.Completed; CompletedAt = DateTime.UtcNow; }
    public void MarkFailed() { Status = ImportJobStatus.Failed; CompletedAt = DateTime.UtcNow; }

    public static ImportJob Create(Guid vendorId, string fileName, string fileHash, string enteredBy)
        => new() { Id = Guid.NewGuid(), VendorId = vendorId, FileName = fileName,
                   FileHash = fileHash, Status = ImportJobStatus.Pending,
                   EnteredBy = enteredBy, CreatedAt = DateTime.UtcNow };
}

public class ImportJobError
{
    public Guid Id { get; private set; }
    public Guid ImportJobId { get; private set; }
    public int RowIndex { get; private set; }
    public string? ColumnName { get; private set; }
    public string ErrorType { get; private set; } = string.Empty;  // "blocking" | "warning"
    public string Message { get; private set; } = string.Empty;

    public static ImportJobError Create(Guid jobId, int rowIndex, string? column, string errorType, string message)
        => new() { Id = Guid.NewGuid(), ImportJobId = jobId, RowIndex = rowIndex,
                   ColumnName = column, ErrorType = errorType, Message = message };
}

public enum ImportJobStatus
{
    Pending, Validating, ValidatedOk, ValidatedWithWarnings, ValidatedWithErrors, Applying, Completed, Failed
}
```

### Task 2 Detail: TimeEntry — thêm import tracking fields

```csharp
// TimeEntry entity — thêm 2 properties:
public Guid? ImportJobId { get; private set; }
public string? RowFingerprint { get; private set; }

// TimeEntry.Create() — thêm 2 params optional:
public static TimeEntry Create(..., Guid? importJobId = null, string? rowFingerprint = null)
    => new() { ..., ImportJobId = importJobId, RowFingerprint = rowFingerprint };
```

**TimeEntryConfiguration — thêm:**
```csharp
b.Property(x => x.ImportJobId).HasColumnName("import_job_id");
b.Property(x => x.RowFingerprint).HasColumnName("row_fingerprint").HasMaxLength(64);
// Unique constraint để prevent double-count:
b.HasIndex(x => new { x.ImportJobId, x.RowFingerprint })
 .HasDatabaseName("ix_time_entries_import_job_fingerprint")
 .IsUnique()
 .HasFilter("import_job_id IS NOT NULL");
```

**ImportJobConfiguration:**
```csharp
b.ToTable("import_jobs"); // schema time_tracking (via HasDefaultSchema)
b.HasKey(x => x.Id);
b.Property(x => x.FileHash).HasColumnName("file_hash").HasMaxLength(64).IsRequired();
b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
b.Property(x => x.EnteredBy).HasColumnName("entered_by").HasMaxLength(256);
// + all other columns...
b.HasIndex(x => x.FileHash).HasDatabaseName("ix_import_jobs_file_hash");
```

**ImportJobErrorConfiguration:**
```csharp
b.ToTable("import_job_errors");
b.HasKey(x => x.Id);
b.HasIndex(x => x.ImportJobId).HasDatabaseName("ix_import_job_errors_job_id");
```

### Task 3 Detail: StartImportJobHandler

```csharp
// CSV parsing — dùng thư viện có sẵn: System.IO.Pipelines KHÔNG cần thêm dependency
// CsvHelper (nếu đã có) hoặc string.Split(',') cho MVP
// Mỗi row: đọc theo column mapping, validate:
//   - ResourceId phải là valid Guid → blocking
//   - Hours phải là decimal > 0 và <= 16 → blocking
//   - Date phải parse được → blocking
//   - EntryType = VendorConfirmed (hardcoded — đây là import pipeline) 
//   - Warning: nếu resource không tồn tại trong DB (check IWorkforceDbContext)

// Row fingerprint:
var fingerprint = SHA256Hash($"{jobId}|{resourceId}|{projectId}|{date}|{hours}");
// Dùng: Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLower()[..32]
```

**ApplyImportJobHandler:**
```csharp
// 1. Check job status == ValidatedOk || ValidatedWithWarnings
// 2. Check file_hash chưa Completed trước (tránh re-apply)
// 3. Với mỗi valid row từ DB (re-parse hoặc store parsed rows):
//    - Skip nếu (ImportJobId, RowFingerprint) đã tồn tại
//    - GetHourlyRateAsync → Create TimeEntry với EntryType=VendorConfirmed + ImportJobId + RowFingerprint
// 4. job.MarkCompleted()
// 5. SaveChangesAsync
```

**Lưu ý**: Để Apply không cần re-parse CSV (file đã dispose), store raw CSV content trong ImportJob (column `file_content` BYTEA hoặc TEXT) hoặc trong memory giữa validate và apply — đơn giản nhất: **store CSV as text** trong `ImportJob.RawContent` (nullable, cleared after apply).

### Task 4 Detail: API endpoints

```csharp
// POST /api/v1/import-jobs — multipart/form-data
[HttpPost, Consumes("multipart/form-data")]
public async Task<IActionResult> StartImport(
    [FromForm] IFormFile file,
    [FromForm] Guid vendorId,
    [FromForm] string columnMapping,  // JSON string
    CancellationToken ct)

// GET /api/v1/import-jobs/{jobId}
[HttpGet("{jobId:guid}")]
public async Task<IActionResult> GetImportJob(Guid jobId, CancellationToken ct)

// POST /api/v1/import-jobs/{jobId}/apply
[HttpPost("{jobId:guid}/apply")]
public async Task<IActionResult> ApplyImport(Guid jobId, CancellationToken ct)

// GET /api/v1/import-jobs/{jobId}/errors/download
[HttpGet("{jobId:guid}/errors/download")]
public async Task<IActionResult> DownloadErrors(Guid jobId, CancellationToken ct)
// → return File(csvBytes, "text/csv", "errors.csv")
```

### Task 5 Detail: Frontend

**Component**: `vendor-import.ts` — service-based (NO NgRx cho import flow; import là one-shot operation không cần shared state)

```typescript
// Inject TimeTrackingApiService (thêm import methods)
// State: 'idle' | 'uploading' | 'validating' | 'ready-to-apply' | 'applying' | 'done' | 'error'
// Polling: interval(3000).pipe(takeUntil(this.destroy$)) → getImportJob(jobId)

// Methods:
upload(file: File, vendorId: string, mapping: ColumnMapping): void
apply(jobId: string): void
downloadErrors(jobId: string): void  // window.open(downloadUrl)
```

**API service thêm methods:**
```typescript
startImportJob(file: File, vendorId: string, mapping: object): Observable<ImportJobDto>
getImportJob(jobId: string): Observable<ImportJobDto>
applyImportJob(jobId: string): Observable<ImportJobDto>
// errors download = direct link: GET /api/v1/import-jobs/{jobId}/errors/download
```

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| `ITimeTrackingDbContext` + `DbSet<T>` pattern | Story 3.1 |
| EF config + manual migration | Stories 3.1, 3.3 |
| `_currentUser.UserId.ToString()` | All controller stories |
| `[Authorize]` controller | Story 3.1 |
| `ITimeTrackingRateService.GetHourlyRateAsync` | Story 3.1 |
| `CreateTimeEntryHandler.ToDto(entry)` | Story 3.1 |
| Service-based component (NO NgRx) | Story 2.5 AuditLog |

### CSV parsing dependency

Dùng `CsvHelper` nếu đã có trong project; nếu không, dùng `TextFieldParser` (Microsoft.VisualBasic) hoặc manual `string.Split`. Ưu tiên **không thêm NuGet package mới** — dùng `string.Split` + `TextReader` cho CSV MVP.

### File lock workaround

Build `TimeTracking.Api.csproj` thay vì Host khi Host.exe đang chạy.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- Backend xử lý synchronously: start → validate → set status trong 1 request; apply cũng synchronous
- Thêm `GET /api/v1/import-jobs/{jobId}/errors` (JSON) ngoài spec để UI hiển thị inline
- `RawContent` lưu trên ImportJob để Apply re-parse; cleared sau MarkCompleted() để tiết kiệm space
- Frontend dùng ChangeDetectionStrategy.Default (không OnPush) vì cần mutation detection cho polling
- `NgFor` không cần trong vendor-import (Mat table dùng dataSource, không dùng *ngFor)

### File List

**Backend:**
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/Enums/ImportJobStatus.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/Entities/ImportJob.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/Entities/ImportJobError.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/Entities/TimeEntry.cs` (added ImportJobId, RowFingerprint)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/Common/Interfaces/ITimeTrackingDbContext.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/TimeTrackingDbContext.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/Configurations/ImportJobConfiguration.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/Configurations/ImportJobErrorConfiguration.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/Configurations/TimeEntryConfiguration.cs` (added import fields)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Migrations/20260426130000_AddImportJob_TimeTracking.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/ImportJobs/DTOs/ImportJobDto.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/ImportJobs/Commands/StartImportJob/StartImportJobCommand.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/ImportJobs/Commands/StartImportJob/StartImportJobHandler.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/ImportJobs/Commands/ApplyImportJob/ApplyImportJobCommand.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/ImportJobs/Commands/ApplyImportJob/ApplyImportJobHandler.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/ImportJobs/Queries/GetImportJobQuery.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Api/Controllers/ImportJobsController.cs`

**Frontend:**
- `frontend/project-management-web/src/app/features/time-tracking/models/import-job.model.ts`
- `frontend/project-management-web/src/app/features/time-tracking/services/time-tracking-api.service.ts` (added import methods)
- `frontend/project-management-web/src/app/features/time-tracking/components/vendor-import/vendor-import.ts`
- `frontend/project-management-web/src/app/features/time-tracking/components/vendor-import/vendor-import.html`
- `frontend/project-management-web/src/app/features/time-tracking/time-tracking.routes.ts` (added /import route)
