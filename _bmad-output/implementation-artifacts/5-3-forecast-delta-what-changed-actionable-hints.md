# Story 5.3: Forecast delta ("what changed") + actionable hints

Status: review

**Story ID:** 5.3
**Epic:** Epic 5 — Capacity Planning Suite (Heatmap + 4-week Forecast)
**Sprint:** Sprint 6
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want thấy thay đổi chính của forecast so với lần trước,
So that tôi phát hiện bottleneck proactively thay vì reactive.

## Acceptance Criteria

1. **Given** có ít nhất 2 bản forecast Succeeded
   **When** gọi `GET /api/v1/capacity/forecast/delta`
   **Then** trả về top N thay đổi lớn nhất (theo |delta utilization%|) với `previousVersion`, `currentVersion`
   **And** mỗi item có: `resourceId`, `weekStart`, `previousUtilizationPct`, `currentUtilizationPct`, `deltaPct`, `currentTrafficLight`, `hint`

2. **Given** chỉ có 0 hoặc 1 bản forecast Succeeded
   **When** gọi `/forecast/delta`
   **Then** trả về `{ hasData: false, topChanges: [] }` — không lỗi

3. **Given** actionable hints
   **When** render delta
   **Then** hint cụ thể theo tình trạng:
   - Current Red/Orange: "Nguy cơ overload — cân nhắc điều chỉnh phân công"
   - Giảm từ Red/Orange → Green/Yellow: "Cải thiện capacity"
   - |delta| > 20%: "Thay đổi đáng kể"
   - Còn lại: "Theo dõi"

4. **Given** UI hiển thị delta
   **When** có delta data
   **Then** bảng delta hiển thị trong `ForecastViewComponent` ngay dưới bảng forecast chính
   **And** badge màu cho `currentTrafficLight`, dấu +/- rõ ràng trên `deltaPct`

---

## Tasks / Subtasks

- [x] **Task 1: Capacity.Application — GetForecastDeltaQuery**
  - [x] 1.1 Tạo `Queries/GetForecastDelta/GetForecastDeltaQuery.cs` + handler
  - [x] 1.2 Handler: lấy 2 artifacts Succeeded mới nhất (ORDER BY version DESC LIMIT 2); nếu < 2 trả `hasData: false`
  - [x] 1.3 Deserialize cả 2 payloads, join theo (resourceId, weekStart), tính `deltaPct = current - previous`
  - [x] 1.4 Sort by `|deltaPct|` DESC, lấy top 10; gán hint theo rule AC-3

- [x] **Task 2: Capacity.Api — endpoint**
  - [x] 2.1 Thêm `GET /api/v1/capacity/forecast/delta` vào `CapacityController`

- [x] **Task 3: Frontend — models**
  - [x] 3.1 Thêm `ForecastDeltaItem`, `ForecastDeltaResult` vào `utilization.model.ts`

- [x] **Task 4: Frontend — API service**
  - [x] 4.1 Thêm `getForecastDelta()` vào `capacity-api.service.ts`

- [x] **Task 5: Frontend — NgRx store**
  - [x] 5.1 Thêm actions: `loadForecastDelta`, `loadForecastDeltaSuccess`, `loadForecastDeltaFailure`
  - [x] 5.2 Mở rộng `CapacityState` + reducer: `forecastDelta: ForecastDeltaResult | null`, `forecastDeltaLoading: boolean`
  - [x] 5.3 Export selectors + thêm effect `loadForecastDelta$`; patch `triggerForecast$` dispatch thêm `loadForecastDelta()`

- [x] **Task 6: Frontend — mở rộng ForecastViewComponent**
  - [x] 6.1 Inject `selectForecastDelta`, `selectForecastDeltaLoading` vào `forecast-view.ts`
  - [x] 6.2 Dispatch `loadForecastDelta()` trong `ngOnInit()`; dispatch lại sau `triggerForecastSuccess`
  - [x] 6.3 Render delta table trong `forecast-view.html` ngay dưới bảng forecast chính

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors
  - [x] 7.2 `ng build` → 0 errors

---

## Dev Notes

### Task 1 — GetForecastDeltaQuery

**File:** `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetForecastDelta/GetForecastDeltaQuery.cs`

Namespace `TriggerForecastCompute` đã export `ForecastPayload`, `ForecastResourceRow`, `ForecastWeekCell` — dùng lại via `using`.

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;
using ProjectManagement.Capacity.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetForecastDelta;

public sealed record ForecastDeltaItem(
    Guid ResourceId,
    string WeekStart,
    decimal PreviousUtilizationPct,
    decimal CurrentUtilizationPct,
    decimal DeltaPct,
    string CurrentTrafficLight,
    string Hint);

public sealed record ForecastDeltaResult(
    int CurrentVersion,
    int PreviousVersion,
    IReadOnlyList<ForecastDeltaItem> TopChanges,
    bool HasData);

public sealed record GetForecastDeltaQuery : IRequest<ForecastDeltaResult>;

public sealed class GetForecastDeltaHandler
    : IRequestHandler<GetForecastDeltaQuery, ForecastDeltaResult>
{
    private readonly ICapacityDbContext _capacityDb;

    public GetForecastDeltaHandler(ICapacityDbContext capacityDb)
    {
        _capacityDb = capacityDb;
    }

    public async Task<ForecastDeltaResult> Handle(
        GetForecastDeltaQuery query, CancellationToken ct)
    {
        var artifacts = await _capacityDb.ForecastArtifacts
            .Where(a => a.Status == "Succeeded")
            .OrderByDescending(a => a.Version)
            .Take(2)
            .ToListAsync(ct);

        if (artifacts.Count < 2)
            return new ForecastDeltaResult(
                artifacts.Count == 1 ? artifacts[0].Version : 0, 0, [], false);

        var current  = JsonSerializer.Deserialize<ForecastPayload>(artifacts[0].Payload!)!;
        var previous = JsonSerializer.Deserialize<ForecastPayload>(artifacts[1].Payload!)!;

        // Build lookup: (resourceId, weekStart) -> utilization%
        var prevLookup = previous.Resources
            .SelectMany(r => r.Cells.Select(c => (r.ResourceId, c.WeekStart, c.ForecastedUtilizationPct)))
            .ToDictionary(x => (x.ResourceId, x.WeekStart), x => x.ForecastedUtilizationPct);

        var deltas = current.Resources
            .SelectMany(r => r.Cells.Select(c =>
            {
                var prevPct = prevLookup.TryGetValue((r.ResourceId, c.WeekStart), out var p) ? p : 0m;
                var deltaPct = Math.Round(c.ForecastedUtilizationPct - prevPct, 1);
                var hint = BuildHint(c.ForecastedUtilizationPct, c.TrafficLight, prevPct, deltaPct);
                return new ForecastDeltaItem(
                    r.ResourceId, c.WeekStart, prevPct,
                    c.ForecastedUtilizationPct, deltaPct, c.TrafficLight, hint);
            }))
            .OrderByDescending(d => Math.Abs(d.DeltaPct))
            .Take(10)
            .ToList();

        return new ForecastDeltaResult(artifacts[0].Version, artifacts[1].Version, deltas, true);
    }

    private static string BuildHint(
        decimal currentPct, string currentLight, decimal prevPct, decimal deltaPct)
    {
        if (currentLight is "Red" or "Orange")
            return "Nguy cơ overload — cân nhắc điều chỉnh phân công";

        // Was overloaded, now improved
        if (prevPct >= 95m && currentPct < 95m)
            return "Cải thiện capacity";

        if (Math.Abs(deltaPct) > 20m)
            return deltaPct > 0 ? "Tăng đáng kể" : "Giảm đáng kể";

        return "Theo dõi";
    }
}
```

### Task 2 — CapacityController

Thêm vào `CapacityController.cs`:

```csharp
using ProjectManagement.Capacity.Application.Queries.GetForecastDelta;

// Add to top using block ↑

/// <summary>
/// Forecast delta: what changed between the two most recent succeeded forecasts.
/// </summary>
[HttpGet("forecast/delta")]
public async Task<IActionResult> GetForecastDelta(CancellationToken ct)
{
    var result = await _mediator.Send(new GetForecastDeltaQuery(), ct);
    return Ok(result);
}
```

### Task 3 — Frontend Models

Thêm vào cuối `utilization.model.ts`:

```typescript
export interface ForecastDeltaItem {
  resourceId: string;
  weekStart: string;
  previousUtilizationPct: number;
  currentUtilizationPct: number;
  deltaPct: number;
  currentTrafficLight: TrafficLightStatus;
  hint: string;
}

export interface ForecastDeltaResult {
  currentVersion: number;
  previousVersion: number;
  topChanges: ForecastDeltaItem[];
  hasData: boolean;
}
```

### Task 4 — API Service

```typescript
import { ForecastDeltaResult } from '../models/utilization.model'; // thêm vào import

getForecastDelta(): Observable<ForecastDeltaResult> {
  return this.http.get<ForecastDeltaResult>('/api/v1/capacity/forecast/delta');
}
```

### Task 5 — NgRx Store

**capacity.actions.ts** — thêm vào events:
```typescript
'Load Forecast Delta': emptyProps(),
'Load Forecast Delta Success': props<{ result: ForecastDeltaResult }>(),
'Load Forecast Delta Failure': props<{ error: string }>(),
```

**capacity.reducer.ts** — thêm:
```typescript
// State
forecastDelta: ForecastDeltaResult | null;
forecastDeltaLoading: boolean;

// initialState
forecastDelta: null,
forecastDeltaLoading: false,

// Cases
on(CapacityActions.loadForecastDelta, state => ({ ...state, forecastDeltaLoading: true })),
on(CapacityActions.loadForecastDeltaSuccess, (state, { result }) => ({
  ...state, forecastDeltaLoading: false, forecastDelta: result,
})),
on(CapacityActions.loadForecastDeltaFailure, state => ({ ...state, forecastDeltaLoading: false })),

// Selectors
selectForecastDelta,
selectForecastDeltaLoading,
```

**capacity.effects.ts** — thêm effect:
```typescript
loadForecastDelta$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.loadForecastDelta),
    switchMap(() =>
      this.api.getForecastDelta().pipe(
        map(result => CapacityActions.loadForecastDeltaSuccess({ result })),
        catchError(err => of(CapacityActions.loadForecastDeltaFailure({ error: err?.message ?? 'Lỗi tải delta.' })))
      )
    )
  )
);
```

**Quan trọng:** Sau khi `triggerForecastSuccess` xong, delta cũng cần reload. Sửa `triggerForecast$` effect trong `capacity.effects.ts` để dispatch thêm `loadForecastDelta()`:
```typescript
// Sửa switchMap trong triggerForecast$ từ:
switchMap(result => [
  CapacityActions.triggerForecastSuccess({ result }),
  CapacityActions.loadForecast(),
]),
// Thành:
switchMap(result => [
  CapacityActions.triggerForecastSuccess({ result }),
  CapacityActions.loadForecast(),
  CapacityActions.loadForecastDelta(),
]),
```

### Task 6 — Mở rộng ForecastViewComponent

**forecast-view.ts** — thêm selectors và dispatch:
```typescript
import { selectForecastDelta, selectForecastDeltaLoading } from '../../store/capacity.reducer';

// Thêm observables:
readonly forecastDelta$ = this.store.select(selectForecastDelta);
readonly forecastDeltaLoading$ = this.store.select(selectForecastDeltaLoading);

// ngOnInit — thêm:
this.store.dispatch(CapacityActions.loadForecastDelta());

// Thêm helper method:
deltaSign(delta: number): string {
  return delta > 0 ? '+' : '';
}

deltaClass(item: ForecastDeltaItem): string {
  if (item.deltaPct > 10) return 'delta-up';
  if (item.deltaPct < -10) return 'delta-down';
  return 'delta-neutral';
}
```

**forecast-view.html** — thêm delta section ngay dưới bảng forecast chính (TRONG `<ng-container *ngIf="forecast$ | async as forecast">`):
```html
<!-- Delta section -->
<ng-container *ngIf="forecastDelta$ | async as delta">
  <div *ngIf="delta.hasData" style="margin-top:24px">
    <h3 style="margin-bottom:8px;font-size:15px">
      Thay đổi so với lần trước
      <span style="color:#666;font-size:12px;font-weight:400">
        (v{{ delta.previousVersion }} → v{{ delta.currentVersion }})
      </span>
    </h3>

    <p *ngIf="delta.topChanges.length === 0" style="color:#666">Không có thay đổi đáng kể.</p>

    <table *ngIf="delta.topChanges.length > 0" class="delta-table">
      <thead>
        <tr>
          <th>Nhân sự</th>
          <th>Tuần</th>
          <th>Trước</th>
          <th>Sau</th>
          <th>Thay đổi</th>
          <th>Trạng thái</th>
          <th>Gợi ý</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let item of delta.topChanges">
          <td style="font-size:11px">{{ item.resourceId }}</td>
          <td>{{ item.weekStart }}</td>
          <td>{{ item.previousUtilizationPct | number:'1.1-1' }}%</td>
          <td [ngClass]="'cell-' + item.currentTrafficLight.toLowerCase()">
            {{ item.currentUtilizationPct | number:'1.1-1' }}%
          </td>
          <td [ngClass]="deltaClass(item)" style="font-weight:600">
            {{ deltaSign(item.deltaPct) }}{{ item.deltaPct | number:'1.1-1' }}%
          </td>
          <td>
            <span [ngClass]="'badge-' + item.currentTrafficLight.toLowerCase()">
              {{ trafficLightIcon[item.currentTrafficLight] }} {{ item.currentTrafficLight }}
            </span>
          </td>
          <td style="font-size:12px;color:#555">{{ item.hint }}</td>
        </tr>
      </tbody>
    </table>
  </div>

  <p *ngIf="!delta.hasData" style="color:#999;font-size:13px;margin-top:16px">
    Cần ít nhất 2 lần tính forecast để so sánh delta.
  </p>
</ng-container>
```

Thêm CSS vào `<style>` block của `forecast-view.html`:
```css
.delta-table { border-collapse:collapse; width:100%; margin-top:8px; }
.delta-table th, .delta-table td { border:1px solid #e0e0e0; padding:4px 8px; font-size:13px; text-align:center; }
.delta-table th { background:#f5f5f5; }
.delta-up   { color:#c62828; }
.delta-down { color:#2e7d32; }
.delta-neutral { color:#666; }
.badge-green  { color:#2e7d32; font-weight:600; }
.badge-yellow { color:#f57f17; font-weight:600; }
.badge-orange { color:#e65100; font-weight:600; }
.badge-red    { color:#c62828; font-weight:600; }
```

### Import ForecastDeltaItem trong component

`forecast-view.ts` cần import `ForecastDeltaItem` từ models để type-hint helper methods:
```typescript
import { ForecastDeltaItem, ForecastWeekCell } from '../../models/utilization.model';
```

### Patterns từ Stories trước

1. **Reuse types từ `TriggerForecastCompute` namespace** — `ForecastPayload`, `ForecastResourceRow`, `ForecastWeekCell` đã được export từ namespace đó; `GetForecastDeltaHandler` dùng `using` để tái sử dụng không cần duplicate.
2. **emptyProps()** cho actions không có payload — `loadForecastDelta`, `loadForecastDeltaSuccess` phải dùng `emptyProps()` đúng chỗ.
3. **Không tạo component mới** — delta render inline trong `ForecastViewComponent` hiện có.
4. **triggerForecast$ effect** cần sửa để dispatch thêm `loadForecastDelta()` sau compute — cập nhật array của switchMap.
5. **`ForecastDeltaItem` import** phải có mặt trong `forecast-view.ts` để dùng trong `deltaClass(item: ForecastDeltaItem)`.

---

## Completion Notes

- Backend: `GetForecastDeltaQuery` — lấy 2 ForecastArtifacts Succeeded mới nhất, deserialize JSON, join theo (ResourceId, WeekStart), tính deltaPct, top 10 by |delta|, gán hint theo 4 rules (overload risk / capacity improved / significant change / watch). `GetForecastDeltaResult.hasData = false` khi < 2 artifacts.
- API: `GET /api/v1/capacity/forecast/delta` thêm vào `CapacityController`.
- Frontend: 2 models mới (`ForecastDeltaItem`, `ForecastDeltaResult`). `getForecastDelta()` service method. 3 NgRx actions, 2 state fields, 3 reducer cases, 2 selectors, 1 effect `loadForecastDelta$`. `triggerForecast$` effect đã được patch để dispatch `loadForecastDelta()` sau khi compute xong.
- `ForecastViewComponent` mở rộng: `ngOnInit` dispatch cả `loadForecast()` lẫn `loadForecastDelta()`; delta table render inline dưới bảng forecast với badge màu + dấu +/- trên deltaPct + hint column.
- `dotnet build` → 0 errors, `ng build` → 0 errors.

## Files Created/Modified

- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetForecastDelta/GetForecastDeltaQuery.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/Controllers/CapacityController.cs` (modified — thêm using + GetForecastDelta endpoint)
- `frontend/project-management-web/src/app/features/capacity/models/utilization.model.ts` (modified — thêm 2 interfaces)
- `frontend/project-management-web/src/app/features/capacity/services/capacity-api.service.ts` (modified — thêm getForecastDelta)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.actions.ts` (modified — thêm 3 actions)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.reducer.ts` (modified — thêm state, cases, selectors)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.effects.ts` (modified — thêm loadForecastDelta$; patch triggerForecast$)
- `frontend/project-management-web/src/app/features/capacity/components/forecast-view/forecast-view.ts` (modified — thêm delta observables, ngOnInit dispatch, helper methods)
- `frontend/project-management-web/src/app/features/capacity/components/forecast-view/forecast-view.html` (modified — thêm delta table section)
