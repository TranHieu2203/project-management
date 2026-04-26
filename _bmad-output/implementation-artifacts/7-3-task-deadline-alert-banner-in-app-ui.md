# Story 7.3: Task deadline visual alerts — in-app UI (Project Detail + Gantt)

Status: review

**Story ID:** 7.3
**Epic:** Epic 7 — Operations Layer (Notifications + In-product transparency metrics)
**Sprint:** Sprint 9
**Date Created:** 2026-04-26
**Last Revised:** 2026-04-26 (sau roundtable Sally × Winston × John × Amelia)

---

## Story

As a PM,
I want thấy ngay trên màn Project Detail và Gantt những task nào quá hạn / đến hạn hôm nay / sắp đến hạn (7 ngày),
So that tôi không cần mở email digest hay scroll toàn bộ task tree mới phát hiện rủi ro deadline.

---

## Design Decisions (đã chốt qua roundtable)

| # | Vấn đề | Quyết định |
|---|--------|-----------|
| 1 | Banner collapsible? | **Không.** Banner compact ~48px, always visible khi có alert, tự ẩn khi không có. Không có dismiss/collapse button. Không localStorage. |
| 2 | Click badge → hành vi? | **Filter + highlight in-place** tất cả task thuộc nhóm đó. Scroll đến task đầu tiên trong nhóm. `activeDeadlineFilter` state quản lý ở `ProjectDetailComponent` (không ở Banner). |
| 3 | Phase/Milestone indicator? | **Out of scope.** Chỉ tô `type='Task'`. `EXCLUDED_TYPES` constant trong service đảm bảo extensibility cho Story 7-4. |
| 4 | Timezone "today"? | **Local date** của browser. Dùng `getLocalDateString()` helper — không dùng `toISOString()` (UTC trap). |

---

## Acceptance Criteria

1. **Given** PM mở Project Detail (`/projects/{id}`)
   **When** có task `type='Task'`, chưa hoàn thành (`status ∉ {Completed, Cancelled}`), `plannedEndDate < localToday`
   **Then** task row được tô màu đỏ nhạt (`.row-overdue`) và alert banner phía trên hiển thị badge "X quá hạn" màu đỏ

2. **Given** PM mở Project Detail
   **When** có task `type='Task'` chưa hoàn thành với `plannedEndDate === localToday`
   **Then** task row được tô màu cam nhạt (`.row-due-today`) và banner hiển thị badge "Y đến hạn hôm nay" màu cam

3. **Given** PM mở Project Detail
   **When** có task `type='Task'` chưa hoàn thành với `localToday < plannedEndDate ≤ localToday + 7 ngày`
   **Then** task row được tô màu vàng nhạt (`.row-due-soon`) và banner hiển thị badge "Z sắp đến hạn (7 ngày)" màu vàng

4. **Given** alert banner hiển thị với badge "X quá hạn"
   **When** PM click badge đó
   **Then** tất cả task thuộc nhóm overdue được highlight (row outline/border) trong task tree
   **And** task tree scroll đến task overdue đầu tiên (theo sortOrder)
   **And** `activeDeadlineFilter = 'overdue'` được set trên parent component

5. **Given** PM click badge nhóm khác (due-today / due-soon)
   **When** click xảy ra
   **Then** highlight chuyển sang nhóm mới, scroll đến task đầu tiên của nhóm mới
   **And** `activeDeadlineFilter` cập nhật theo

6. **Given** PM click lại badge của nhóm đang active
   **When** click xảy ra
   **Then** `activeDeadlineFilter = null` — tất cả highlight tắt (toggle off)

7. **Given** PM mở Gantt view (`/projects/{id}/gantt`)
   **When** task `type='Task'` có deadline status (overdue / due-today / due-soon)
   **Then** thanh Gantt có CSS class tương ứng (`.b-task-overdue` / `.b-task-due-today` / `.b-task-due-soon`) kèm đường viền màu
   **And** cùng alert banner xuất hiện phía trên Bryntum component

8. **Given** task có `plannedEndDate = null` hoặc `status ∈ {Completed, Cancelled}` hoặc `type ∈ {Phase, Milestone}`
   **When** tính deadline status
   **Then** task được bỏ qua — deadline status = 'none', không tô màu, không đếm vào banner

9. **Given** không có task nào thuộc 3 nhóm deadline
   **When** render màn
   **Then** alert banner KHÔNG render (dùng `@if` ở parent, không render component rỗng)

---

## Tasks / Subtasks

- [ ] **Task 1: DeadlineAlertService**
  - [ ] 1.1 Tạo `features/projects/services/deadline-alert.service.ts`
    - `export type DeadlineStatus = 'overdue' | 'due-today' | 'due-soon' | 'none'`
    - `export interface DeadlineSummary { overdue: ProjectTask[]; dueToday: ProjectTask[]; dueSoon: ProjectTask[]; }`
    - `const EXCLUDED_TYPES = ['Phase', 'Milestone'] as const` — constant array, extensible cho 7-4
    - `const DONE_STATUSES = new Set(['Completed', 'Cancelled'])`
    - `const DUE_SOON_DAYS = 7`
    - `getDeadlineStatus(task: ProjectTask, today: string): DeadlineStatus`
    - `computeDeadlineSummary(tasks: ProjectTask[], today: string): DeadlineSummary`
    - `getLocalDateString(): string` — helper tính local date (xem code mẫu bên dưới)
    - Không inject HttpClient, không dependency NgRx

- [ ] **Task 2: DeadlineAlertBannerComponent**
  - [ ] 2.1 Tạo `features/projects/components/deadline-alert-banner/deadline-alert-banner.ts`
    - Standalone, OnPush
    - `@Input() summary!: DeadlineSummary`
    - `@Input() activeFilter: DeadlineStatus | null = null` — nhận từ parent
    - `@Output() filterChange = new EventEmitter<DeadlineStatus | null>()` — emit khi click badge
    - Click badge: emit `filterChange` với group đó; nếu group đang active → emit `null` (toggle off)
    - Không có collapsed/expand state, không localStorage
    - Height cố định ~48px khi không có task nào → không render (parent dùng `@if`)
  - [ ] 2.2 Tạo `deadline-alert-banner.html`
    - Layout: horizontal flex bar, height 48px
    - 3 badge chips điều kiện: chỉ hiển thị chip có count > 0
    - Badge active (= `activeFilter`) có visual distinction (outline/filled)
    - Không có nút collapse, không có nút dismiss
  - [ ] 2.3 Tạo `deadline-alert-banner.scss`
    - `.deadline-banner`: `height: 48px`, flex row, `background: var(--mat-sys-surface-variant)`, border-bottom 1px
    - `.badge-overdue`: dùng `color: var(--mat-sys-error)`, background tương ứng
    - `.badge-today`: orange (#e65100 hoặc Material Deep Orange)
    - `.badge-soon`: amber (#f9a825 hoặc Material Amber)
    - `.badge-active`: outlined variant khi đang active filter

- [ ] **Task 3: Tích hợp ProjectDetailComponent**
  - [ ] 3.1 Sửa `project-detail.ts`:
    - Import `DeadlineAlertBannerComponent`, `DeadlineAlertService`
    - `private readonly deadlineService = inject(DeadlineAlertService)`
    - `readonly today = this.deadlineService.getLocalDateString()` — tính 1 lần khi component init
    - `readonly deadlineSummary$ = this.tasks$.pipe(map(tasks => this.deadlineService.computeDeadlineSummary(tasks, this.today)))`
    - `activeDeadlineFilter = signal<DeadlineStatus | null>(null)` — quản lý filter state tại đây
    - `highlightTaskId = signal<string | null>(null)`
    - `onFilterChange(filter: DeadlineStatus | null, summary: DeadlineSummary): void` — set filter, scroll to first task
  - [ ] 3.2 Sửa `project-detail.html`:
    - `@if (deadlineSummary$ | async; as summary)` bao quanh `<app-deadline-alert-banner>`
    - `[summary]="summary"` `[activeFilter]="activeDeadlineFilter()"` `(filterChange)="onFilterChange($event, summary)"`
    - Truyền `[activeDeadlineFilter]="activeDeadlineFilter()"` và `[highlightTaskId]="highlightTaskId()"` xuống `<app-task-tree>`
  - [ ] 3.3 Implement `onFilterChange`:
    ```typescript
    onFilterChange(filter: DeadlineStatus | null, summary: DeadlineSummary): void {
      this.activeDeadlineFilter.set(filter);
      if (!filter) { this.highlightTaskId.set(null); return; }
      const group = filter === 'overdue' ? summary.overdue
                  : filter === 'due-today' ? summary.dueToday
                  : summary.dueSoon;
      if (!group.length) return;
      this.highlightTaskId.set(group[0].id);
      setTimeout(() => {
        document.querySelector(`[data-task-id="${group[0].id}"]`)
          ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }, 50);
    }
    ```

- [ ] **Task 4: Row tinting + active filter highlight trong TaskTreeComponent**
  - [ ] 4.1 Sửa `task-tree.ts`:
    - Inject `DeadlineAlertService`
    - `@Input() today: string = this.deadlineService.getLocalDateString()`
    - `@Input() activeDeadlineFilter: DeadlineStatus | null = null`
    - `@Input() highlightTaskId: string | null = null`
    - Method `rowClasses(task: ProjectTask): Record<string, boolean>`:
      ```typescript
      rowClasses(task: ProjectTask) {
        const s = this.deadlineService.getDeadlineStatus(task, this.today);
        return {
          'row-overdue':   s === 'overdue',
          'row-due-today': s === 'due-today',
          'row-due-soon':  s === 'due-soon',
          'row-filtered':  !!this.activeDeadlineFilter && s === this.activeDeadlineFilter,
          'row-highlight': task.id === this.highlightTaskId,
        };
      }
      ```
  - [ ] 4.2 Sửa `task-tree.html`:
    - `[ngClass]="rowClasses(node.task)"` trên `.grid-row`
    - `[attr.data-task-id]="node.task.id"` trên `.grid-row`
  - [ ] 4.3 Sửa `task-tree.scss`:
    ```scss
    .grid-row {
      &.row-overdue   { background-color: rgba(211,47,47,0.08); }
      &.row-due-today { background-color: rgba(230,120,0,0.08); }
      &.row-due-soon  { background-color: rgba(245,200,0,0.07); }
      &.row-filtered  { outline: 2px solid currentColor; outline-offset: -2px; }
      &.row-highlight { animation: highlight-pulse 0.8s ease; }
    }
    @keyframes highlight-pulse {
      0%, 100% { background-color: transparent; }
      50% { background-color: rgba(var(--mat-sys-primary-rgb, 25,118,210), 0.18); }
    }
    ```

- [ ] **Task 5: Tích hợp Gantt view**
  - [ ] 5.1 Tìm GanttComponent: scan `features/projects/components/` cho file import Bryntum (ref story `1-5-gantt-adapter-layer-bryntum-initial-integration-read-render-split-panel.md`)
  - [ ] 5.2 Sửa GanttComponent:
    - Inject `DeadlineAlertService`; capture reference: `private readonly _deadline = inject(DeadlineAlertService)`
    - `readonly today = this._deadline.getLocalDateString()` — tính 1 lần
    - `readonly taskMap = new Map<string, ProjectTask>()` — build từ tasks Input khi set
    - Trong Bryntum config, `taskRenderer` là arrow function capture `this._deadline` và `this.taskMap`:
      ```typescript
      taskRenderer: ({ taskRecord, renderData }) => {
        const task = this.taskMap.get(String(taskRecord.id));
        if (!task) return;
        const s = this._deadline.getDeadlineStatus(task, this.today);
        renderData.cls.remove('b-task-overdue', 'b-task-due-today', 'b-task-due-soon');
        if (s !== 'none') renderData.cls.add(`b-task-${s}`);
      }
      ```
    - Pattern arrow function closure capture tránh `this` context bị mất trong Bryntum callback
  - [ ] 5.3 Thêm CSS vào Gantt SCSS:
    ```scss
    .b-task-overdue   .b-gantt-task { border: 2px solid #d32f2f !important; }
    .b-task-due-today .b-gantt-task { border: 2px solid #e65100 !important; }
    .b-task-due-soon  .b-gantt-task { border: 2px solid #f9a825 !important; }
    ```
  - [ ] 5.4 Thêm `<app-deadline-alert-banner>` phía trên Bryntum wrapper (Gantt không có filter/highlight, chỉ có banner show/hide)
    - Gantt banner: `activeFilter = null` cố định (click badge trên Gantt chỉ scroll, không filter tree)

- [ ] **Task 6: Test cases**
  - [ ] 6.1 Tạo `deadline-alert.service.spec.ts`:
    - `getDeadlineStatus`: task type=Phase → 'none'
    - `getDeadlineStatus`: task type=Milestone → 'none'
    - `getDeadlineStatus`: plannedEndDate=null → 'none'
    - `getDeadlineStatus`: status=Completed → 'none'
    - `getDeadlineStatus`: status=Cancelled → 'none'
    - `getDeadlineStatus`: plannedEndDate < today → 'overdue'
    - `getDeadlineStatus`: plannedEndDate === today → 'due-today' (boundary)
    - `getDeadlineStatus`: plannedEndDate = today+1 → 'due-soon'
    - `getDeadlineStatus`: plannedEndDate = today+7 → 'due-soon' (boundary)
    - `getDeadlineStatus`: plannedEndDate = today+8 → 'none'
    - `getLocalDateString`: mock Date, verify format YYYY-MM-DD theo local timezone (không phải UTC)
    - `computeDeadlineSummary`: empty array → summary all empty
    - `computeDeadlineSummary`: mixed tasks → correct grouping
  - [ ] 6.2 Tạo `deadline-alert-banner.spec.ts`:
    - Không render khi summary rỗng (handled ở parent, nhưng test Input binding)
    - Render badge overdue khi `summary.overdue.length > 0`
    - Không render badge khi count = 0
    - Click badge → emit `filterChange` với đúng group
    - Click badge đang active → emit `filterChange` với null (toggle)
    - `activeFilter` input → badge active styling
  - [ ] 6.3 Thêm test vào `task-tree.spec.ts`:
    - `rowClasses` trả `{'row-overdue': true}` cho Task overdue
    - `rowClasses` trả `{}` (no deadline class) cho Phase overdue
    - `rowClasses` trả `{'row-filtered': true}` khi `activeDeadlineFilter` match
    - `data-task-id` attribute có trên `.grid-row`

- [ ] **Task 7: Build verification**
  - [ ] 7.1 `ng build --configuration development` → 0 errors

---

## Dev Notes

### getLocalDateString() — local timezone, không phải UTC

```typescript
// Trong DeadlineAlertService
getLocalDateString(): string {
  const d = new Date();
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}
```

**Tại sao không dùng `toISOString().split('T')[0]`?**
`toISOString()` trả UTC. PM ở UTC+7, lúc 23:30 ngày 26/04 thì `toISOString()` trả `"2026-04-26"` nhưng giờ local đã là `"2026-04-27"`. Kết quả: "due-today" thành "overdue" lúc nửa đêm → false alarm. Dùng `getFullYear/getMonth/getDate` lấy local date của browser.

**Lưu ý addDays():** hàm này dùng `new Date(date + 'T00:00:00')` — suffix `T00:00:00` (không có Z) đảm bảo parse theo local timezone, không UTC.

---

### DeadlineAlertService — full implementation

```typescript
// File: features/projects/services/deadline-alert.service.ts
import { Injectable } from '@angular/core';
import { ProjectTask } from '../models/task.model';

export type DeadlineStatus = 'overdue' | 'due-today' | 'due-soon' | 'none';

export interface DeadlineSummary {
  overdue: ProjectTask[];
  dueToday: ProjectTask[];
  dueSoon: ProjectTask[];
}

const EXCLUDED_TYPES: ReadonlyArray<string> = ['Phase', 'Milestone'];
const DONE_STATUSES = new Set(['Completed', 'Cancelled']);
const DUE_SOON_DAYS = 7;

@Injectable({ providedIn: 'root' })
export class DeadlineAlertService {

  getLocalDateString(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
  }

  getDeadlineStatus(task: ProjectTask, today: string): DeadlineStatus {
    if (EXCLUDED_TYPES.includes(task.type)) return 'none';
    if (!task.plannedEndDate || DONE_STATUSES.has(task.status)) return 'none';
    if (task.plannedEndDate < today) return 'overdue';
    if (task.plannedEndDate === today) return 'due-today';
    if (task.plannedEndDate <= this.addDays(today, DUE_SOON_DAYS)) return 'due-soon';
    return 'none';
  }

  computeDeadlineSummary(tasks: ProjectTask[], today: string): DeadlineSummary {
    const summary: DeadlineSummary = { overdue: [], dueToday: [], dueSoon: [] };
    for (const task of tasks) {
      const s = this.getDeadlineStatus(task, today);
      if (s === 'overdue')    summary.overdue.push(task);
      else if (s === 'due-today') summary.dueToday.push(task);
      else if (s === 'due-soon')  summary.dueSoon.push(task);
    }
    return summary;
  }

  private addDays(date: string, days: number): string {
    const d = new Date(date + 'T00:00:00'); // local timezone parse
    d.setDate(d.getDate() + days);
    return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
  }
}
```

---

### Cấu trúc file mới

```
frontend/.../features/projects/
├── services/
│   └── deadline-alert.service.ts          ← MỚI
│   └── deadline-alert.service.spec.ts     ← MỚI
├── components/
│   └── deadline-alert-banner/
│       ├── deadline-alert-banner.ts        ← MỚI
│       ├── deadline-alert-banner.html      ← MỚI
│       ├── deadline-alert-banner.scss      ← MỚI
│       └── deadline-alert-banner.spec.ts   ← MỚI
```

---

### Bryntum taskRenderer — DI pattern đúng

Bryntum callback chạy ngoài Angular DI context. **Không** inject service trong callback. Pattern đúng:

```typescript
export class GanttComponent implements OnInit {
  private readonly _deadline = inject(DeadlineAlertService);
  private readonly taskMap = new Map<string, ProjectTask>();
  readonly today = this._deadline.getLocalDateString();

  // Build taskMap từ tasks Input
  @Input() set tasks(tasks: ProjectTask[]) {
    this.taskMap.clear();
    tasks.forEach(t => this.taskMap.set(t.id, t));
  }

  readonly ganttConfig = {
    // Arrow function closure captures this._deadline và this.taskMap
    taskRenderer: ({ taskRecord, renderData }: any) => {
      const task = this.taskMap.get(String(taskRecord.id));
      if (!task) return;
      renderData.cls.remove('b-task-overdue', 'b-task-due-today', 'b-task-due-soon');
      const s = this._deadline.getDeadlineStatus(task, this.today);
      if (s !== 'none') renderData.cls.add(`b-task-${s}`);
    },
  };
}
```

`renderData.cls.remove(...)` trước khi `add` — tránh class stale khi task thay đổi status mà Bryntum reuse element.

---

### CSS specificity — row deadline vs depth tinting

```scss
// task-tree.scss
.grid-row {
  // Depth tinting (specificity 0,1,0)
  &.row-depth-0 { ... }

  // Deadline tinting (specificity 0,2,0) — thắng depth tinting
  &.row-overdue   { background-color: rgba(211,47,47,0.08); }
  &.row-due-today { background-color: rgba(230,120,0,0.08); }
  &.row-due-soon  { background-color: rgba(245,200,0,0.07); }

  // Active filter highlight
  &.row-filtered  { outline: 2px solid; outline-offset: -2px; }

  // Scroll-to animation
  &.row-highlight { animation: highlight-pulse 0.8s ease; }
}
```

---

### Anti-patterns cần tránh

- **KHÔNG** dùng `toISOString()` để lấy ngày hôm nay — UTC trap, lệch múi giờ
- **KHÔNG** tô Phase/Milestone — `EXCLUDED_TYPES` constant kiểm soát, không check ad-hoc trong component
- **KHÔNG** đặt `activeDeadlineFilter` state trong `DeadlineAlertBannerComponent` — state ở parent (`ProjectDetailComponent`)
- **KHÔNG** inject `DeadlineAlertService` trực tiếp trong Bryntum callback — closure capture qua arrow function
- **KHÔNG** bỏ `renderData.cls.remove(...)` trước add — gây class stale khi Bryntum recycle DOM
- **KHÔNG** tạo NgRx action/selector mới cho deadline state — pure computation từ existing `tasks$`

---

### Known Limitations (document, không fix trong story này)

- **Store staleness**: Nếu PM để tab mở cả ngày không refresh, deadline status không update theo thời gian thực (store không tự poll). PM phải reload trang hoặc navigate đi rồi về. Acceptable cho MVP.
- **Phase/Milestone không có indicator**: Task con overdue nhưng Phase row vẫn màu bình thường. Sẽ fix ở Story 7-4 với tree aggregation.
- **Gantt filter**: Banner trên Gantt không có filter/highlight interaction (chỉ show/hide). Interactive filter chỉ ở Project Detail task tree.

---

### Extensibility cho Story 7-4 (Phase/Milestone aggregation)

`EXCLUDED_TYPES` constant: Story 7-4 remove 'Phase' và 'Milestone' khỏi array này, thêm `computeParentStatus(tasks, today)` method vào `DeadlineAlertService`. Interface `DeadlineSummary` không đổi — 7-4 chỉ add thêm, không refactor.

---

### Dashboard follow-up (Out of scope)

Sau 7-3, story tiếp theo (7-4 hoặc 8-1) implement:
- BE: `GET /api/v1/projects/summary` → per-project `{ overdueCount, dueTodayCount, dueSoonCount, overloadCount, progressPct }`
- FE: Project list (`/projects`) thêm badge per card
- Reuse `DeadlineAlertService.getDeadlineStatus()` làm reference spec cho BE

---

## Completion Notes

*(Điền khi dev hoàn thành)*

## Files Created/Modified

**Frontend — Mới:**
- `frontend/project-management-web/src/app/features/projects/services/deadline-alert.service.ts`
- `frontend/project-management-web/src/app/features/projects/services/deadline-alert.service.spec.ts`
- `frontend/project-management-web/src/app/features/projects/components/deadline-alert-banner/deadline-alert-banner.ts`
- `frontend/project-management-web/src/app/features/projects/components/deadline-alert-banner/deadline-alert-banner.html`
- `frontend/project-management-web/src/app/features/projects/components/deadline-alert-banner/deadline-alert-banner.scss`
- `frontend/project-management-web/src/app/features/projects/components/deadline-alert-banner/deadline-alert-banner.spec.ts`

**Frontend — Sửa:**
- `frontend/project-management-web/src/app/features/projects/components/project-detail/project-detail.ts`
- `frontend/project-management-web/src/app/features/projects/components/project-detail/project-detail.html`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.ts`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.html`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.spec.ts`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.scss`
- `frontend/project-management-web/src/app/features/projects/components/gantt/[gantt-component].ts` *(tìm file thực tế từ story 1-5)*
- `frontend/project-management-web/src/app/features/projects/components/gantt/[gantt-component].scss`
