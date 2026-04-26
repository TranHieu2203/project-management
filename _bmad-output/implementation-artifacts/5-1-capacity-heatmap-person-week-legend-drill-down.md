# Story 5.1: Capacity heatmap (person × week) + legend + drill-down

Status: review

**Story ID:** 5.1
**Epic:** Epic 5 — Capacity Planning Suite (Heatmap + 4-week Forecast)
**Sprint:** Sprint 6
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want xem heatmap capacity theo tuần với legend rõ,
So that tôi quyết định phân bổ dựa trên capacity trước.

## Acceptance Criteria

1. **Given** range 4–8 tuần (đến 8 cột tuần)
   **When** render heatmap
   **Then** mỗi ô (resource × week) hiển thị utilization% + trafficLight color
   **And** legend rõ ràng: Green <80%, Yellow 80–95%, Orange 95–105%, Red >105%

2. **Given** tooltip/label accessibility
   **When** hover hoặc đọc bàn phím
   **Then** màu KHÔNG là kênh duy nhất — có icon/pattern + tooltip giải thích giờ thực/giờ available
   **And** hỗ trợ người mù màu (pattern hoặc ký tự: ● ▲ ◆ ✕)

3. **Given** drill-down
   **When** PM click vào ô
   **Then** hiển thị chi tiết của resource đó trong tuần đó (actualHours, availableHours, utilization%)
   **And** drill-down chỉ trong scope membership (heatmap chỉ chứa resources của projects user là member)

4. **Given** endpoint không nhận `projectId` tham số
   **When** PM cố tình thêm projectId lạ
   **Then** data vẫn chỉ scoped theo membership — không leak project ngoài scope

5. **Given** loading và empty state
   **When** không có time entries
   **Then** bảng vẫn render với rows = [] và thông báo "Không có dữ liệu" rõ ràng

---

## Tasks / Subtasks

- [x] **Task 1: Capacity.Application — GetCapacityHeatmapQuery**
  - [x] 1.1 Tạo `Queries/GetCapacityHeatmap/GetCapacityHeatmapQuery.cs` + handler
  - [x] 1.2 Handler dùng cùng membership-scoping pattern với `GetCrossProjectOverloadQuery` (projectIds từ `_projectsDb.ProjectMemberships`)
  - [x] 1.3 Compute per week × resource: actualHours, availableHours (weekdays×8), utilizationPct, trafficLight
  - [x] 1.4 Build weeks list: tất cả Mondays trong `[DateFrom, DateTo]` (dùng `GetMonday()` helper)

- [x] **Task 2: Capacity.Api — endpoint**
  - [x] 2.1 Thêm `GET /api/v1/capacity/heatmap?dateFrom=&dateTo=` vào `CapacityController`
  - [x] 2.2 Controller dùng `_currentUser.UserId` — không nhận projectId param

- [x] **Task 3: Frontend — models**
  - [x] 3.1 Thêm `HeatmapCell`, `HeatmapRow`, `CapacityHeatmapResult` vào `utilization.model.ts`

- [x] **Task 4: Frontend — API service**
  - [x] 4.1 Thêm `getCapacityHeatmap(dateFrom, dateTo)` vào `capacity-api.service.ts`

- [x] **Task 5: Frontend — NgRx store**
  - [x] 5.1 Thêm actions: `loadHeatmap`, `loadHeatmapSuccess`, `loadHeatmapFailure`
  - [x] 5.2 Mở rộng `CapacityState` + reducer: `heatmap: CapacityHeatmapResult | null`, `heatmapLoading: boolean`
  - [x] 5.3 Export `selectHeatmap`, `selectHeatmapLoading` từ `capacityFeature`
  - [x] 5.4 Thêm effect `loadHeatmap$`

- [x] **Task 6: Frontend — CapacityHeatmapComponent + routing**
  - [x] 6.1 Tạo `components/capacity-heatmap/capacity-heatmap.ts` (standalone, OnPush)
  - [x] 6.2 Tạo `components/capacity-heatmap/capacity-heatmap.html` — table heatmap + legend + drill-down panel
  - [x] 6.3 Cập nhật `capacity.routes.ts` — thêm route `heatmap`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors
  - [x] 7.2 `ng build` → 0 errors

---

## Dev Notes

### Backend — GetCapacityHeatmapQuery (Task 1)

**File:** `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetCapacityHeatmap/GetCapacityHeatmapQuery.cs`

Không cần csproj mới — `Capacity.Application.csproj` đã có refs tới `Projects.Application` + `TimeTracking.Application` từ Story 4.4.

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetCapacityHeatmap;

public sealed record HeatmapCell(
    DateOnly WeekStart,
    decimal UtilizationPct,
    string TrafficLight,
    decimal ActualHours,
    decimal AvailableHours);

public sealed record HeatmapRow(
    Guid ResourceId,
    IReadOnlyList<HeatmapCell> Cells);

public sealed record CapacityHeatmapResult(
    IReadOnlyList<DateOnly> Weeks,
    IReadOnlyList<HeatmapRow> Rows,
    DateOnly DateFrom,
    DateOnly DateTo,
    int ProjectCount);

public sealed record GetCapacityHeatmapQuery(
    Guid CurrentUserId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<CapacityHeatmapResult>;

public sealed class GetCapacityHeatmapHandler
    : IRequestHandler<GetCapacityHeatmapQuery, CapacityHeatmapResult>
{
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public GetCapacityHeatmapHandler(
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<CapacityHeatmapResult> Handle(
        GetCapacityHeatmapQuery query, CancellationToken ct)
    {
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == query.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (projectIds.Count == 0)
            return new CapacityHeatmapResult([], [], query.DateFrom, query.DateTo, 0);

        // Build week list: all Mondays in [DateFrom, DateTo]
        var weeks = BuildWeeks(query.DateFrom, query.DateTo);

        var entries = await _timeTrackingDb.TimeEntries.AsNoTracking()
            .Where(e => projectIds.Contains(e.ProjectId)
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.ResourceId, e.Date, e.Hours })
            .ToListAsync(ct);

        // Group by resource → by week → compute utilization
        var rows = entries
            .GroupBy(e => e.ResourceId)
            .Select(resourceGroup =>
            {
                var byWeek = resourceGroup.ToLookup(e => GetMonday(e.Date));

                var cells = weeks.Select(weekStart =>
                {
                    var weekEnd = weekStart.AddDays(4); // Friday
                    // Clamp to query range
                    var effectiveStart = weekStart < query.DateFrom ? query.DateFrom : weekStart;
                    var effectiveEnd = weekEnd > query.DateTo ? query.DateTo : weekEnd;

                    var weekdays = CountWeekdays(effectiveStart, effectiveEnd);
                    var available = weekdays * 8m;
                    var actual = byWeek[weekStart].Sum(e => e.Hours);

                    var utilizationPct = available > 0
                        ? Math.Round(actual / available * 100, 1)
                        : 0m;

                    var trafficLight = utilizationPct switch
                    {
                        >= 105m => "Red",
                        >= 95m  => "Orange",
                        >= 80m  => "Yellow",
                        _       => "Green",
                    };

                    return new HeatmapCell(weekStart, utilizationPct, trafficLight, actual, available);
                }).ToList();

                return new HeatmapRow(resourceGroup.Key, cells);
            })
            .OrderByDescending(r => r.Cells.Count(c => c.TrafficLight is "Red" or "Orange"))
            .ThenByDescending(r => r.Cells.Max(c => c.UtilizationPct))
            .ToList();

        return new CapacityHeatmapResult(weeks, rows, query.DateFrom, query.DateTo, projectIds.Count);
    }

    private static List<DateOnly> BuildWeeks(DateOnly dateFrom, DateOnly dateTo)
    {
        var weeks = new List<DateOnly>();
        var monday = GetMonday(dateFrom);
        while (monday <= dateTo)
        {
            weeks.Add(monday);
            monday = monday.AddDays(7);
        }
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
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        return count;
    }
}
```

**Ordering logic:** Rows sorted by number of Red/Orange cells (most overloaded first), then by max utilization% — so critical resources appear at the top.

### Backend — CapacityController endpoint (Task 2)

Thêm vào `CapacityController.cs` sau `GetCrossProjectOverload`:

```csharp
using ProjectManagement.Capacity.Application.Queries.GetCapacityHeatmap;

// Add to using block at top of file ↑

/// <summary>
/// Capacity heatmap: person × week utilization, scoped to user's project membership.
/// </summary>
[HttpGet("heatmap")]
public async Task<IActionResult> GetHeatmap(
    [FromQuery] DateOnly dateFrom,
    [FromQuery] DateOnly dateTo,
    CancellationToken ct)
{
    if (dateTo < dateFrom)
        return BadRequest(new { detail = "dateTo phải >= dateFrom." });

    var result = await _mediator.Send(
        new GetCapacityHeatmapQuery(_currentUser.UserId, dateFrom, dateTo), ct);
    return Ok(result);
}
```

### Frontend — Models (Task 3)

Thêm vào cuối `utilization.model.ts`:

```typescript
export interface HeatmapCell {
  weekStart: string;        // 'YYYY-MM-DD' (Monday)
  utilizationPct: number;
  trafficLight: TrafficLightStatus;
  actualHours: number;
  availableHours: number;
}

export interface HeatmapRow {
  resourceId: string;
  cells: HeatmapCell[];
}

export interface CapacityHeatmapResult {
  weeks: string[];          // Mondays as 'YYYY-MM-DD'
  rows: HeatmapRow[];
  dateFrom: string;
  dateTo: string;
  projectCount: number;
}
```

### Frontend — API Service (Task 4)

Thêm vào `capacity-api.service.ts`:

```typescript
import { CapacityHeatmapResult } from '../models/utilization.model';  // add to existing import

getCapacityHeatmap(dateFrom: string, dateTo: string): Observable<CapacityHeatmapResult> {
  const params = new HttpParams().set('dateFrom', dateFrom).set('dateTo', dateTo);
  return this.http.get<CapacityHeatmapResult>('/api/v1/capacity/heatmap', { params });
}
```

### Frontend — NgRx store (Task 5)

**capacity.actions.ts** — thêm vào `events`:
```typescript
import { CapacityHeatmapResult } from '../models/utilization.model';  // add to import

// Thêm vào events object:
'Load Heatmap': props<{ dateFrom: string; dateTo: string }>(),
'Load Heatmap Success': props<{ result: CapacityHeatmapResult }>(),
'Load Heatmap Failure': props<{ error: string }>(),
```

**capacity.reducer.ts** — state + reducer + selector:
```typescript
import { CapacityHeatmapResult } from '../models/utilization.model';  // add to import

// State interface — thêm 2 field:
heatmap: CapacityHeatmapResult | null;
heatmapLoading: boolean;

// initialState — thêm:
heatmap: null,
heatmapLoading: false,

// Reducer cases — thêm:
on(CapacityActions.loadHeatmap, state => ({ ...state, heatmapLoading: true })),
on(CapacityActions.loadHeatmapSuccess, (state, { result }) => ({
  ...state, heatmapLoading: false, heatmap: result,
})),
on(CapacityActions.loadHeatmapFailure, state => ({ ...state, heatmapLoading: false })),

// Export selectors — thêm:
selectHeatmap,
selectHeatmapLoading,
```

**capacity.effects.ts** — thêm effect:
```typescript
loadHeatmap$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.loadHeatmap),
    switchMap(({ dateFrom, dateTo }) =>
      this.api.getCapacityHeatmap(dateFrom, dateTo).pipe(
        map(result => CapacityActions.loadHeatmapSuccess({ result })),
        catchError(err => of(CapacityActions.loadHeatmapFailure({ error: err?.message ?? 'Lỗi tải heatmap.' })))
      )
    )
  )
);
```

### Frontend — CapacityHeatmapComponent (Task 6)

**File:** `frontend/project-management-web/src/app/features/capacity/components/capacity-heatmap/capacity-heatmap.ts`

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AsyncPipe, DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CapacityActions } from '../../store/capacity.actions';
import { selectHeatmap, selectHeatmapLoading } from '../../store/capacity.reducer';
import { HeatmapCell } from '../../models/utilization.model';

@Component({
  selector: 'app-capacity-heatmap',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    AsyncPipe,
    DecimalPipe,
    NgIf,
    NgFor,
    NgClass,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './capacity-heatmap.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CapacityHeatmapComponent {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly heatmap$ = this.store.select(selectHeatmap);
  readonly loading$ = this.store.select(selectHeatmapLoading);

  readonly form = this.fb.nonNullable.group({
    dateFrom: ['', Validators.required],
    dateTo: ['', Validators.required],
  });

  selectedCell: { resourceId: string; cell: HeatmapCell } | null = null;

  readonly trafficLightIcon: Record<string, string> = {
    Green: '●',
    Yellow: '▲',
    Orange: '◆',
    Red: '✕',
  };

  load(): void {
    if (this.form.invalid) return;
    const { dateFrom, dateTo } = this.form.getRawValue();
    this.store.dispatch(CapacityActions.loadHeatmap({ dateFrom, dateTo }));
    this.selectedCell = null;
  }

  selectCell(resourceId: string, cell: HeatmapCell): void {
    this.selectedCell = { resourceId, cell };
  }

  cellTooltip(cell: HeatmapCell): string {
    return `${cell.actualHours.toFixed(1)}h / ${cell.availableHours.toFixed(0)}h available (${cell.utilizationPct.toFixed(1)}%)`;
  }

  cellClass(cell: HeatmapCell): string {
    return `cell-${cell.trafficLight.toLowerCase()}`;
  }
}
```

**File:** `frontend/project-management-web/src/app/features/capacity/components/capacity-heatmap/capacity-heatmap.html`

```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Heatmap Capacity (Person × Week)</mat-card-title>
  </mat-card-header>
  <mat-card-content>

    <!-- Legend -->
    <div class="legend">
      <span class="legend-item cell-green">● Green &lt;80%</span>
      <span class="legend-item cell-yellow">▲ Yellow 80–95%</span>
      <span class="legend-item cell-orange">◆ Orange 95–105%</span>
      <span class="legend-item cell-red">✕ Red &gt;105%</span>
    </div>

    <!-- Filter form -->
    <form [formGroup]="form" (ngSubmit)="load()" style="display:flex;gap:12px;align-items:flex-start;flex-wrap:wrap;margin-bottom:16px">
      <mat-form-field style="width:160px">
        <mat-label>Từ ngày (yyyy-MM-dd)</mat-label>
        <input matInput formControlName="dateFrom" placeholder="2026-04-14" />
        <mat-error *ngIf="form.controls.dateFrom.hasError('required')">Bắt buộc</mat-error>
      </mat-form-field>

      <mat-form-field style="width:160px">
        <mat-label>Đến ngày (yyyy-MM-dd)</mat-label>
        <input matInput formControlName="dateTo" placeholder="2026-05-09" />
        <mat-error *ngIf="form.controls.dateTo.hasError('required')">Bắt buộc</mat-error>
      </mat-form-field>

      <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || (loading$ | async)">
        Xem heatmap
      </button>
      <mat-spinner *ngIf="loading$ | async" diameter="24" style="align-self:center"></mat-spinner>
    </form>

    <ng-container *ngIf="heatmap$ | async as heatmap">
      <p style="color:#666;margin-bottom:12px">
        <strong>{{ heatmap.projectCount }}</strong> project • <strong>{{ heatmap.rows.length }}</strong> nhân sự
      </p>

      <p *ngIf="heatmap.rows.length === 0" style="color:#666">Không có dữ liệu trong khoảng ngày này.</p>

      <div *ngIf="heatmap.rows.length > 0" style="overflow-x:auto">
        <table class="heatmap-table">
          <thead>
            <tr>
              <th class="resource-col">Nhân sự</th>
              <th *ngFor="let w of heatmap.weeks" class="week-col">
                {{ w }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of heatmap.rows">
              <td class="resource-col" style="font-size:11px">{{ row.resourceId }}</td>
              <td *ngFor="let cell of row.cells"
                  class="heatmap-cell"
                  [ngClass]="cellClass(cell)"
                  [matTooltip]="cellTooltip(cell)"
                  (click)="selectCell(row.resourceId, cell)"
                  style="cursor:pointer">
                <span class="cell-icon">{{ trafficLightIcon[cell.trafficLight] }}</span>
                <span class="cell-pct">{{ cell.utilizationPct | number:'1.0-0' }}%</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Drill-down panel -->
      <div *ngIf="selectedCell" class="drill-down-panel">
        <h4>Chi tiết — Tuần {{ selectedCell.cell.weekStart }}</h4>
        <p><strong>Resource:</strong> {{ selectedCell.resourceId }}</p>
        <p><strong>Giờ thực tế:</strong> {{ selectedCell.cell.actualHours | number:'1.1-1' }}h</p>
        <p><strong>Giờ available:</strong> {{ selectedCell.cell.availableHours | number:'1.0-0' }}h</p>
        <p><strong>Utilization:</strong> {{ selectedCell.cell.utilizationPct | number:'1.1-1' }}%</p>
        <p><strong>Trạng thái:</strong>
          <span [ngClass]="'badge-' + selectedCell.cell.trafficLight.toLowerCase()">
            {{ trafficLightIcon[selectedCell.cell.trafficLight] }} {{ selectedCell.cell.trafficLight }}
          </span>
        </p>
        <button mat-button (click)="selectedCell = null">Đóng</button>
      </div>

    </ng-container>
  </mat-card-content>
</mat-card>

<style>
  .legend { display:flex; gap:16px; flex-wrap:wrap; margin-bottom:12px; font-size:13px; }
  .legend-item { padding:4px 10px; border-radius:4px; font-weight:600; }

  .heatmap-table { border-collapse:collapse; }
  .heatmap-table th, .heatmap-table td { border:1px solid #e0e0e0; padding:4px 8px; text-align:center; white-space:nowrap; }
  .resource-col { text-align:left !important; min-width:120px; }
  .week-col { min-width:90px; font-size:12px; }

  .heatmap-cell { min-width:90px; }
  .cell-icon { display:block; font-size:11px; }
  .cell-pct { font-size:12px; font-weight:600; }

  .cell-green  { background:#e8f5e9; color:#2e7d32; }
  .cell-yellow { background:#fff8e1; color:#f57f17; }
  .cell-orange { background:#fff3e0; color:#e65100; }
  .cell-red    { background:#ffebee; color:#c62828; }

  .legend-item.cell-green  { background:#e8f5e9; color:#2e7d32; }
  .legend-item.cell-yellow { background:#fff8e1; color:#f57f17; }
  .legend-item.cell-orange { background:#fff3e0; color:#e65100; }
  .legend-item.cell-red    { background:#ffebee; color:#c62828; }

  .drill-down-panel { margin-top:16px; padding:16px; background:#f5f5f5; border-radius:8px; max-width:400px; }
  .drill-down-panel h4 { margin:0 0 12px; }

  .badge-green  { color:#2e7d32; font-weight:600; }
  .badge-yellow { color:#f57f17; font-weight:600; }
  .badge-orange { color:#e65100; font-weight:600; }
  .badge-red    { color:#c62828; font-weight:600; }
</style>
```

**File:** `frontend/project-management-web/src/app/features/capacity/capacity.routes.ts`

Thêm route `heatmap` sau `cross-project`:
```typescript
{
  path: 'heatmap',
  loadComponent: () =>
    import('./components/capacity-heatmap/capacity-heatmap').then(m => m.CapacityHeatmapComponent),
},
```

### Patterns từ Story 4.4 cần giữ

1. **MediatR assembly auto-registration** — handler mới trong `Capacity.Application` tự được pick up, không cần cập nhật gì ở Program.cs.
2. **ICurrentUserService.UserId** — lấy JWT `sub` claim tự động, không nhận userId từ query param.
3. **GetMonday()** helper — copy lại trong handler mới (không dùng shared static để tránh coupling giữa handlers).
4. **CountWeekdays()** helper — cần triển khai trong handler (đếm Thứ 2–Thứ 6 trong range, không tính Thứ 7/CN).
5. **Ordering rows** — sắp xếp theo mức độ nghiêm trọng: nhiều ô Red/Orange nhất lên đầu.

### Frontend — Patterns quan trọng

- `ChangeDetectionStrategy.OnPush` + `AsyncPipe` — tất cả components Capacity đều dùng pattern này.
- `NgFor` phải import trong `imports: []` array của standalone component.
- `MatTooltipModule` phải import để dùng `[matTooltip]`.
- `selectedCell` là local component state (không vào store) — đây là UI-only state, không cần persist.
- NgClass nhận string `'cell-green'` etc, không phải object — xem `cellClass()` method.

### Lưu ý về heatmap range

- Backend nhận `dateFrom`, `dateTo` bất kỳ.
- `BuildWeeks()` tính tất cả Mondays trong range: bắt đầu từ Monday của `dateFrom`, tăng 7 ngày, đến khi Monday > `dateTo`.
- Nếu `dateFrom = 2026-04-16` (Thứ 5), `GetMonday(dateFrom) = 2026-04-13` → tuần đầu là `2026-04-13`.
- Mỗi cell tính `effectiveStart`/`effectiveEnd` để clamp range cho tuần đầu/cuối bị cắt bởi `dateFrom`/`dateTo`.
- Recommend UI: suggest range 4 tuần (ví dụ placeholder `2026-04-14` → `2026-05-09`).

---

## Completion Notes

- Backend: `GetCapacityHeatmapQuery` handler trong `Capacity.Application` — membership-scoped (reuse pattern từ 4.4), build tuần từ `GetMonday()`, clamp edges, tính weekdays×8 available, sort rows theo Red/Orange nhiều nhất lên đầu.
- Backend: `GET /api/v1/capacity/heatmap` trong `CapacityController` — dùng `_currentUser.UserId`, không nhận projectId (non-leak).
- Frontend: models `HeatmapCell`, `HeatmapRow`, `CapacityHeatmapResult` thêm vào `utilization.model.ts`.
- Frontend: NgRx đầy đủ — 3 actions, state mở rộng, reducer cases, selectors, effect.
- Frontend: `CapacityHeatmapComponent` standalone OnPush — form + legend + table heatmap + drill-down panel.
- Accessibility: icon pattern (● ▲ ◆ ✕) + text utilization% trong mỗi ô, không dựa màu đơn thuần. Tooltip hiển thị giờ chi tiết.
- `dotnet build` → 0 errors (10 pre-existing MSB3277 warnings), `ng build` → 0 errors.

## Files Created/Modified

- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetCapacityHeatmap/GetCapacityHeatmapQuery.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/Controllers/CapacityController.cs` (modified — thêm using + GetHeatmap endpoint)
- `frontend/project-management-web/src/app/features/capacity/models/utilization.model.ts` (modified — thêm 3 interfaces)
- `frontend/project-management-web/src/app/features/capacity/services/capacity-api.service.ts` (modified — thêm getCapacityHeatmap)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.actions.ts` (modified — thêm 3 actions)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.reducer.ts` (modified — thêm state fields, cases, selectors)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.effects.ts` (modified — thêm loadHeatmap$)
- `frontend/project-management-web/src/app/features/capacity/components/capacity-heatmap/capacity-heatmap.ts` (new)
- `frontend/project-management-web/src/app/features/capacity/components/capacity-heatmap/capacity-heatmap.html` (new)
- `frontend/project-management-web/src/app/features/capacity/capacity.routes.ts` (modified — thêm route heatmap)
