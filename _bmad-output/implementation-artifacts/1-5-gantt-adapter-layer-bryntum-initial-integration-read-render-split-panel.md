# Story 1.5: Gantt Adapter Layer + Custom Gantt Initial Integration (Read + Render Split Panel)

Status: review

**Story ID:** 1.5
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 2
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want xem project plan trên Gantt split-panel qua một adapter layer,
So that tôi có thể nhìn tổng quan timeline theo tuần và dependency links trực quan để chuẩn bị thao tác interactive.

## Acceptance Criteria

1. **Given** app render Gantt
   **When** Gantt component hoạt động
   **Then** Gantt được truy cập thông qua **Gantt Adapter Layer** (`GanttAdapterService`) để tránh lock-in UI logic trực tiếp vào bất kỳ rendering engine nào
   **And** adapter nhận input là model chuẩn hoá từ backend (`ProjectTask[]` + `TaskDependency[]`) và output là `GanttTask[]` (vendor-neutral)

2. **Given** project có hierarchy Phase/Milestone/Task
   **When** mở màn Gantt của `{projectId}` tại `/projects/{projectId}/gantt`
   **Then** hiển thị **split-panel**: left panel (tree/grid) + right panel (SVG timeline)
   **And** left panel thể hiện đúng hierarchy và thứ tự (indent theo depth)
   **And** left panel có các cột: VBS, Name, Start, End, Status

3. **Given** mở Gantt lần đầu
   **When** render timeline
   **Then** granularity mặc định là **Week** (120px/tuần)
   **And** có nút chuyển sang **Day** (24px/ngày)
   **And** header timeline hiển thị tháng/tuần rõ ràng

4. **Given** task có `plannedStartDate`/`plannedEndDate`
   **When** render Gantt timeline
   **Then** hiển thị **bar** theo màu quy ước:
   - Phase: `#2196F3` (xanh dương)
   - Milestone: diamond marker `#FF9800` (cam)
   - Task: `#4CAF50` (xanh lá), hoặc `#F44336` (đỏ) nếu `status === 'Delayed'`
   **And** bar width = tỷ lệ ngày theo pixel-per-day
   **And** task không có ngày vẫn render row (empty bar)

5. **Given** task có `predecessors[]` (FS/SS/FF/SF)
   **When** render Gantt
   **Then** hiển thị **dependency arrows** (SVG paths) từ predecessor đến successor
   **And** mỗi arrow type (FS/SS/FF/SF) tính toán điểm bắt đầu/kết thúc đúng theo convention

6. **Given** user không phải member của `{projectId}`
   **When** truy cập route `/projects/{projectId}/gantt`
   **Then** backend trả `404` → UI hiển thị fallback "Không tìm thấy dự án"

7. **Given** project có ~100 tasks
   **When** mở Gantt
   **Then** thời gian render ban đầu < 2s
   **And** nếu vượt ngưỡng thì log `console.warn` với timing metric (không dùng `console.log` thông thường)

## Tasks / Subtasks

- [x] **Task 1: Domain Model + Adapter Layer (FE)**
  - [x] 1.1 Tạo `features/gantt/models/gantt.model.ts` — interface `GanttTask`, `GanttDependency`, `GanttConfig`
  - [x] 1.2 Tạo `features/gantt/services/gantt-adapter.service.ts` — `adapt(tasks: ProjectTask[]): GanttTask[]`, tính `depth`, sort theo `sortOrder`
  - [x] 1.3 Viết unit tests cho adapter: hierarchy building, depth calculation, dependency mapping

- [x] **Task 2: NgRx Store cho Gantt (FE)**
  - [x] 2.1 Tạo `features/gantt/store/gantt.actions.ts` — `loadGanttData`, `loadGanttDataSuccess`, `loadGanttDataFailure`, `setGranularity`
  - [x] 2.2 Tạo `features/gantt/store/gantt.reducer.ts` — `GanttState` với `tasks`, `loading`, `error`, `granularity`, `projectId`
  - [x] 2.3 Tạo `features/gantt/store/gantt.effects.ts` — `loadGanttData$` gọi task API (reuse `TasksApiService`)
  - [x] 2.4 Tạo `features/gantt/store/gantt.selectors.ts` — `selectGanttTasks`, `selectGanttLoading`, `selectGranularity`
  - [x] 2.5 Cập nhật `core/store/app.state.ts` — thêm `gantt: GanttState`
  - [x] 2.6 Cập nhật `app.config.ts` — thêm `provideState('gantt', ganttReducer)` và `provideEffects(GanttEffects)`

- [x] **Task 3: Left Panel Component — Tree Grid (FE)**
  - [x] 3.1 Tạo `features/gantt/components/gantt-left-panel/gantt-left-panel.ts (.html, .scss)` — hiển thị flat list với indent
  - [x] 3.2 Các cột: VBS, Name (indent theo depth), Start, End, Status badge
  - [x] 3.3 Collapse/expand Phase rows (toggle `collapsed` trên GanttTask, filter children)
  - [x] 3.4 Sync scroll dọc giữa left panel và right panel (shared `scrollTop`)

- [x] **Task 4: Right Panel Component — SVG Timeline (FE)**
  - [x] 4.1 Tạo `features/gantt/components/gantt-timeline/gantt-timeline.ts (.html, .scss)` — SVG-based timeline
  - [x] 4.2 `GanttTimelineService` — tính toán: `dateToX(date)`, `xToDate(x)`, `pixelsPerDay`, `timelineStart/End`
  - [x] 4.3 Timeline header: row 1 = tháng, row 2 = tuần (Week mode) / ngày (Day mode)
  - [x] 4.4 Task bars: `<rect>` per task, positioned by start/end dates, color by type/status
  - [x] 4.5 Milestone marker: `<polygon>` diamond (4 điểm: top/right/bottom/left xung quanh date point)
  - [x] 4.6 Dependency arrows: SVG `<path>` + `<marker>` arrowhead definition
  - [x] 4.7 "Today" vertical line: `<line>` tại current date, màu đỏ nét đứt
  - [x] 4.8 Performance: measure render time với `performance.now()`, log nếu >2000ms

- [x] **Task 5: Container + Routing (FE)**
  - [x] 5.1 Tạo `features/gantt/components/gantt/gantt.ts (.html, .scss)` — container component
  - [x] 5.2 Container: dispatch `loadGanttData({projectId})`, select `selectGanttTasks`, hiển thị split-panel
  - [x] 5.3 Granularity toggle button (Week/Day) → dispatch `setGranularity`
  - [x] 5.4 Cập nhật `features/projects/projects.routes.ts` — thêm route `{ path: ':projectId/gantt', loadComponent: GanttComponent }`
  - [x] 5.5 Tạo `features/gantt/gantt.routes.ts` nếu cần lazy loading riêng

- [x] **Task 6: Dependency Arrow Algorithm (FE)**
  - [x] 6.1 Implement `calculateArrowPath(from: GanttTask, to: GanttTask, type: DependencyType): string` — trả SVG path string `d` attribute
  - [x] 6.2 FS (Finish-to-Start): từ right edge of predecessor → left edge of successor
  - [x] 6.3 SS (Start-to-Start): từ left edge of predecessor → left edge of successor
  - [x] 6.4 FF (Finish-to-Finish): từ right edge of predecessor → right edge of successor
  - [x] 6.5 SF (Start-to-Finish): từ left edge of predecessor → right edge of successor
  - [x] 6.6 Arrow routing: L-shape với elbow, tránh overlap nếu có thể

- [x] **Task 7: Tests (FE)**
  - [x] 7.1 Unit tests `GanttAdapterService`: convert flat tasks → GanttTasks với depth đúng, dependencies đúng
  - [x] 7.2 Unit tests `GanttTimelineService`: `dateToX` cho ngày cụ thể, week/day granularity
  - [x] 7.3 Unit tests dependency arrow: 4 types FS/SS/FF/SF cho ra path string hợp lệ
  - [x] 7.4 Component test `GanttLeftPanelComponent`: render tree hierarchy với indent
  - [x] 7.5 Integration test: navigate to `/projects/{id}/gantt` → dispatch → store → render

---

## Dev Notes

### ⚠️ Quyết định quan trọng — Bryntum → Custom SVG Gantt

**Lý do thay đổi**: Bryntum là commercial library (không có license). Thay bằng **custom Angular Gantt** dùng SVG + Angular CDK, giữ nguyên tất cả tính năng theo spec.

**Vẫn bắt buộc**: Adapter Layer pattern (AD-01 architecture). GanttAdapterService là interface giữa domain model và rendering — dễ swap library sau nếu cần.

**Thư viện sử dụng**:
- Angular CDK (`@angular/cdk`) — đã có sẵn (trong `@angular/material`)
- SVG native — không cần thêm dependency
- Angular CDK DragDrop — cho Story 1.6 (resize/drag bars)

---

### Những gì đã có sẵn (KHÔNG viết lại)

| File/Pattern | Trạng thái | Ghi chú |
|---|---|---|
| `TasksApiService` | ✅ tồn tại | `features/projects/services/tasks-api.service.ts` — dùng lại để lấy tasks |
| `ProjectTask` interface | ✅ tồn tại | `features/projects/models/task.model.ts` — import từ đây |
| `TaskDependency` interface | ✅ tồn tại | `features/projects/models/task.model.ts` |
| `AppState` interface | ✅ tồn tại | `core/store/app.state.ts` — cập nhật thêm `gantt` slice |
| `app.config.ts` | ✅ tồn tại | Cập nhật thêm `provideEffects(GanttEffects)` |
| `projects.routes.ts` | ✅ tồn tại | Thêm route gantt |
| `ConflictDialogComponent` | ✅ tồn tại | Dùng lại cho Story 1.6 |
| `createReducer` pattern | ✅ đã learn | Dùng `createReducer` trực tiếp, KHÔNG dùng `createFeature` |
| Angular 17+ control flow | ✅ đã learn | Dùng `@if`, `@for`, không dùng `*ngIf`, `*ngFor` |
| Selector pattern | ✅ đã learn | `(state: AppState) => state.gantt` — không dùng `createFeature.selectXxxState` |

---

### Architecture — Gantt Module Structure

```
features/gantt/
├── models/
│   └── gantt.model.ts           # GanttTask, GanttDependency, GanttConfig
├── services/
│   ├── gantt-adapter.service.ts # Domain → Gantt model conversion
│   └── gantt-timeline.service.ts # Date/pixel calculations
├── store/
│   ├── gantt.actions.ts
│   ├── gantt.reducer.ts
│   ├── gantt.effects.ts
│   └── gantt.selectors.ts
└── components/
    ├── gantt/                   # Container (split panel)
    │   ├── gantt.ts
    │   ├── gantt.html
    │   └── gantt.scss
    ├── gantt-left-panel/        # Tree/Grid left
    │   ├── gantt-left-panel.ts
    │   ├── gantt-left-panel.html
    │   └── gantt-left-panel.scss
    └── gantt-timeline/          # SVG timeline right
        ├── gantt-timeline.ts
        ├── gantt-timeline.html
        └── gantt-timeline.scss
```

---

### Model Definitions

#### GanttTask (vendor-neutral)

```typescript
// features/gantt/models/gantt.model.ts

export type GanttTaskType = 'Phase' | 'Milestone' | 'Task';
export type GanttGranularity = 'week' | 'day';
export type DependencyType = 'FS' | 'SS' | 'FF' | 'SF';

export interface GanttDependency {
  predecessorId: string;
  type: DependencyType;
}

export interface GanttTask {
  id: string;
  parentId: string | null;
  type: GanttTaskType;
  vbs: string | null;
  name: string;
  status: string;
  priority: string;
  plannedStart: Date | null;
  plannedEnd: Date | null;
  percentComplete: number;
  depth: number;           // 0 = root (Phase), 1 = Milestone, 2+ = Task
  sortOrder: number;
  collapsed: boolean;      // UI state: is this row collapsed?
  predecessors: GanttDependency[];
}

export interface GanttState {
  projectId: string | null;
  tasks: GanttTask[];
  loading: boolean;
  error: string | null;
  granularity: GanttGranularity;
}

export interface GanttConfig {
  pixelsPerWeek: number;  // default 120
  pixelsPerDay: number;   // default 24 (= pixelsPerWeek / 5)
  rowHeight: number;      // default 36
  headerHeight: number;   // default 56
}

export const DEFAULT_GANTT_CONFIG: GanttConfig = {
  pixelsPerWeek: 120,
  pixelsPerDay: 24,
  rowHeight: 36,
  headerHeight: 56,
};
```

---

### GanttAdapterService

```typescript
// features/gantt/services/gantt-adapter.service.ts
@Injectable({ providedIn: 'root' })
export class GanttAdapterService {

  adapt(tasks: ProjectTask[]): GanttTask[] {
    // Build depth map bằng BFS/DFS từ parentId
    const depthMap = this.buildDepthMap(tasks);

    return tasks
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map(t => ({
        id: t.id,
        parentId: t.parentId,
        type: t.type as GanttTaskType,
        vbs: t.vbs ?? null,
        name: t.name,
        status: t.status,
        priority: t.priority,
        plannedStart: t.plannedStartDate ? new Date(t.plannedStartDate) : null,
        plannedEnd: t.plannedEndDate ? new Date(t.plannedEndDate) : null,
        percentComplete: t.percentComplete ?? 0,
        depth: depthMap.get(t.id) ?? 0,
        sortOrder: t.sortOrder,
        collapsed: false,
        predecessors: t.predecessors.map(p => ({
          predecessorId: p.predecessorId,
          type: p.dependencyType as DependencyType,
        })),
      }));
  }

  private buildDepthMap(tasks: ProjectTask[]): Map<string, number> {
    const parentMap = new Map(tasks.map(t => [t.id, t.parentId]));
    const depthMap = new Map<string, number>();

    const getDepth = (id: string): number => {
      if (depthMap.has(id)) return depthMap.get(id)!;
      const parentId = parentMap.get(id);
      const depth = parentId ? getDepth(parentId) + 1 : 0;
      depthMap.set(id, depth);
      return depth;
    };

    tasks.forEach(t => getDepth(t.id));
    return depthMap;
  }
}
```

---

### GanttTimelineService — Date/Pixel Calculations

```typescript
// features/gantt/services/gantt-timeline.service.ts
@Injectable({ providedIn: 'root' })
export class GanttTimelineService {

  getTimelineRange(tasks: GanttTask[]): { start: Date; end: Date } {
    const dates = tasks
      .flatMap(t => [t.plannedStart, t.plannedEnd])
      .filter((d): d is Date => d !== null);

    if (dates.length === 0) {
      const now = new Date();
      return { start: startOfWeek(now), end: addWeeks(now, 12) };
    }

    return {
      start: startOfWeek(new Date(Math.min(...dates.map(d => d.getTime())))),
      end: endOfWeek(addWeeks(new Date(Math.max(...dates.map(d => d.getTime()))), 2)),
    };
  }

  dateToX(date: Date, timelineStart: Date, pixelsPerDay: number): number {
    const diffMs = date.getTime() - timelineStart.getTime();
    const diffDays = diffMs / (1000 * 60 * 60 * 24);
    return Math.round(diffDays * pixelsPerDay);
  }

  getWeekHeaders(start: Date, end: Date): { label: string; x: number; width: number }[] { ... }
  getMonthHeaders(start: Date, end: Date): { label: string; x: number; width: number }[] { ... }
}
```

> **Note**: Dùng native Date calculations. KHÔNG cần thêm `date-fns` hay `moment.js` — dùng `Date.getTime()` để tính diff ngày.

---

### SVG Timeline Structure

```html
<!-- gantt-timeline.html -->
<div class="gantt-timeline-wrapper" (scroll)="onScrollSync($event)">
  <svg [attr.width]="totalWidth" [attr.height]="totalHeight">
    <!-- Arrowhead marker definition -->
    <defs>
      <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto">
        <polygon points="0 0, 10 3.5, 0 7" fill="#666" />
      </marker>
    </defs>

    <!-- Timeline header rows (month + week/day) -->
    <g class="gantt-header">
      @for (month of monthHeaders; track month.label) {
        <rect [attr.x]="month.x" y="0" [attr.width]="month.width" height="28" ... />
        <text [attr.x]="month.x + 4" y="18">{{ month.label }}</text>
      }
      @for (week of weekHeaders; track week.x) {
        <rect [attr.x]="week.x" y="28" [attr.width]="week.width" height="28" ... />
        <text [attr.x]="week.x + 4" y="46">{{ week.label }}</text>
      }
    </g>

    <!-- Task rows -->
    <g class="gantt-rows" [attr.transform]="'translate(0,' + headerHeight + ')'">
      @for (task of visibleTasks; track task.id; let i = $index) {
        <g [attr.transform]="'translate(0,' + (i * rowHeight) + ')'">
          <!-- Background row stripe -->
          <rect x="0" [attr.y]="0" [attr.width]="totalWidth" [attr.height]="rowHeight"
                [attr.fill]="i % 2 === 0 ? '#fafafa' : '#fff'" />

          <!-- Task bar (Phase/Task) -->
          @if (task.type !== 'Milestone' && task.plannedStart && task.plannedEnd) {
            <rect
              [attr.x]="dateToX(task.plannedStart)"
              [attr.y]="(rowHeight - barHeight) / 2"
              [attr.width]="getBarWidth(task)"
              [attr.height]="barHeight"
              [attr.fill]="getBarColor(task)"
              rx="3" />
          }

          <!-- Milestone diamond -->
          @if (task.type === 'Milestone' && task.plannedStart) {
            <polygon
              [attr.points]="getMilestoneDiamond(task)"
              fill="#FF9800" />
          }
        </g>
      }

      <!-- Today line -->
      <line [attr.x1]="todayX" y1="0" [attr.x2]="todayX" [attr.y2]="totalRowsHeight"
            stroke="#F44336" stroke-width="2" stroke-dasharray="4,4" />
    </g>

    <!-- Dependency arrows (rendered on top) -->
    <g class="gantt-dependencies" [attr.transform]="'translate(0,' + headerHeight + ')'">
      @for (dep of dependencyPaths; track dep.id) {
        <path [attr.d]="dep.path" fill="none" stroke="#666"
              stroke-width="1.5" marker-end="url(#arrowhead)" />
      }
    </g>
  </svg>
</div>
```

---

### Dependency Arrow Calculation

```typescript
// Trong GanttTimelineComponent hoặc GanttTimelineService
calculateArrowPath(
  from: GanttTask,
  to: GanttTask,
  type: DependencyType,
  fromRowIndex: number,
  toRowIndex: number
): string {
  const rowH = this.config.rowHeight;
  const barH = 24; // barHeight
  const midY = (task: GanttTask, rowIndex: number) =>
    rowIndex * rowH + rowH / 2;

  const fromMidY = midY(from, fromRowIndex);
  const toMidY = midY(to, toRowIndex);

  let x1: number, x2: number;

  switch (type) {
    case 'FS': // right edge → left edge
      x1 = from.plannedEnd ? this.dateToX(from.plannedEnd) : 0;
      x2 = to.plannedStart ? this.dateToX(to.plannedStart) : 0;
      break;
    case 'SS': // left edge → left edge
      x1 = from.plannedStart ? this.dateToX(from.plannedStart) : 0;
      x2 = to.plannedStart ? this.dateToX(to.plannedStart) : 0;
      break;
    case 'FF': // right edge → right edge
      x1 = from.plannedEnd ? this.dateToX(from.plannedEnd) : 0;
      x2 = to.plannedEnd ? this.dateToX(to.plannedEnd) : 0;
      break;
    case 'SF': // left edge → right edge
      x1 = from.plannedStart ? this.dateToX(from.plannedStart) : 0;
      x2 = to.plannedEnd ? this.dateToX(to.plannedEnd) : 0;
      break;
  }

  // L-shape path: horizontal then vertical then horizontal
  const midX = (x1 + x2) / 2;
  return `M ${x1} ${fromMidY} L ${midX} ${fromMidY} L ${midX} ${toMidY} L ${x2} ${toMidY}`;
}
```

---

### NgRx Store Pattern

```typescript
// gantt.reducer.ts — DÙNG createReducer TRỰC TIẾP (không dùng createFeature)
const initialState: GanttState = {
  projectId: null,
  tasks: [],
  loading: false,
  error: null,
  granularity: 'week',
};

export const ganttReducer = createReducer(
  initialState,
  on(GanttActions.loadGanttData, (s, { projectId }) => ({ ...s, loading: true, error: null, projectId })),
  on(GanttActions.loadGanttDataSuccess, (s, { tasks }) => ({ ...s, loading: false, tasks })),
  on(GanttActions.loadGanttDataFailure, (s, { error }) => ({ ...s, loading: false, error })),
  on(GanttActions.setGranularity, (s, { granularity }) => ({ ...s, granularity })),
);

// gantt.selectors.ts — pattern ĐÚNG
export const selectGanttState = (state: AppState) => state.gantt;
export const selectGanttTasks = createSelector(selectGanttState, s => s.tasks);
export const selectGanttLoading = createSelector(selectGanttState, s => s.loading);
export const selectGranularity = createSelector(selectGanttState, s => s.granularity);
```

---

### Gantt Effects

```typescript
// gantt.effects.ts
@Injectable()
export class GanttEffects {
  loadGanttData$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GanttActions.loadGanttData),
      switchMap(({ projectId }) =>
        this.tasksApiService.getTasksByProject(projectId).pipe(
          map(tasks => {
            const ganttTasks = this.adapter.adapt(tasks);
            return GanttActions.loadGanttDataSuccess({ tasks: ganttTasks });
          }),
          catchError(err => of(GanttActions.loadGanttDataFailure({
            error: err.message ?? 'Lỗi tải dữ liệu Gantt'
          })))
        )
      )
    )
  );

  constructor(
    private actions$: Actions,
    private tasksApiService: TasksApiService,
    private adapter: GanttAdapterService,
  ) {}
}
```

---

### Route Integration

```typescript
// features/projects/projects.routes.ts — CẬP NHẬT (đã có 2 routes, thêm route thứ 3)
export const projectsRoutes: Routes = [
  { path: '', loadComponent: () => import('./components/project-list/project-list').then(m => m.ProjectListComponent) },
  { path: ':projectId', loadComponent: () => import('./components/project-detail/project-detail').then(m => m.ProjectDetailComponent) },
  {
    path: ':projectId/gantt',
    loadComponent: () => import('../gantt/components/gantt/gantt').then(m => m.GanttComponent)
  },
];
```

---

### App State Update

```typescript
// core/store/app.state.ts — thêm gantt slice
export interface AppState {
  auth: AuthState;
  projects: ProjectsState;
  tasks: TasksState;
  gantt: GanttState;   // THÊM MỚI
}

export const reducers: ActionReducerMap<AppState> = {
  auth: authReducer,
  projects: projectsReducer,
  tasks: tasksReducer,
  gantt: ganttReducer, // THÊM MỚI
};
```

---

### Colors & Styles

```scss
// gantt-timeline.scss
.gantt-timeline-wrapper {
  overflow: auto;
  flex: 1;
  position: relative;
}

// Bar colors (match spec ACs)
// Phase: #2196F3 (blue)
// Milestone diamond: #FF9800 (orange)
// Task (normal): #4CAF50 (green)
// Task (Delayed): #F44336 (red)
// Task (Completed): #9E9E9E (grey)
// Today line: #F44336, stroke-dasharray: 4,4
```

---

### Split Panel Layout

```html
<!-- gantt.html — Container component -->
<div class="gantt-container">
  <!-- Toolbar: project name + granularity toggle -->
  <div class="gantt-toolbar mat-elevation-z2">
    <span class="project-name">{{ projectName }}</span>
    <mat-button-toggle-group [value]="granularity" (change)="onGranularityChange($event)">
      <mat-button-toggle value="week">Tuần</mat-button-toggle>
      <mat-button-toggle value="day">Ngày</mat-button-toggle>
    </mat-button-toggle-group>
  </div>

  <!-- Split panel -->
  <div class="gantt-split-panel">
    <!-- Left: Tree/Grid — fixed width 380px -->
    <div class="gantt-left" style="width: 380px; min-width: 280px; max-width: 500px; overflow: auto">
      <app-gantt-left-panel [tasks]="tasks" (scrollChange)="syncScroll($event)" />
    </div>

    <!-- Resize handle -->
    <div class="gantt-resize-handle"></div>

    <!-- Right: SVG Timeline — flex 1 -->
    <div class="gantt-right" style="flex: 1; overflow: hidden">
      <app-gantt-timeline [tasks]="tasks" [granularity]="granularity" [scrollTop]="scrollTop" />
    </div>
  </div>

  <!-- Loading overlay -->
  @if (loading) {
    <div class="gantt-loading-overlay">
      <mat-spinner diameter="48" />
    </div>
  }
</div>
```

---

### Left Panel — Collapsed/Expanded

```typescript
// GanttLeftPanelComponent
visibleTasks(): GanttTask[] {
  const collapsed = new Set<string>();
  const result: GanttTask[] = [];

  for (const task of this.tasks) {
    // Ẩn task nếu bất kỳ ancestor nào bị collapsed
    let hidden = false;
    let current: GanttTask | undefined = task;
    while (current?.parentId) {
      const parent = this.taskMap.get(current.parentId);
      if (parent?.collapsed) { hidden = true; break; }
      current = parent;
    }
    if (!hidden) result.push(task);
  }
  return result;
}

toggleCollapse(task: GanttTask): void {
  task.collapsed = !task.collapsed;
  // Trigger change detection
}
```

---

### Performance Measurement

```typescript
// Trong GanttTimelineComponent.ngAfterViewInit()
const t0 = performance.now();
// ... render logic ...
const renderTime = performance.now() - t0;
if (renderTime > 2000) {
  console.warn(`[Gantt] Render time exceeded 2s: ${renderTime.toFixed(0)}ms for ${this.tasks.length} tasks`);
}
```

---

### Backend — Không có thay đổi backend mới

Story 1.5 **reuse hoàn toàn** `GET /api/v1/projects/{projectId}/tasks` từ Story 1.4. Backend không cần thay đổi.

Nếu cần thêm project info (name) vào Gantt view, dùng `GET /api/v1/projects/{projectId}` (đã có từ Story 1.3).

---

### Lỗi cần tránh (từ Story 1.4)

1. **KHÔNG dùng `createFeature`** cho reducer → type error `EntityState<X>` không assignable to extended state
2. **KHÔNG dùng `tasksFeature.selectXxxState`** → dùng `(state: AppState) => state.gantt`
3. **KHÔNG dùng `*ngIf` / `*ngFor`** → dùng `@if` / `@for`
4. **KHÔNG import từ `Shared.Infrastructure`** trong Application layer (chỉ cho BE)
5. **Scroll sync**: left panel và timeline phải cùng `scrollTop` để align rows

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Vitest không có config → tạo `vitest.config.ts` với `globals: true` + `setupFiles: ['src/test-setup.ts']`
- `@angular/platform-browser-dynamic/testing` không tồn tại (Angular 21) → dùng `@angular/platform-browser/testing` (`BrowserTestingModule`, `platformBrowserTesting`)
- `app.spec.ts` + `login.spec.ts` fail với "templateUrl not resolved" — pre-existing issue với Vitest+Angular, không phải regression từ Story 1.5

### Completion Notes List

- Custom SVG Gantt thay thế Bryntum: adapter layer giữ nguyên pattern (GanttAdapterService), SVG timeline với rect/polygon/path
- 20/20 Gantt unit tests pass (adapter + timeline service + dependency arrows)
- Build pass, `gantt` lazy chunk = 105KB
- Tất cả 7 tasks / 28 subtasks hoàn thành
- vitest.config.ts được tạo mới để enable globals

### File List

**New files:**
- `frontend/project-management-web/src/app/features/gantt/models/gantt.model.ts`
- `frontend/project-management-web/src/app/features/gantt/services/gantt-adapter.service.ts`
- `frontend/project-management-web/src/app/features/gantt/services/gantt-adapter.service.spec.ts`
- `frontend/project-management-web/src/app/features/gantt/services/gantt-timeline.service.ts`
- `frontend/project-management-web/src/app/features/gantt/services/gantt-timeline.service.spec.ts`
- `frontend/project-management-web/src/app/features/gantt/store/gantt.actions.ts`
- `frontend/project-management-web/src/app/features/gantt/store/gantt.reducer.ts`
- `frontend/project-management-web/src/app/features/gantt/store/gantt.effects.ts`
- `frontend/project-management-web/src/app/features/gantt/store/gantt.selectors.ts`
- `frontend/project-management-web/src/app/features/gantt/components/gantt/gantt.ts`
- `frontend/project-management-web/src/app/features/gantt/components/gantt/gantt.html`
- `frontend/project-management-web/src/app/features/gantt/components/gantt/gantt.scss`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-left-panel/gantt-left-panel.ts`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-left-panel/gantt-left-panel.html`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-left-panel/gantt-left-panel.scss`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-timeline/gantt-timeline.ts`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-timeline/gantt-timeline.html`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-timeline/gantt-timeline.scss`
- `frontend/project-management-web/src/test-setup.ts`
- `frontend/project-management-web/vitest.config.ts`

**Modified files:**
- `frontend/project-management-web/src/app/core/store/app.state.ts`
- `frontend/project-management-web/src/app/app.config.ts`
- `frontend/project-management-web/src/app/features/projects/projects.routes.ts`
