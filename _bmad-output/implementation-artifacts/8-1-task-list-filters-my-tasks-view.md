# Story 8.1: Modern Filter Bar (Inline Chip Bar + Gantt Sync + Saved Presets) + My Tasks View

Status: ready-for-dev

**Story ID:** 8.1
**Epic:** Epic 8 — Jira-Parity Smooth UX: Filters, My Tasks & Board View
**Sprint:** Sprint 8
**Date Created:** 2026-04-27
**Revised:** 2026-04-27 — Party Mode review: redesign từ collapsible panel → inline chip bar, bổ sung Gantt sync (Bryntum native), Saved Presets, Milestone filter, NgRx architecture chuẩn.

---

## Story

As a PM,
I want một bộ lọc hiện đại luôn hiển thị trên màn task tree và Gantt — với quick presets, chip bar, và saved filters — đồng thời có view "My Tasks" cross-project,
So that tôi thấy đúng task cần quan tâm ngay lập tức, không cần mở panel, không cần set lại filter mỗi ngày.

---

## Acceptance Criteria

### A. Inline Filter Bar — Luôn Hiển Thị (không ẩn sau button)

**AC-1: Layout filter bar**

**Given** PM đang xem project detail (tab Tasks hoặc Gantt)
**When** trang load
**Then** ngay dưới project header / toolbar, hiển thị một hàng filter bar ngang luôn visible gồm:
- Search input (icon 🔍, placeholder "Tìm task...", shortcut `/` để focus)
- Quick Preset chips: `My Tasks` · `Overdue` · `High Priority` · `Unassigned` — mỗi chip là toggle
- Nút `+ Filters` — mở Advanced Filter dropdown cho các filter phức tạp hơn
- Khi có filter active: nút `Clear all` xuất hiện cuối hàng (màu đỏ nhạt)
- Task count: `Showing X / Y tasks` — update real-time

**AC-2: Quick Preset Chips — 1 click, instant**

**Given** filter bar đang hiển thị
**When** PM click một Quick Preset chip
**Then** filter áp dụng ngay, không cần confirm hay click Apply
- **My Tasks** → `assigneeId = currentUserId`
- **Overdue** → `plannedEndDate < today AND status ∉ {Completed, Cancelled}`
- **High Priority** → `priority ∈ {High, Critical}`
- **Unassigned** → `assigneeId = null`

**And** chip đó đổi màu (filled/active state), có dấu `✓`
**And** có thể kết hợp nhiều preset cùng lúc (AND logic)
**And** click lại preset đang active → tắt filter đó

**AC-3: Advanced Filter Dropdown**

**Given** PM click `+ Filters`
**When** dropdown mở ra (không overlay toàn màn hình — chỉ là dropdown nhỏ)
**Then** hiện các filter bổ sung:
- **Status** — multi-select checkboxes: NotStarted / InProgress / OnHold / Delayed / Completed / Cancelled
- **Assignee** — searchable multi-select danh sách project members
- **Priority** — multi-select: Low / Medium / High / Critical
- **Node Type** — multi-select: Phase / Milestone / Task
- **Milestone** — dropdown chọn một milestone cụ thể → filter tasks thuộc milestone đó (bao gồm toàn bộ subtree)
- **Planned End Date range** — date picker from/to (apply khi cả 2 đầu đã chọn)

**And** mỗi filter được chọn ngay lập tức tạo ra một **Active Filter Chip** trên filter bar
**And** dropdown có thể đóng (click outside) mà không mất filter đang active

**AC-4: Active Filter Chips trên Filter Bar**

**Given** user đã áp dụng ít nhất 1 filter từ Advanced dropdown
**When** filter bar render
**Then** mỗi filter active hiển thị dạng chip: `Status: In Progress ×` · `Assignee: Minh ×` · `Milestone: Sprint 2 ×`
**And** click `×` trên từng chip → xóa chỉ filter đó, giữ nguyên các filter còn lại
**And** Quick Preset chips và Active Filter Chips cùng nằm trên 1 hàng, không tạo hàng thứ 2

**AC-5: Apply Strategy (không có nút Apply)**

- **Search input**: debounce 300ms → tự động apply, không cần Enter
- **Checkbox / toggle**: apply ngay khi click (instant, O(n) client-side)
- **Date range**: chỉ apply khi cả `from` và `to` đều có giá trị
- **Milestone**: apply ngay khi chọn

**AC-6: Saved Filter Presets**

**Given** PM đang có một bộ filter active (bất kỳ combination nào)
**When** PM click `Save view` (link nhỏ cuối filter bar)
**Then** hiện input để đặt tên cho preset (ví dụ: "Tasks của tôi tuần này")
**And** preset được lưu, hiện trong dropdown "Saved Views" trên filter bar

**Given** PM click vào "Saved Views"
**When** dropdown hiện ra
**Then** hiển thị các user-defined presets + 3 system defaults: `My Tasks`, `Overdue Tasks`, `Unfinished High Priority`
**And** click preset → áp dụng toàn bộ filter combination của preset đó
**And** nút `✕` xóa user-defined preset (system defaults không xóa được)

**AC-7: Filter State Persistence**

**Given** filter đang active
**When** user refresh trang, hoặc navigate ra rồi quay lại project
**Then** filter được restore chính xác từ URL query params
URL pattern: `/projects/:id?status=InProgress&status=OnHold&assigneeId=xxx&overdue=true`

**Given** user copy URL và share cho người khác (cùng project member)
**When** người khác mở URL
**Then** filter tự động active với cùng criteria

**AC-8: Empty State**

**Given** filter không tìm thấy task nào
**When** task tree hoặc list render
**Then** hiển thị empty state:
```
🔍 Không tìm thấy task nào khớp với bộ lọc hiện tại.
   [Clear all filters]   [Điều chỉnh filters]
```

---

### B. Task Tree — Filter Behavior (client-side, ≤500 tasks)

**AC-9: Tree Integrity khi filter**

**Given** filter active trả về một số tasks match
**When** task tree render
**Then** tasks match → hiển thị bình thường
**And** ancestor nodes (Phase/Milestone cha của task match) → hiển thị nhưng label italic + màu mờ hơn để phân biệt "parent context" vs "match"
**And** tasks không match và không phải ancestor → ẩn hoàn toàn

Ví dụ: Milestone "M1" có 5 tasks, filter chỉ match 2 tasks → M1 vẫn hiện (italic), 3 tasks còn lại ẩn.

**AC-10: Task count badge**

**Given** filter đang active
**When** filter bar hiển thị
**Then** `Showing X / Y tasks` — X là số tasks match (không đếm ancestor context nodes), Y là tổng tasks trong project

---

### C. Gantt View — Filter Behavior (Bryntum native, khác task tree)

**AC-11: Gantt filter dùng Bryntum native — KHÔNG dùng NgRx filteredIds**

**Given** user ở tab Gantt và có filter active
**When** Gantt render
**Then** Gantt áp dụng filter qua `gantt.taskStore.filter()` (Bryntum API) — KHÔNG phải pass filtered data từ NgRx
**And** Bryntum tự bảo toàn tree integrity và dependency arrows

**AC-12: Dim mode là default cho Gantt (không ẩn)**

**Given** filter active trên Gantt view
**When** Gantt render
**Then** tasks không match → **làm mờ (opacity 0.3, màu xám)**, không ẩn hoàn toàn
**And** tasks match → hiển thị bình thường (full color)
**And** dependency arrows của tasks bị mờ cũng bị mờ tương ứng

**Lý do:** Ẩn tasks trên Gantt phá vỡ cấu trúc timeline và làm mất context phụ thuộc.

**AC-13: Toggle Hide / Dim trên Gantt**

**Given** filter active trên Gantt
**When** user click toggle `Ẩn hẳn` / `Làm mờ` (nằm trong filter bar, chỉ visible khi ở Gantt tab)
**Then** Gantt switch giữa hide behavior và dim behavior tương ứng
**And** preference này lưu localStorage key `gantt-filter-mode` (default: `dim`)

**AC-14: Filter bar shared giữa Task Tree và Gantt**

**Given** user set filter ở tab Tasks
**When** user switch sang tab Gantt (hoặc ngược lại)
**Then** filter criteria giữ nguyên — cùng filter state áp dụng cho cả hai view
**And** Gantt toggle "Ẩn/Mờ" chỉ hiển thị khi đang ở Gantt tab

---

### D. My Tasks View (Cross-project)

**AC-15: My Tasks page**

**Given** user đăng nhập
**When** user vào route `/my-tasks` (hoặc click "My Tasks" trên sidebar)
**Then** hiển thị tất cả tasks có `assigneeUserId = currentUserId` trên MỌI project user là member
**And** nhóm theo 4 section:
- 🔴 **Overdue** — plannedEndDate < today AND status ∉ {Completed, Cancelled}
- 🟡 **Due This Week** — plannedEndDate trong 7 ngày tới
- 🔵 **Upcoming** — plannedEndDate sau 7 ngày tới
- ⚪ **No Due Date** — không có plannedEndDate

**And** mỗi task row hiển thị: project name chip · VBS · task name · status badge · priority badge · plannedEndDate

**AC-16: Sub-filter trong My Tasks**

**Given** My Tasks đang hiển thị
**When** user dùng filter bar của My Tasks
**Then** áp dụng filter: theo project (multi-select), theo status (multi-select), theo priority (multi-select)
**And** Quick Presets trên My Tasks: `Overdue` · `Due This Week` · `High Priority`

**AC-17: Deep link từ My Tasks**

**Given** user click vào một task trong My Tasks
**When** navigate
**Then** đến `/projects/:projectId?taskId=:taskId` — project detail tự scroll đến và highlight task đó

---

## Dev Notes / Guardrails

### Stack đã có — KHÔNG tạo lại

- Angular standalone components, lazy-loaded routes — pattern từ Epic 1
- NgRx Store/Effects cho tasks — Story 1.4, chỉ extend, không tạo mới
- `GET /api/v1/projects/{id}/tasks` — đã có, chỉ thêm query params
- `ProjectTask` entity đầy đủ fields từ Story 1.4
- Membership middleware từ Story 1.2 — dùng lại cho `/my-tasks`
- `PagedResult<T>` — đã có từ Story 3.2, dùng lại
- Sidebar navigation từ Story 1.9 — thêm "My Tasks" vào đây

---

### NgRx Architecture — Bắt Buộc Theo

```typescript
// FilterCriteria — PHẢI serializable JSON (dự phòng Saved Presets)
interface FilterCriteria {
  keyword?: string;
  statuses?: TaskStatus[];       // enum values, không phải label
  assigneeIds?: string[];        // uuid[], 'UNASSIGNED' = special value
  priorities?: TaskPriority[];
  nodeTypes?: TaskType[];
  milestoneId?: string;          // uuid của milestone node
  dueDateFrom?: string;          // ISO date 'YYYY-MM-DD'
  dueDateTo?: string;
  overdueOnly?: boolean;
}

// FilterPreset — schema dự phòng, implement localStorage trong story này
interface FilterPreset {
  id: string;
  name: string;
  criteria: FilterCriteria;      // serializable → lưu được
  isSystem?: boolean;            // system defaults không xóa được
}

// NgRx tasks state — extend từ Story 1.4
interface TasksState {
  ids: string[];                 // EntityAdapter
  entities: Record<string, TaskDto>;
  activeFilter: FilterCriteria;  // NEW — source of truth filter
  filteredIds: string[];         // NEW — computed bởi selector, KHÔNG lưu entities mới
  totalCount: number;            // tổng tasks không filter (để hiện X/Y)
  loading: boolean;
  error: string | null;
}
```

**CRITICAL — `filteredIds` là pure memoized selector, KHÔNG phải state:**
```typescript
// ĐÚng: selector memoized
export const selectFilteredTaskIds = createSelector(
  selectAllTasks,
  selectActiveFilter,
  (tasks, filter) => applyFilter(tasks, filter).map(t => t.id)
  // createSelector tự memoize — không tạo array mới nếu input không đổi
);

// SAI: lưu filteredIds vào state rồi update mỗi lần filter → re-render cascade
```

---

### URL ↔ NgRx Sync — Hai chiều, dùng NgRx Router Store

```typescript
// 1. URL → Store: Effect đọc route params khi navigate
loadFilterFromUrl$ = createEffect(() =>
  this.actions$.pipe(
    ofType(ROUTER_NAVIGATED),
    map(action => {
      const params = action.payload.routerState.queryParams;
      return TasksActions.setFilter({ filter: parseQueryParams(params) });
    })
  )
);

// 2. Store → URL: Effect update URL khi filter thay đổi
syncFilterToUrl$ = createEffect(() =>
  this.actions$.pipe(
    ofType(TasksActions.setFilter),
    tap(({ filter }) => {
      this.router.navigate([], {
        queryParams: serializeFilter(filter),
        queryParamsHandling: 'merge',
        replaceUrl: true  // không tạo history entry mới mỗi lần filter
      });
    })
  ), { dispatch: false }
);
```

---

### Gantt Integration — Execution Path Riêng

**CRITICAL: Không pass filtered data vào Gantt. Dùng Bryntum native filter API.**

```typescript
// gantt-view.component.ts
ngOnInit() {
  this.store.select(selectActiveFilter).pipe(
    distinctUntilChanged(isEqual),  // deep compare, không trigger thừa
    takeUntilDestroyed()
  ).subscribe(filter => {
    this.applyGanttFilter(filter);
  });
}

private applyGanttFilter(filter: FilterCriteria) {
  this.gantt.taskStore.clearFilters();
  if (isEmpty(filter)) return;

  const ganttFilters = this.mapToGanttFilters(filter);
  // Bryntum filter() giữ tree integrity + dependency arrows
  this.gantt.taskStore.filter({ filters: ganttFilters, replace: true });
}

private mapToGanttFilters(f: FilterCriteria): BryntumFilter[] {
  const filters: BryntumFilter[] = [];
  if (f.keyword) filters.push({ property: 'name', operator: '*', value: f.keyword });
  if (f.statuses?.length) filters.push({ property: 'status', operator: 'isIncludedIn', value: f.statuses });
  // ... map các criteria khác
  return filters;
}
```

**Dim mode (default) — custom Bryntum renderer:**
```typescript
// Khi ganttFilterMode = 'dim': không dùng taskStore.filter()
// Thay vào đó, dùng taskRenderer để style tasks không match
gantt.taskRenderer = ({ taskRecord, renderData }) => {
  const matches = this.taskMatchesFilter(taskRecord, this.activeFilter);
  renderData.cls.add(matches ? 'task-match' : 'task-dim');
};
// CSS: .task-dim { opacity: 0.25; filter: grayscale(80%); }
```

---

### API Changes (Backend)

**Endpoint 1: GET /api/v1/projects/{projectId}/tasks — thêm filter params**
```
?keyword=string
&status=NotStarted&status=InProgress        (multi, enum values)
&assigneeId=uuid1&assigneeId=UNASSIGNED     (multi; UNASSIGNED → WHERE assignee_user_id IS NULL)
&priority=High&priority=Critical            (multi)
&nodeType=Task&nodeType=Milestone           (multi, TaskType enum)
&milestoneId=uuid                           (filter tasks thuộc subtree của milestone này)
&dueDateFrom=2025-01-01                     (ISO date, inclusive)
&dueDateTo=2025-03-31                       (ISO date, inclusive)
&overdueOnly=true                           (bool)
&includeAncestors=true                      (bool — PHẢI có: trả về ancestor chain của tasks match để FE build tree đúng)
&pageSize=500
&cursor=string                              (cursor-based pagination, không offset)
```

**`includeAncestors=true` logic trong Handler:**
```csharp
// Sau khi filter ra matchingTaskIds:
var matchingIds = query.Select(t => t.Id).ToHashSet();
var ancestorIds = await GetAllAncestorIds(matchingIds, dbContext);
var allIds = matchingIds.Union(ancestorIds);
// Trả về tất cả, FE tự biết cái nào là "match" vs "ancestor context"
// → thêm bool IsFilterMatch vào TaskDto: true = match, false = ancestor context
```

**TaskDto bổ sung:**
```csharp
record TaskDto {
  // ... fields hiện tại ...
  bool? IsFilterMatch { get; init; } // null khi không có filter, true/false khi có filter
}
```

**Endpoint 2: GET /api/v1/my-tasks — NEW**
```
?status=multi
&priority=multi
&projectId=multi
&overdueOnly=bool
&dueThisWeek=bool
&pageSize=100
&cursor=string
Response: PagedResult<MyTaskDto>

record MyTaskDto : TaskDto {
  Guid ProjectId { get; init; }
  string ProjectName { get; init; }
  string ProjectCode { get; init; }
  string MilestoneName { get; init; }  // tên milestone cha nếu có
}
```

---

### FE Module Structure

```
features/projects/components/
  filter-bar/
    filter-bar.component.ts          (standalone — dùng chung cho task tree + Gantt)
    filter-bar.component.html
    filter-bar.component.scss
    advanced-filter-dropdown/
      advanced-filter-dropdown.component.ts
    saved-presets-dropdown/
      saved-presets-dropdown.component.ts
  task-tree/                         (đã có từ Story 1.4 — extend)
    task-tree.ts                     (nhận filteredIds từ selector)

features/my-tasks/
  my-tasks.component.ts              (lazy-loaded /my-tasks)
  my-tasks.component.html
  my-tasks.effects.ts
  my-tasks.selectors.ts
  my-task-card/
    my-task-card.component.ts

store/tasks/
  tasks.state.ts                     (extend: activeFilter, filteredIds selector)
  tasks.actions.ts                   (thêm: setFilter, clearFilter, loadFilterFromUrl)
  tasks.effects.ts                   (thêm: URL sync effects)
  tasks.selectors.ts                 (thêm: selectFilteredTaskIds — memoized)
  filter.model.ts                    (FilterCriteria, FilterPreset interfaces)
  filter.utils.ts                    (applyFilter, parseQueryParams, serializeFilter, isEmpty)
```

---

### Saved Presets — Lưu localStorage (không cần API)

```typescript
// filter-presets.service.ts (injectable)
const SYSTEM_PRESETS: FilterPreset[] = [
  { id: 'sys-my-tasks', name: 'My Tasks', criteria: { assigneeIds: ['CURRENT_USER'] }, isSystem: true },
  { id: 'sys-overdue', name: 'Overdue Tasks', criteria: { overdueOnly: true }, isSystem: true },
  { id: 'sys-high-priority', name: 'Unfinished High Priority', criteria: { priorities: ['High','Critical'], statuses: ['NotStarted','InProgress','OnHold','Delayed'] }, isSystem: true },
];
// User presets: localStorage key 'task-filter-presets-v1', max 10 items
// Serialize: JSON.stringify(FilterCriteria) — phải serializable, không chứa Date objects
```

---

### Performance

| Scenario | Strategy |
|---|---|
| Project ≤ 500 tasks | Client-side filter qua `selectFilteredTaskIds` selector. Không gọi API thêm. |
| Project > 500 tasks | Server-side: dispatch `loadTasks` với filter params, API trả filtered + ancestors |
| Ngưỡng switch | Check `totalCount` từ response đầu tiên → lưu vào state, tất cả filter sau đó biết dùng chiều nào |
| Gantt filter | Luôn dùng Bryntum native (không phụ thuộc ngưỡng 500) |

---

### UI Spec — Filter Bar

```
┌─────────────────────────────────────────────────────────────────────┐
│ [🔍 Tìm task...    ] [My Tasks] [Overdue] [High Priority] [+ Filters]│
│ Active: Status: In Progress × │ Assignee: Minh × │ Milestone: M1 ×  │
│ Saved Views ▾                                    Showing 12/47 tasks │
│                                                          [Clear all]  │
└─────────────────────────────────────────────────────────────────────┘
```

- Search input: `min-width: 200px`, focus khi user nhấn `/`
- Quick preset chips: `mat-chip-option` (toggleable), màu accent khi active
- Active filter chips: `mat-chip` với `×` remove button, màu primary-light
- Advanced dropdown: `mat-menu` hoặc CDK Overlay, `min-width: 280px`, max-height scroll
- Task count: right-aligned, font secondary, `color: var(--mat-sys-on-surface-variant)`
- Clear all: `color: warn`, chỉ hiện khi có ít nhất 1 filter active
- **Gantt-only toggle** (chỉ hiện khi tab Gantt): `[Làm mờ ▾]` dropdown với 2 options: "Làm mờ tasks không khớp" / "Ẩn tasks không khớp"

---

### Testing

- Unit: `FilterBarComponent` — preset chips toggle đúng criteria
- Unit: `selectFilteredTaskIds` selector — test từng filter criterion riêng biệt
- Unit: `applyFilter()` util — edge cases: empty filter = no-op, ancestor inclusion logic
- Unit: `GetTasksByProjectQuery` handler — test `includeAncestors`, test `UNASSIGNED` special value
- Unit: `GetMyTasksQuery` handler — verify membership-only, verify sort order overdue-first
- Unit: `FilterPresetsService` — save/load/delete localStorage
- Integration: URL ↔ Store sync — navigate với query params → verify store state

---

## Tasks / Subtasks

### Backend

- [ ] **Task 1: Mở rộng GetTasksByProjectQuery với filter + includeAncestors**
  - [ ] 1.1 Tạo `FilterCriteria` record: Keyword, Statuses[], AssigneeIds[] (UNASSIGNED special), Priorities[], NodeTypes[], MilestoneId?, DueDateFrom?, DueDateTo?, OverdueOnly, IncludeAncestors, PageSize, Cursor
  - [ ] 1.2 Cập nhật `GetTasksByProjectQuery` + Handler: build IQueryable filter chain
  - [ ] 1.3 Implement `GetAllAncestorIds()` helper (recursive CTE hoặc application-side walk)
  - [ ] 1.4 Bổ sung `IsFilterMatch` vào `TaskDto` (null khi không filter, bool khi có filter)
  - [ ] 1.5 Cập nhật `TasksController` — map query params → FilterCriteria; bind `includeAncestors=true` mặc định khi có filter
  - [ ] 1.6 Unit test: mỗi filter criterion + ancestor logic

- [ ] **Task 2: Tạo GetMyTasksQuery + MyTaskDto**
  - [ ] 2.1 Tạo `MyTaskDto` record (extends TaskDto + ProjectId, ProjectName, ProjectCode, MilestoneName)
  - [ ] 2.2 `GetMyTasksQuery` Handler: join tasks + projects + membership, `WHERE assignee_user_id = @currentUserId`
  - [ ] 2.3 Sort: overdue trước → due_this_week → upcoming → no_date; trong mỗi nhóm: plannedEndDate ASC
  - [ ] 2.4 Tạo `MyTasksController` → `GET /api/v1/my-tasks` với filter params
  - [ ] 2.5 Unit test: membership-only, sort order

### Frontend

- [ ] **Task 3: FilterCriteria model + utils**
  - [ ] 3.1 Tạo `filter.model.ts`: `FilterCriteria`, `FilterPreset` interfaces
  - [ ] 3.2 Tạo `filter.utils.ts`: `applyFilter(tasks, criteria): TaskDto[]`, `parseQueryParams(params): FilterCriteria`, `serializeFilter(criteria): Params`, `isEmpty(criteria): boolean`, `criteriaEquals(a, b): boolean`
  - [ ] 3.3 Unit test toàn bộ utils — đặc biệt ancestor inclusion và serialization round-trip

- [ ] **Task 4: NgRx tasks state extension**
  - [ ] 4.1 Thêm `activeFilter: FilterCriteria` + `totalCount: number` vào `TasksState`
  - [ ] 4.2 Thêm actions: `setFilter`, `clearFilter`, `clearOneCriterion`
  - [ ] 4.3 Tạo `selectFilteredTaskIds` selector (memoized `createSelector`) — KHÔNG lưu filteredIds vào state
  - [ ] 4.4 Thêm effects: `loadFilterFromUrl$` (ROUTER_NAVIGATED → setFilter) + `syncFilterToUrl$` (setFilter → Router.navigate replaceUrl)
  - [ ] 4.5 Unit test selectors với mock state

- [ ] **Task 5: FilterBarComponent**
  - [ ] 5.1 Tạo `FilterBarComponent` (standalone): nhận `projectId`, `milestones[]`, `members[]`; emit `filterChange: FilterCriteria`
  - [ ] 5.2 Search input: debounce 300ms, focus on `/` keydown
  - [ ] 5.3 Quick Preset chips: My Tasks / Overdue / High Priority / Unassigned — toggleable `mat-chip-option`
  - [ ] 5.4 Active filter chips: render từng criterion active, `×` dispatch `clearOneCriterion`
  - [ ] 5.5 `+ Filters` button → mở `AdvancedFilterDropdownComponent`
  - [ ] 5.6 `Saved Views` dropdown → mở `SavedPresetsDropdownComponent`
  - [ ] 5.7 Task count `Showing X/Y` (X = filteredIds.length, Y = totalCount)
  - [ ] 5.8 `Clear all` button → dispatch `clearFilter`, chỉ visible khi `!isEmpty(activeFilter)`

- [ ] **Task 6: AdvancedFilterDropdownComponent**
  - [ ] 6.1 Status multi-select checkboxes
  - [ ] 6.2 Assignee searchable multi-select (danh sách members từ project)
  - [ ] 6.3 Priority multi-select
  - [ ] 6.4 Node Type multi-select
  - [ ] 6.5 Milestone dropdown (load milestones của project từ tasks store — filter nodeType=Milestone)
  - [ ] 6.6 Planned End Date range (2 date inputs, apply chỉ khi cả 2 có giá trị)

- [ ] **Task 7: SavedPresetsDropdownComponent + FilterPresetsService**
  - [ ] 7.1 `FilterPresetsService`: load/save/delete từ localStorage `task-filter-presets-v1`
  - [ ] 7.2 Tích hợp 3 system presets (không xóa được)
  - [ ] 7.3 UI: list presets, click → dispatch setFilter; `✕` xóa user preset; `Save current view` → input đặt tên

- [ ] **Task 8: Gantt integration — Bryntum native filter**
  - [ ] 8.1 Trong `GanttViewComponent`: subscribe `selectActiveFilter`, gọi `applyGanttFilter()`
  - [ ] 8.2 Implement `mapToGanttFilters(criteria): BryntumFilter[]`
  - [ ] 8.3 Implement **dim mode** (default): custom `taskRenderer` add/remove CSS class `.task-dim`
  - [ ] 8.4 Implement **hide mode**: dùng `taskStore.filter()` thay vì renderer
  - [ ] 8.5 Gantt-only toggle "Làm mờ / Ẩn" trong FilterBar (chỉ render khi `[ganttMode]="true"`)
  - [ ] 8.6 Persist gantt-filter-mode preference vào localStorage `gantt-filter-mode`

- [ ] **Task 9: Tích hợp FilterBar vào project detail**
  - [ ] 9.1 Đặt `<app-filter-bar>` ngay dưới project header, trên cả Tasks tab và Gantt tab
  - [ ] 9.2 Tasks tab: `selectFilteredTaskIds` → task-tree component nhận ids → render filtered tree
  - [ ] 9.3 Gantt tab: FilterBar emit filter → GanttViewComponent apply Bryntum native filter
  - [ ] 9.4 Switch tab Tasks ↔ Gantt: filter state giữ nguyên (shared NgRx state)

- [ ] **Task 10: My Tasks Page**
  - [ ] 10.1 Tạo `MyTasksComponent` (lazy-loaded `/my-tasks`)
  - [ ] 10.2 Tạo `my-tasks-api.service.ts` → `GET /api/v1/my-tasks`
  - [ ] 10.3 NgRx my-tasks slice: actions, effects, selectors
  - [ ] 10.4 Render 4 sections: Overdue / Due This Week / Upcoming / No Due Date
  - [ ] 10.5 `MyTaskCardComponent`: project chip, VBS, task name, status badge, priority badge, due date, overdue tag
  - [ ] 10.6 Filter bar cho My Tasks (subset: project filter, status, priority, quick presets)
  - [ ] 10.7 Thêm "My Tasks" vào sidebar (Story 1.9)
  - [ ] 10.8 Deep link: click task → `/projects/:projectId?taskId=:taskId`; project detail scroll + highlight task

- [ ] **Task 11: Build + smoke test**
  - [ ] 11.1 `dotnet build` → 0 errors
  - [ ] 11.2 `ng build` → 0 errors
  - [ ] 11.3 Manual: set 3 filters → refresh → verify restore từ URL
  - [ ] 11.4 Manual: filter trên Gantt tab → switch Tasks tab → filter còn nguyên
  - [ ] 11.5 Manual: save preset → navigate đi → quay lại → apply preset

---

## Completion Criteria

Story hoàn thành khi:
- Filter bar luôn hiển thị (không hidden), có đủ Quick Presets + Advanced dropdown
- Inline active filter chips với X xóa từng filter
- Filter by Milestone hoạt động (filter cả subtree)
- Saved presets: lưu, load, xóa user presets; 3 system presets mặc định
- URL persistence: filter restore sau refresh, URL shareable
- Gantt: dim mode mặc định, toggle sang hide mode hoạt động, dùng Bryntum native filter
- Filter state shared khi switch giữa Tasks tab và Gantt tab
- My Tasks cross-project: 4 sections, deep link, filter bar
- `dotnet build` + `ng build` → 0 errors
