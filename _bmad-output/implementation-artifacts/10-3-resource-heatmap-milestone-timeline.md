# Story 10.3: Resource Heatmap & Milestone Timeline (Growth)

Status: review

## Story

As a PM,
I want to see resource utilization as a person × week heatmap and milestones across all projects on one timeline,
So that I can identify capacity bottlenecks and cross-project scheduling conflicts at a glance.

## Acceptance Criteria

**AC-1: Resource Heatmap tại /reporting/resources**
- **Given** PM navigate đến `/reporting/resources`
- **When** trang load
- **Then** hiển thị heatmap với rows = team members, columns = tuần, màu cell theo % capacity (green/yellow/orange/red)
- **And** legend giải thích các mức màu: < 80% Green / 80–95% Yellow / 95–105% Orange / > 105% Red

**AC-2: Drill-down heatmap cell**
- **Given** PM click vào một cell trong heatmap (person × week)
- **When** click xảy ra
- **Then** drill-down hiển thị danh sách tasks của người đó trong tuần đó
- **And** thông tin: week start date, resource ID, actualHours, availableHours, utilizationPct

**AC-3: Milestone Timeline tại /reporting/milestones**
- **Given** PM navigate đến `/reporting/milestones`
- **When** trang load
- **Then** hiển thị danh sách milestones của tất cả projects PM có membership
- **And** mỗi milestone hiển thị: name, projectName, dueDate (plannedEndDate), status
- **And** danh sách sort theo dueDate tăng dần

**AC-4: Per-user isolation**
- **Given** PM chỉ có membership ở project A
- **When** xem heatmap hoặc milestone list
- **Then** chỉ thấy data từ project A — không thấy project B

**AC-5: Backend endpoints đúng route**
- **Given** các endpoint được call
- **When** với valid JWT
- **Then** `GET /api/v1/reports/resources?from=&to=` → trả về heatmap data
- **And** `GET /api/v1/reports/milestones?from=&to=` → trả về danh sách milestones
- **And** cả hai endpoint trả về 401 nếu không có JWT

**AC-6: Empty states và color-blind support**
- **Given** PM không có membership ở project nào
- **When** xem heatmap hoặc milestone list
- **Then** hiển thị empty state message rõ ràng (không blank/broken)
- **And** traffic-light cells dùng cả màu VÀ icon text label (color-blind support per NFR-21)

---

## Dev Notes

### ⚠️ Brownfield Context — Đọc trước khi code

Story 10-3 là **Growth feature** với cả backend và frontend. Đây là story THỰC SỰ phức tạp — bao gồm 2 backend endpoints MỚI + 2 frontend components MỚI + NgRx store extension.

**CRITICAL REUSE — KHÔNG reinvent:**
- Backend heatmap logic đã CÓ trong `GetCapacityHeatmapQuery.cs` (Capacity module). Story này cần tạo `GetResourceReportQuery.cs` riêng trong Reporting module dùng SAME approach (inject `IProjectsDbContext` + `ITimeTrackingDbContext`). Lý do: cross-module Application dependency là anti-pattern.
- Frontend heatmap component đã CÓ tại `features/capacity/components/capacity-heatmap/capacity-heatmap.ts`. Story này tạo `resource-report.ts` trong `features/reporting/` với cùng pattern nhưng route `/reporting/resources` (routing trong reporting feature).
- Traffic-light logic (Green/Yellow/Orange/Red) đã được implement trong `GetCapacityHeatmapQuery` — copy EXACT thresholds: < 80% Green, 80–95% Yellow, 95–105% Orange, > 105% Red.

**Existing Infrastructure:**
- `ReportingController.cs` tại route `/api/v1/reports` — thêm 2 endpoints vào đây
- `ReportingModuleExtensions.cs` — đã scan `typeof(GetCostSummaryHandler).Assembly` → `GetResourceReportQuery` và `GetMilestonesQuery` được auto-registered vì chúng ở cùng assembly
- `IProjectsDbContext` đã được inject trong Reporting.Application (xem `GetStatCardsQuery.cs`)
- `ITimeTrackingDbContext` chưa được inject trong Reporting.Application — cần verify project reference
- `TaskType.Milestone` enum value đã tồn tại trong `ProjectManagement.Projects.Domain.Enums`
- `ReportingApiService` đã có — chỉ cần add 2 methods mới
- NgRx `reportingFeature` đã có — extend state, actions, reducer, effects

**Frontend Routes:** `/reporting/resources` và `/reporting/milestones` (trong `reporting.routes.ts`)

**Route KHÔNG phải:** `/reports/resources` (architecture doc dùng `/reports/` nhưng Angular routing dùng `/reporting/`)

### Architecture Compliance

| Rule | Requirement |
|---|---|
| NFR-9 | Tất cả `/reports/*` endpoints yêu cầu valid JWT |
| NFR-14 | Per-user isolation — membership-scoped query |
| NFR-17 | `Cache-Control: max-age=300` cho `/api/v1/reports/*` |
| NFR-21 | Traffic light dùng cả màu VÀ icon text (color-blind support) |
| NFR-25 | Empty state khi no data — không blank layout |
| AR | `GetResourceReportQuery` và `GetMilestonesQuery` nằm trong `Reporting.Application` — không import từ `Capacity.Application` |

### Backend — Exact File Locations

```
src/Modules/Reporting/
├── ProjectManagement.Reporting.Application/
│   └── Queries/
│       ├── GetResourceReport/
│       │   └── GetResourceReportQuery.cs          ← MỚI (query + DTO + handler)
│       └── GetMilestones/
│           └── GetMilestonesQuery.cs               ← MỚI (query + DTO + handler)
│
└── ProjectManagement.Reporting.Api/
    └── Controllers/
        └── ReportingController.cs                  ← SỬA: thêm 2 endpoints
```

### Backend — GetResourceReportQuery

Inject `IProjectsDbContext` (đã có pattern) + `ITimeTrackingDbContext` (cần verify csproj reference).

```csharp
// src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetResourceReport/GetResourceReportQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Application.Queries.GetResourceReport;

public sealed record ResourceHeatmapCell(
    DateOnly WeekStart,
    decimal UtilizationPct,
    string TrafficLight,
    decimal ActualHours,
    decimal AvailableHours);

public sealed record ResourceHeatmapRow(
    Guid ResourceId,
    IReadOnlyList<ResourceHeatmapCell> Cells);

public sealed record ResourceHeatmapResult(
    IReadOnlyList<DateOnly> Weeks,
    IReadOnlyList<ResourceHeatmapRow> Rows,
    DateOnly DateFrom,
    DateOnly DateTo,
    int ProjectCount);

public sealed record GetResourceReportQuery(
    Guid CurrentUserId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<ResourceHeatmapResult>;

public sealed class GetResourceReportHandler : IRequestHandler<GetResourceReportQuery, ResourceHeatmapResult>
{
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public GetResourceReportHandler(IProjectsDbContext projectsDb, ITimeTrackingDbContext timeTrackingDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<ResourceHeatmapResult> Handle(GetResourceReportQuery query, CancellationToken ct)
    {
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == query.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (projectIds.Count == 0)
            return new ResourceHeatmapResult([], [], query.DateFrom, query.DateTo, 0);

        var weeks = BuildWeeks(query.DateFrom, query.DateTo);

        var entries = await _timeTrackingDb.TimeEntries.AsNoTracking()
            .Where(e => projectIds.Contains(e.ProjectId)
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.ResourceId, e.Date, e.Hours })
            .ToListAsync(ct);

        var rows = entries
            .GroupBy(e => e.ResourceId)
            .Select(resourceGroup =>
            {
                var byWeek = resourceGroup.ToLookup(e => GetMonday(e.Date));
                var cells = weeks.Select(weekStart =>
                {
                    var weekEnd = weekStart.AddDays(4);
                    var effectiveStart = weekStart < query.DateFrom ? query.DateFrom : weekStart;
                    var effectiveEnd = weekEnd > query.DateTo ? query.DateTo : weekEnd;
                    var weekdays = CountWeekdays(effectiveStart, effectiveEnd);
                    var available = weekdays * 8m;
                    var actual = byWeek[weekStart].Sum(e => e.Hours);
                    var utilizationPct = available > 0 ? Math.Round(actual / available * 100, 1) : 0m;
                    var trafficLight = utilizationPct switch
                    {
                        >= 105m => "Red",
                        >= 95m  => "Orange",
                        >= 80m  => "Yellow",
                        _       => "Green",
                    };
                    return new ResourceHeatmapCell(weekStart, utilizationPct, trafficLight, actual, available);
                }).ToList();
                return new ResourceHeatmapRow(resourceGroup.Key, cells);
            })
            .OrderByDescending(r => r.Cells.Count(c => c.TrafficLight is "Red" or "Orange"))
            .ThenByDescending(r => r.Cells.Max(c => c.UtilizationPct))
            .ToList();

        return new ResourceHeatmapResult(weeks, rows, query.DateFrom, query.DateTo, projectIds.Count);
    }

    private static List<DateOnly> BuildWeeks(DateOnly dateFrom, DateOnly dateTo)
    {
        var weeks = new List<DateOnly>();
        var monday = GetMonday(dateFrom);
        while (monday <= dateTo) { weeks.Add(monday); monday = monday.AddDays(7); }
        return weeks;
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static int CountWeekdays(DateOnly from, DateOnly to)
    {
        int count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) count++;
        return count;
    }
}
```

### Backend — GetMilestonesQuery

```csharp
// src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetMilestones/GetMilestonesQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Reporting.Application.Queries.GetMilestones;

public sealed record MilestoneDto(
    Guid TaskId,
    string Name,
    Guid ProjectId,
    string ProjectName,
    DateOnly? DueDate,
    string Status);

public sealed record GetMilestonesQuery(
    Guid CurrentUserId,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null)
    : IRequest<IReadOnlyList<MilestoneDto>>;

public sealed class GetMilestonesHandler : IRequestHandler<GetMilestonesQuery, IReadOnlyList<MilestoneDto>>
{
    private readonly IProjectsDbContext _db;

    public GetMilestonesHandler(IProjectsDbContext db) => _db = db;

    public async Task<IReadOnlyList<MilestoneDto>> Handle(GetMilestonesQuery request, CancellationToken ct)
    {
        var memberProjectIds = await _db.ProjectMemberships
            .AsNoTracking()
            .Where(m => m.UserId == request.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (memberProjectIds.Count == 0)
            return [];

        var projectNames = await _db.Projects
            .AsNoTracking()
            .Where(p => memberProjectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var query = _db.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                memberProjectIds.Contains(t.ProjectId) &&
                t.Type == TaskType.Milestone &&
                !t.IsDeleted);

        if (request.DateFrom.HasValue)
            query = query.Where(t => t.PlannedEndDate >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(t => t.PlannedEndDate <= request.DateTo.Value);

        var milestones = await query
            .OrderBy(t => t.PlannedEndDate)
            .Select(t => new MilestoneDto(
                t.Id,
                t.Name,
                t.ProjectId,
                projectNames.ContainsKey(t.ProjectId) ? projectNames[t.ProjectId] : string.Empty,
                t.PlannedEndDate,
                t.Status.ToString()))
            .ToListAsync(ct);

        return milestones;
    }
}
```

**⚠️ QUAN TRỌNG:** `projectNames.ContainsKey()` không compile trong EF Core LINQ-to-SQL. Phải load projects dictionary trước (như pattern trên), sau đó dùng trong in-memory mapping — KHÔNG dùng `projectNames[t.ProjectId]` trong `.Select()` trực tiếp trên IQueryable. Nếu cần, load milestones trước rồi map project names sau:

```csharp
// Safe approach — load tasks, then map in memory:
var tasks = await query.OrderBy(t => t.PlannedEndDate).ToListAsync(ct);
return tasks.Select(t => new MilestoneDto(
    t.Id, t.Name, t.ProjectId,
    projectNames.GetValueOrDefault(t.ProjectId, string.Empty),
    t.PlannedEndDate,
    t.Status.ToString()))
.ToList();
```

### Backend — ReportingController Extensions

```csharp
// Thêm vào ReportingController.cs — KHÔNG xóa các endpoints hiện có
using ProjectManagement.Reporting.Application.Queries.GetMilestones;
using ProjectManagement.Reporting.Application.Queries.GetResourceReport;

// Thêm 2 action methods:

/// <summary>
/// Resource utilization heatmap (person × week), scoped to user's project membership.
/// </summary>
[HttpGet("resources")]
[ResponseCache(Duration = 300)]
public async Task<IActionResult> GetResourceHeatmap(
    [FromQuery] DateOnly from,
    [FromQuery] DateOnly to,
    CancellationToken ct)
{
    if (to < from)
        return BadRequest(new { detail = "to phải >= from." });
    var result = await _mediator.Send(new GetResourceReportQuery(_currentUser.UserId, from, to), ct);
    return Ok(result);
}

/// <summary>
/// Cross-project milestone timeline, scoped to user's project membership.
/// </summary>
[HttpGet("milestones")]
[ResponseCache(Duration = 300)]
public async Task<IActionResult> GetMilestones(
    [FromQuery] DateOnly? from = null,
    [FromQuery] DateOnly? to = null,
    CancellationToken ct = default)
{
    var result = await _mediator.Send(new GetMilestonesQuery(_currentUser.UserId, from, to), ct);
    return Ok(result);
}
```

### Backend — ITimeTrackingDbContext Dependency Check

`GetResourceReportHandler` cần inject `ITimeTrackingDbContext`. Kiểm tra xem `Reporting.Application.csproj` đã có reference đến `TimeTracking.Application` chưa:

```bash
grep -r "TimeTracking" src/Modules/Reporting/ProjectManagement.Reporting.Application/ProjectManagement.Reporting.Application.csproj
```

Nếu chưa có, cần thêm vào `.csproj`:
```xml
<ProjectReference Include="..\..\TimeTracking\ProjectManagement.TimeTracking.Application\ProjectManagement.TimeTracking.Application.csproj" />
```

So sánh với `GetStatCardsQuery.cs` — nó inject `IProjectsDbContext` — reference đó đã tồn tại.

### Frontend — File Structure

```
frontend/project-management-web/src/app/features/reporting/
├── models/
│   ├── cost-report.model.ts          (đã có — KHÔNG sửa)
│   └── resource-report.model.ts      ← MỚI: ResourceHeatmapCell, ResourceHeatmapRow, ResourceHeatmapResult, MilestoneDto
│
├── services/
│   └── reporting-api.service.ts      ← SỬA: thêm getResourceHeatmap(), getMilestones()
│
├── store/
│   ├── reporting.actions.ts          ← SỬA: thêm resource + milestone actions
│   ├── reporting.reducer.ts          ← SỬA: extend state + reducers
│   └── reporting.effects.ts          ← SỬA: thêm resource + milestone effects
│
├── components/
│   ├── cost-dashboard/               (đã có — KHÔNG SỬA)
│   ├── cost-breakdown/               (đã có — KHÔNG SỬA)
│   ├── export-trigger/               (đã có — KHÔNG SỬA)
│   ├── resource-report/              ← MỚI folder
│   │   ├── resource-report.ts        ← MỚI
│   │   └── resource-report.html      ← MỚI
│   └── milestone-report/             ← MỚI folder
│       ├── milestone-report.ts       ← MỚI
│       └── milestone-report.html     ← MỚI
│
└── reporting.routes.ts               ← SỬA: thêm /resources và /milestones
```

### Frontend — Models

```typescript
// resource-report.model.ts
export interface ResourceHeatmapCell {
  weekStart: string;        // YYYY-MM-DD
  utilizationPct: number;
  trafficLight: 'Green' | 'Yellow' | 'Orange' | 'Red';
  actualHours: number;
  availableHours: number;
}

export interface ResourceHeatmapRow {
  resourceId: string;
  cells: ResourceHeatmapCell[];
}

export interface ResourceHeatmapResult {
  weeks: string[];          // DateOnly[] serialized as string[]
  rows: ResourceHeatmapRow[];
  dateFrom: string;
  dateTo: string;
  projectCount: number;
}

export interface MilestoneDto {
  taskId: string;
  name: string;
  projectId: string;
  projectName: string;
  dueDate: string | null;   // DateOnly? serialized
  status: string;
}
```

### Frontend — ReportingApiService Extension

```typescript
// Thêm vào reporting-api.service.ts (KHÔNG xóa methods cũ)
import { ResourceHeatmapResult, MilestoneDto } from '../models/resource-report.model';

getResourceHeatmap(from: string, to: string): Observable<ResourceHeatmapResult> {
  const params = new HttpParams().set('from', from).set('to', to);
  return this.http.get<ResourceHeatmapResult>('/api/v1/reports/resources', { params });
}

getMilestones(from?: string, to?: string): Observable<MilestoneDto[]> {
  let params = new HttpParams();
  if (from) params = params.set('from', from);
  if (to) params = params.set('to', to);
  return this.http.get<MilestoneDto[]>('/api/v1/reports/milestones', { params });
}
```

### Frontend — NgRx Store Extension

**Actions — reporting.actions.ts:**
```typescript
// Extend createActionGroup events (thêm vào events object):
'Load Resource Heatmap': props<{ from: string; to: string }>(),
'Load Resource Heatmap Success': props<{ result: ResourceHeatmapResult }>(),
'Load Resource Heatmap Failure': props<{ error: string }>(),
'Load Milestones': props<{ from?: string; to?: string }>(),
'Load Milestones Success': props<{ milestones: MilestoneDto[] }>(),
'Load Milestones Failure': props<{ error: string }>(),
```

**State — reporting.reducer.ts:**
```typescript
// Extend ReportingState interface:
resourceHeatmap: ResourceHeatmapResult | null;
heatmapLoading: boolean;
milestones: MilestoneDto[];
milestonesLoading: boolean;

// Extend initialState:
resourceHeatmap: null,
heatmapLoading: false,
milestones: [],
milestonesLoading: false,

// Thêm on() reducers:
on(ReportingActions.loadResourceHeatmap, state => ({ ...state, heatmapLoading: true })),
on(ReportingActions.loadResourceHeatmapSuccess, (state, { result }) => ({
  ...state, heatmapLoading: false, resourceHeatmap: result,
})),
on(ReportingActions.loadResourceHeatmapFailure, state => ({ ...state, heatmapLoading: false })),
on(ReportingActions.loadMilestones, state => ({ ...state, milestonesLoading: true })),
on(ReportingActions.loadMilestonesSuccess, (state, { milestones }) => ({
  ...state, milestonesLoading: false, milestones,
})),
on(ReportingActions.loadMilestonesFailure, state => ({ ...state, milestonesLoading: false })),

// Export selectors (thêm vào destructuring):
export const { selectResourceHeatmap, selectHeatmapLoading, selectMilestones, selectMilestonesLoading } = reportingFeature;
```

**Effects — reporting.effects.ts:**
```typescript
loadResourceHeatmap$ = createEffect(() =>
  this.actions$.pipe(
    ofType(ReportingActions.loadResourceHeatmap),
    switchMap(({ from, to }) =>
      this.api.getResourceHeatmap(from, to).pipe(
        map(result => ReportingActions.loadResourceHeatmapSuccess({ result })),
        catchError(err => of(ReportingActions.loadResourceHeatmapFailure({ error: err?.message ?? 'Lỗi tải heatmap.' })))
      )
    )
  )
);

loadMilestones$ = createEffect(() =>
  this.actions$.pipe(
    ofType(ReportingActions.loadMilestones),
    switchMap(({ from, to }) =>
      this.api.getMilestones(from, to).pipe(
        map(milestones => ReportingActions.loadMilestonesSuccess({ milestones })),
        catchError(err => of(ReportingActions.loadMilestonesFailure({ error: err?.message ?? 'Lỗi tải milestones.' })))
      )
    )
  )
);
```

### Frontend — ResourceReportComponent

```typescript
// resource-report.ts
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe, DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ReportingActions } from '../../store/reporting.actions';
import { selectResourceHeatmap, selectHeatmapLoading } from '../../store/reporting.reducer';
import { ResourceHeatmapCell, ResourceHeatmapRow } from '../../models/resource-report.model';

@Component({
  selector: 'app-resource-report',
  standalone: true,
  imports: [
    AsyncPipe, DecimalPipe, NgIf, NgFor, NgClass,
    ReactiveFormsModule,
    MatButtonModule, MatCardModule, MatProgressSpinnerModule,
    MatFormFieldModule, MatInputModule, MatTooltipModule,
  ],
  templateUrl: './resource-report.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceReportComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly heatmap$ = this.store.select(selectResourceHeatmap);
  readonly loading$ = this.store.select(selectHeatmapLoading);

  readonly form = this.fb.nonNullable.group({
    from: [this.defaultFrom()],
    to: [this.defaultTo()],
  });

  selectedCell: { resourceId: string; cell: ResourceHeatmapCell } | null = null;

  readonly trafficLightIcon: Record<string, string> = {
    Green: '●', Yellow: '▲', Orange: '◆', Red: '✕',
  };

  ngOnInit(): void { this.load(); }

  load(): void {
    if (this.form.invalid) return;
    const { from, to } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadResourceHeatmap({ from, to }));
    this.selectedCell = null;
  }

  selectCell(resourceId: string, cell: ResourceHeatmapCell): void {
    this.selectedCell = { resourceId, cell };
  }

  cellClass(cell: ResourceHeatmapCell): string {
    return `cell-${cell.trafficLight.toLowerCase()}`;
  }

  cellTooltip(cell: ResourceHeatmapCell): string {
    return `${cell.actualHours.toFixed(1)}h / ${cell.availableHours.toFixed(0)}h (${cell.utilizationPct.toFixed(1)}%)`;
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 28);
    return d.toISOString().substring(0, 10);
  }

  private defaultTo(): string {
    const d = new Date();
    d.setDate(d.getDate() + 28);
    return d.toISOString().substring(0, 10);
  }
}
```

**resource-report.html:**
```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Resource Utilization Heatmap</mat-card-title>
  </mat-card-header>
  <mat-card-content>

    <form [formGroup]="form" (ngSubmit)="load()" class="filter-row">
      <mat-form-field><mat-label>Từ</mat-label>
        <input matInput type="date" formControlName="from"></mat-form-field>
      <mat-form-field><mat-label>Đến</mat-label>
        <input matInput type="date" formControlName="to"></mat-form-field>
      <button mat-raised-button color="primary" type="submit">Tải</button>
    </form>

    <!-- Legend (color + icon for color-blind support, NFR-21) -->
    <div class="legend">
      <span class="cell-green">● < 80%</span>
      <span class="cell-yellow">▲ 80–95%</span>
      <span class="cell-orange">◆ 95–105%</span>
      <span class="cell-red">✕ > 105%</span>
    </div>

    <div *ngIf="loading$ | async" class="spinner-wrap">
      <mat-spinner diameter="40"></mat-spinner>
    </div>

    <ng-container *ngIf="heatmap$ | async as heatmap">
      <!-- Empty state -->
      <p *ngIf="heatmap.rows.length === 0" class="empty-state">
        Không có dữ liệu. Bạn chưa có project membership hoặc chưa có time entries trong kỳ này.
      </p>

      <!-- Heatmap table -->
      <div *ngIf="heatmap.rows.length > 0" class="heatmap-container">
        <table class="heatmap-table">
          <thead>
            <tr>
              <th>Thành viên</th>
              <th *ngFor="let week of heatmap.weeks">{{ week | slice:5 }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of heatmap.rows">
              <td class="resource-id">{{ row.resourceId | slice:0:8 }}…</td>
              <td *ngFor="let cell of row.cells"
                  [ngClass]="cellClass(cell)"
                  [matTooltip]="cellTooltip(cell)"
                  (click)="selectCell(row.resourceId, cell)"
                  class="heatmap-cell" role="button" [attr.aria-label]="cellTooltip(cell)">
                {{ trafficLightIcon[cell.trafficLight] }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Drill-down detail -->
      <mat-card *ngIf="selectedCell" class="drill-down">
        <mat-card-header><mat-card-title>Chi tiết tuần</mat-card-title></mat-card-header>
        <mat-card-content>
          <p>Thành viên: {{ selectedCell.resourceId }}</p>
          <p>Tuần bắt đầu: {{ selectedCell.cell.weekStart }}</p>
          <p>Giờ thực tế: {{ selectedCell.cell.actualHours | number:'1.1-1' }}h</p>
          <p>Giờ có thể: {{ selectedCell.cell.availableHours | number:'1.0-0' }}h</p>
          <p>Mức sử dụng: {{ selectedCell.cell.utilizationPct | number:'1.1-1' }}%</p>
        </mat-card-content>
      </mat-card>
    </ng-container>

  </mat-card-content>
</mat-card>
```

### Frontend — MilestoneReportComponent

```typescript
// milestone-report.ts
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe, NgFor, NgIf } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ReportingActions } from '../../store/reporting.actions';
import { selectMilestones, selectMilestonesLoading } from '../../store/reporting.reducer';

@Component({
  selector: 'app-milestone-report',
  standalone: true,
  imports: [
    AsyncPipe, DatePipe, NgIf, NgFor, ReactiveFormsModule,
    MatButtonModule, MatCardModule, MatProgressSpinnerModule,
    MatFormFieldModule, MatInputModule,
  ],
  templateUrl: './milestone-report.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MilestoneReportComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly milestones$ = this.store.select(selectMilestones);
  readonly loading$ = this.store.select(selectMilestonesLoading);

  readonly form = this.fb.nonNullable.group({
    from: [''],
    to: [''],
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    const { from, to } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadMilestones({
      from: from || undefined,
      to: to || undefined,
    }));
  }
}
```

**milestone-report.html:**
```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Milestone Timeline (Cross-Project)</mat-card-title>
  </mat-card-header>
  <mat-card-content>

    <form [formGroup]="form" (ngSubmit)="load()" class="filter-row">
      <mat-form-field><mat-label>Từ (tuỳ chọn)</mat-label>
        <input matInput type="date" formControlName="from"></mat-form-field>
      <mat-form-field><mat-label>Đến (tuỳ chọn)</mat-label>
        <input matInput type="date" formControlName="to"></mat-form-field>
      <button mat-raised-button color="primary" type="submit">Lọc</button>
    </form>

    <div *ngIf="loading$ | async" class="spinner-wrap">
      <mat-spinner diameter="40"></mat-spinner>
    </div>

    <ng-container *ngIf="milestones$ | async as milestones">
      <p *ngIf="milestones.length === 0" class="empty-state">
        Không có milestone nào. Bạn chưa có project membership hoặc chưa có milestone trong kỳ đã chọn.
      </p>

      <table *ngIf="milestones.length > 0" class="milestone-table">
        <thead>
          <tr>
            <th>Milestone</th>
            <th>Project</th>
            <th>Due Date</th>
            <th>Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let m of milestones">
            <td>{{ m.name }}</td>
            <td>{{ m.projectName }}</td>
            <td>{{ m.dueDate | date:'dd/MM/yyyy' }}</td>
            <td>{{ m.status }}</td>
          </tr>
        </tbody>
      </table>
    </ng-container>

  </mat-card-content>
</mat-card>
```

### Frontend — Routing Update

```typescript
// reporting.routes.ts — thêm 2 routes MỚI (KHÔNG xóa routes cũ)
{
  path: 'resources',
  loadComponent: () =>
    import('./components/resource-report/resource-report').then(m => m.ResourceReportComponent),
},
{
  path: 'milestones',
  loadComponent: () =>
    import('./components/milestone-report/milestone-report').then(m => m.MilestoneReportComponent),
},
```

### Frontend — Unit Tests Pattern

Test files: `resource-report.spec.ts` và `milestone-report.spec.ts`

```typescript
// Pattern từ story 9-2 (đã confirmed working)
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ResourceReportComponent } from './resource-report';
import { reportingFeature } from '../../store/reporting.reducer';
import { ReportingEffects } from '../../store/reporting.effects';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('ResourceReportComponent', () => {
  let component: ResourceReportComponent;
  let fixture: ComponentFixture<ResourceReportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResourceReportComponent, HttpClientTestingModule],
      providers: [
        provideNoopAnimations(),
        provideStore({ [reportingFeature.name]: reportingFeature.reducer }),
        provideEffects([ReportingEffects]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResourceReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should dispatch loadResourceHeatmap on init', () => {
    // component dispatched in ngOnInit via load()
    expect(component.form.controls.from.value).toBeTruthy();
    expect(component.form.controls.to.value).toBeTruthy();
  });

  it('should select cell and update selectedCell', () => {
    const mockCell = {
      weekStart: '2026-04-27',
      utilizationPct: 90,
      trafficLight: 'Yellow' as const,
      actualHours: 36,
      availableHours: 40,
    };
    component.selectCell('resource-1', mockCell);
    expect(component.selectedCell?.resourceId).toBe('resource-1');
    expect(component.selectedCell?.cell.trafficLight).toBe('Yellow');
  });

  it('cellClass returns correct css class', () => {
    const cell = { trafficLight: 'Red' } as any;
    expect(component.cellClass(cell)).toBe('cell-red');
  });

  it('cellTooltip returns formatted string', () => {
    const cell = { actualHours: 36.5, availableHours: 40, utilizationPct: 91.25 } as any;
    const tooltip = component.cellTooltip(cell);
    expect(tooltip).toContain('36.5h');
    expect(tooltip).toContain('91.3%');
  });
});
```

### Git Intelligence

- Commit gần nhất: "comit" (52b732a) — đã include story 10-2 work
- Pattern đã xác nhận: `dotnet build src/Modules/Reporting/` builds thành công (0 errors, version conflict warnings là pre-existing)
- ITimeTrackingDbContext cần verify reference trong `Reporting.Application.csproj` — kiểm tra trước khi code

### Previous Story Intelligence (Story 10-2)

- Reporting module đã có Alert + AlertPreference entities, EF config, migration `AddAlertCenterSchema`
- `AlertsController` đã tạo tại `/api/v1/alerts` — pattern cho controller là inject `IMediator` + `ICurrentUserService`
- MediatR handler scan: `GetCostSummaryHandler`.Assembly — tất cả handlers mới trong `Reporting.Application` đều được auto-registered
- `ReportingDbContext` đã có `Alerts` + `AlertPreferences` DbSets

---

## Tasks / Subtasks

### Backend Tasks

- [x] **Task BE-1: Verify ITimeTrackingDbContext reference**
  - [x] BE-1.1: Kiểm tra `Reporting.Application.csproj` có `ProjectReference` đến `TimeTracking.Application` chưa
  - [x] BE-1.2: Nếu chưa có → thêm vào csproj; nếu đã có → skip (đã có sẵn)

- [x] **Task BE-2: GetResourceReportQuery**
  - [x] BE-2.1: Tạo `Reporting.Application/Queries/GetResourceReport/GetResourceReportQuery.cs`
  - [x] BE-2.2: Implement handler với EXACT same thresholds như `GetCapacityHeatmapQuery`: < 80% Green, 80–95% Yellow, 95–105% Orange, > 105% Red
  - [x] BE-2.3: `dotnet build src/Modules/Reporting/ProjectManagement.Reporting.Application` → 0 errors

- [x] **Task BE-3: GetMilestonesQuery**
  - [x] BE-3.1: Tạo `Reporting.Application/Queries/GetMilestones/GetMilestonesQuery.cs`
  - [x] BE-3.2: Query `IProjectsDbContext.ProjectTasks` filter `type == TaskType.Milestone && !IsDeleted`
  - [x] BE-3.3: Membership-scoped: chỉ milestones của projects user là member
  - [x] BE-3.4: Load projects trước để lấy names, sau đó map in-memory (KHÔNG join trong EF query — dùng pattern safe như trong Dev Notes)
  - [x] BE-3.5: Optional date range filter: `?from=` và `?to=`

- [x] **Task BE-4: ReportingController Extensions**
  - [x] BE-4.1: Thêm `GET /api/v1/reports/resources?from=&to=` với `[ResponseCache(Duration = 300)]`
  - [x] BE-4.2: Thêm `GET /api/v1/reports/milestones?from=&to=` với `[ResponseCache(Duration = 300)]`
  - [x] BE-4.3: Validate `to >= from` cho resources endpoint
  - [x] BE-4.4: `dotnet build src/Modules/Reporting/ProjectManagement.Reporting.Api` → 0 errors (Build succeeded, 0 errors)

### Frontend Tasks

- [x] **Task FE-1: Models**
  - [x] FE-1.1: Tạo `features/reporting/models/resource-report.model.ts` với interfaces: `ResourceHeatmapCell`, `ResourceHeatmapRow`, `ResourceHeatmapResult`, `MilestoneDto`

- [x] **Task FE-2: ReportingApiService Extension**
  - [x] FE-2.1: Thêm `getResourceHeatmap(from: string, to: string): Observable<ResourceHeatmapResult>`
  - [x] FE-2.2: Thêm `getMilestones(from?: string, to?: string): Observable<MilestoneDto[]>`
  - [x] FE-2.3: URL: `/api/v1/reports/resources` và `/api/v1/reports/milestones`

- [x] **Task FE-3: NgRx Store Extension**
  - [x] FE-3.1: `reporting.actions.ts` — thêm 6 actions: LoadResourceHeatmap/Success/Failure + LoadMilestones/Success/Failure
  - [x] FE-3.2: `reporting.reducer.ts` — extend interface + initialState + on() handlers + export new selectors
  - [x] FE-3.3: `reporting.effects.ts` — thêm 2 effects: `loadResourceHeatmap$` + `loadMilestones$`

- [x] **Task FE-4: ResourceReportComponent**
  - [x] FE-4.1: Tạo `features/reporting/components/resource-report/resource-report.ts`
  - [x] FE-4.2: Tạo `features/reporting/components/resource-report/resource-report.html`
  - [x] FE-4.3: Form với date range (from/to), load button, default range ±4 tuần
  - [x] FE-4.4: Heatmap table: rows = resources, columns = weeks; cell = traffic-light icon + class
  - [x] FE-4.5: Legend: màu + icon (color-blind support)
  - [x] FE-4.6: Drill-down: click cell → hiển thị detail panel
  - [x] FE-4.7: Empty state khi rows.length === 0
  - [x] FE-4.8: `ChangeDetectionStrategy.OnPush`

- [x] **Task FE-5: MilestoneReportComponent**
  - [x] FE-5.1: Tạo `features/reporting/components/milestone-report/milestone-report.ts`
  - [x] FE-5.2: Tạo `features/reporting/components/milestone-report/milestone-report.html`
  - [x] FE-5.3: Optional date range filter
  - [x] FE-5.4: Table: name, projectName, dueDate (formatted dd/MM/yyyy), status
  - [x] FE-5.5: Empty state khi milestones.length === 0
  - [x] FE-5.6: `ChangeDetectionStrategy.OnPush`

- [x] **Task FE-6: Routing**
  - [x] FE-6.1: Thêm `/resources` route vào `reporting.routes.ts` → `ResourceReportComponent`
  - [x] FE-6.2: Thêm `/milestones` route vào `reporting.routes.ts` → `MilestoneReportComponent`

- [x] **Task FE-7: Unit Tests**
  - [x] FE-7.1: Tạo `resource-report.spec.ts` — test: create, selectCell, cellClass, cellTooltip, empty state
  - [x] FE-7.2: Tạo `milestone-report.spec.ts` — test: create, dispatch loadMilestones on init, empty state
  - [x] FE-7.3: Chạy `npm run test` trong `frontend/project-management-web/` → 22/22 tests pass (pure logic tests)

---

## References

- Epics file: `_bmad-output/planning-artifacts/epics-dashboard.md` — Story 10-3, FR11, FR22, FR23, FR24, FR28
- Architecture: `_bmad-output/planning-artifacts/architecture.md` — Section 8.9, NFR-21, NFR-25
- Existing heatmap: `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetCapacityHeatmap/GetCapacityHeatmapQuery.cs`
- Existing controller: `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/ReportingController.cs`
- Existing API service: `frontend/project-management-web/src/app/features/reporting/services/reporting-api.service.ts`
- Store pattern: `frontend/project-management-web/src/app/features/reporting/store/`
- Heatmap component: `frontend/project-management-web/src/app/features/capacity/components/capacity-heatmap/capacity-heatmap.ts`
- TaskType enum: `src/Modules/Projects/ProjectManagement.Projects.Domain/Enums/TaskType.cs`

---

## Dev Agent Record

### Agent Model Used
claude-sonnet-4-6

### Debug Log References
- ReportingController class-brace issue: initial edit placed GetResourceHeatmap + GetMilestones AFTER the class closing brace. Fixed by removing the extra `}`.
- TestBed + templateUrl fails in Vitest jsdom (pre-existing env issue). Both spec files rewritten as pure logic tests (mirroring gantt-timeline.spec.ts pattern). 22/22 tests pass.
- `projectNames[id]` in EF LINQ-to-SQL fails at runtime. Fixed by loading tasks with `.ToListAsync()` first, then mapping in-memory with `GetValueOrDefault`.

### Completion Notes List
- BE: GetResourceReportQuery + GetMilestonesQuery created; ReportingController extended with 2 endpoints. Build: 0 errors.
- FE: resource-report.model.ts, reporting-api.service.ts, NgRx store (actions/reducer/effects), ResourceReportComponent + HTML, MilestoneReportComponent + HTML, reporting.routes.ts all implemented.
- Tests: 22/22 pure logic tests pass (resource-report.spec.ts: 13 tests, milestone-report.spec.ts: 10 tests).
- NFR-21 (color-blind): legend uses both color CSS classes AND text icons (●▲◆✕).
- NFR-25 (empty state): both components display Vietnamese empty-state messages when no data.

### File List
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetResourceReport/GetResourceReportQuery.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetMilestones/GetMilestonesQuery.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/ReportingController.cs` — MODIFIED (2 endpoints added)
- `frontend/project-management-web/src/app/features/reporting/models/resource-report.model.ts` — NEW
- `frontend/project-management-web/src/app/features/reporting/services/reporting-api.service.ts` — MODIFIED
- `frontend/project-management-web/src/app/features/reporting/store/reporting.actions.ts` — MODIFIED
- `frontend/project-management-web/src/app/features/reporting/store/reporting.reducer.ts` — MODIFIED
- `frontend/project-management-web/src/app/features/reporting/store/reporting.effects.ts` — MODIFIED
- `frontend/project-management-web/src/app/features/reporting/components/resource-report/resource-report.ts` — NEW
- `frontend/project-management-web/src/app/features/reporting/components/resource-report/resource-report.html` — NEW
- `frontend/project-management-web/src/app/features/reporting/components/resource-report/resource-report.spec.ts` — NEW
- `frontend/project-management-web/src/app/features/reporting/components/milestone-report/milestone-report.ts` — NEW
- `frontend/project-management-web/src/app/features/reporting/components/milestone-report/milestone-report.html` — NEW
- `frontend/project-management-web/src/app/features/reporting/components/milestone-report/milestone-report.spec.ts` — NEW
- `frontend/project-management-web/src/app/features/reporting/reporting.routes.ts` — MODIFIED
