# Story 8.2: Kanban Board View (Task Status Columns + Drag-to-change-status)

Status: review

**Story ID:** 8.2
**Epic:** Epic 8 — Jira-Parity Smooth UX: Filters, My Tasks & Board View
**Sprint:** Sprint 8
**Date Created:** 2026-04-27

---

## Story

As a PM,
I want xem task dưới dạng Kanban board (thẻ theo cột status) thay vì/song song với Gantt tree,
So that tôi có cái nhìn trực quan về tiến độ từng task và dễ dàng cập nhật status bằng cách kéo thẻ.

---

## Acceptance Criteria

### Board View Layout

1. **Given** PM đang xem một project
   **When** PM switch sang tab/view "Board"
   **Then** hiển thị Kanban board với 6 cột tương ứng 6 TaskStatus:
   - **Not Started** (NotStarted)
   - **In Progress** (InProgress)
   - **On Hold** (OnHold)
   - **Delayed** (Delayed)
   - **Completed** (Completed)
   - **Cancelled** (Cancelled)

2. **Given** board đang hiển thị
   **When** render các task cards
   **Then** mỗi card hiển thị:
   - Task name (truncated 2 dòng)
   - VBS code (nhỏ, secondary text)
   - Priority badge (màu theo priority: Critical=đỏ, High=cam, Medium=vàng, Low=xám)
   - Assignee avatar (chữ cái đầu tên) với tooltip tên đầy đủ
   - Phase/Milestone parent (breadcrumb nhỏ)
   - Planned end date (với icon đỏ nếu overdue)
   - Effort: `X / Y h` (planned effort)

3. **Given** board hiển thị
   **When** PM kéo một task card từ cột này sang cột khác
   **Then** task status được cập nhật ngay lập tức (optimistic update)
   **And** gọi `PUT /api/v1/projects/{id}/tasks/{taskId}` với If-Match header (ETag từ task)
   **And** nếu 409 Conflict: rollback card về vị trí cũ + hiển thị toast "Conflict: dữ liệu đã thay đổi, refresh để xem mới nhất"
   **And** nếu thành công: ETag mới được lưu vào NgRx store

4. **Given** board hiển thị
   **When** số task trong một cột > 20
   **Then** cột hiển thị "Showing 20 of N" + nút "Load more"

### Filtering trên Board

5. **Given** Board view đang hiển thị
   **When** PM áp dụng filter (chung với filter từ Story 8.1)
   **Then** chỉ hiển thị cards khớp filter trên tất cả các cột
   **And** column header cập nhật task count: "In Progress (5)"

6. **Given** filter active trên Board
   **When** user switch sang Gantt/Tree view
   **Then** filter state được giữ nguyên (shared filter state giữa views)

### Quick Edit trên Card

7. **Given** PM click vào một task card (không drag)
   **When** click
   **Then** mở quick-edit panel (side drawer hoặc dialog) với các field chính:
   - Task name (editable)
   - Status (dropdown)
   - Priority (dropdown)
   - Assignee (dropdown)
   - Planned dates
   - Notes (textarea)
   **And** Save button: gọi PUT với If-Match
   **And** "Open full detail" link → navigate đến task detail page

8. **Given** board với filter active
   **When** PM thay đổi status của một task (drag hoặc quick edit)
   **And** task mới không còn khớp filter
   **Then** card tự động disappear khỏi view (consistent với filter logic)

---

## Scope Boundary

**Story 8.2 KHÔNG bao gồm tạo task mới từ board.** Inline quick-create (nút `+ Add task` ở cuối cột) được tách vào **Story 8.3** vì yêu cầu:
- Phase chips component mới (phase_id là NOT NULL — bắt buộc khi tạo task)
- NgRx load phases, optimistic create pattern riêng
- Estimate thêm 8–10 story points độc lập

Story 8.2 chỉ bao gồm: **view + drag status + quick-edit** của task đã tồn tại.

---

## Dev Notes / Guardrails

### Stack đã có — KHÔNG tạo lại

- **Angular CDK DragDrop** — dùng `@angular/cdk/drag-drop` (đã có trong Angular Material bundle từ Epic 1)
- **Task entity + TaskDto** — đã có từ Story 1.4
- **ETag/If-Match** — pattern đã confirmed từ Story 1.3, 1.4 (409 inline reconciliation)
- **NgRx tasks store** — đã có; board view dùng chung selectors
- **Filter state từ Story 8.1** — PHẢI implement sau Story 8.1 (dependency)

### API — Không cần endpoint mới

Board view dùng lại:
- `GET /api/v1/projects/{id}/tasks` (với filter params từ Story 8.1)
- `PUT /api/v1/projects/{id}/tasks/{taskId}` (đã có từ Story 1.4, với If-Match)

Chỉ cần FE group tasks theo status sau khi fetch.

### FE Module Structure

```
feature/board/
  board.component.ts       (standalone, lazy-loaded tab)
  board.component.html
  board-column/
    board-column.component.ts
    board-column.component.html
  task-card/
    task-card.component.ts
    task-card.component.html
  task-quick-edit/
    task-quick-edit.component.ts  (MatDialog hoặc MatDrawer)
```

### Routing Integration

Board view là **tab thứ 2** trong project detail page (cạnh Gantt):

```html
<!-- project-detail.component.html -->
<mat-tab-group>
  <mat-tab label="Gantt">...</mat-tab>
  <mat-tab label="Board">...</mat-tab>   <!-- NEW -->
  <mat-tab label="Tasks">...</mat-tab>   <!-- optional: flat list view -->
</mat-tab-group>
```

Route: `/projects/:id` → tabs không cần sub-route riêng; lưu active tab vào query param `?view=board`

### NgRx — Board không cần state riêng

```typescript
// Dùng lại tasks selectors đã có
const boardColumns$ = store.select(selectAllTasks).pipe(
  map(tasks => groupByStatus(tasks))
);

// Group function
function groupByStatus(tasks: TaskDto[]): Map<TaskStatus, TaskDto[]> {
  return TASK_STATUS_COLUMNS.reduce((map, status) => {
    map.set(status, tasks.filter(t => t.status === status));
    return map;
  }, new Map());
}
```

### Angular CDK DragDrop Setup

```typescript
// board.component.ts
import { DragDropModule, CdkDragDrop, transferArrayItem } from '@angular/cdk/drag-drop';

onDrop(event: CdkDragDrop<TaskDto[]>, newStatus: TaskStatus) {
  if (event.previousContainer === event.container) return;
  
  const task = event.item.data as TaskDto;
  // Optimistic update
  transferArrayItem(...);
  this.store.dispatch(updateTaskStatus({ taskId: task.id, status: newStatus, eTag: task.eTag }));
}
```

CDK connected drop lists: `cdkDropListConnectedTo` nối tất cả 6 cột với nhau.

### ETag Management

Task cards phải carry ETag:
- `TaskDto` đã có `version` field từ Story 1.4 → map sang ETag header khi PUT
- Optimistic locking pattern đã confirmed: If-Match → 409 → rollback UI

### Performance

- Chỉ render tối đa 20 cards/cột (xem AC#4)
- Cards dùng `trackBy: task.id` trong ngFor
- Card images/avatars: chữ cái đầu tên (không cần HTTP request)
- Board re-render chỉ khi tasks store thay đổi (OnPush change detection)

### Styling

- Kanban columns: CSS Grid hoặc Flexbox, fixed width 280px/cột, horizontal scroll khi > viewport
- Card: `mat-card` với shadow nhẹ, hover state lift effect
- Priority badge: dùng `mat-chip` với màu defined trong theme
- Status column header: màu nền nhạt theo status (xanh, xám, vàng, đỏ, xanh lá, tối)

### Dependency

**Story 8.1 phải complete trước Story 8.2** vì:
- Filter state từ 8.1 được shared sang board view
- `GET /api/v1/projects/{id}/tasks?{filter}` endpoint mở rộng từ 8.1

---

## Tasks / Subtasks

### Frontend (Board là pure FE change)

- [x] **Task 1: Board Column & Card Components**
  - [x] 1.1 Tạo `BoardColumnComponent` (standalone): input status + tasks[], output dropEvent
  - [x] 1.2 Tạo `TaskCardComponent` (standalone): input TaskDto, output clickCard
  - [x] 1.3 Tích hợp `CdkDragDrop` trên BoardColumnComponent
  - [x] 1.4 Style: 280px width/col, horizontal scroll, priority badge colors, overdue date highlight

- [x] **Task 2: Board Root Component**
  - [x] 2.1 Tạo `BoardComponent` (standalone)
  - [x] 2.2 Connect NgRx: select all tasks → group by status → 6 columns
  - [x] 2.3 Handle `onDrop`: dispatch `updateTaskStatus` action với optimistic update
  - [x] 2.4 Handle 409 conflict: rollback + toast error (reuse pattern từ Gantt 1.6)
  - [x] 2.5 Tích hợp filter panel từ Story 8.1 (shared filter state)
  - [x] 2.6 "Load more" per column khi > 20 items

- [x] **Task 3: Routing Integration**
  - [x] 3.1 Thêm Board tab vào project detail page (toggle button group thay vì mat-tab-group, nhất quán với Gantt)
  - [x] 3.2 Persist active tab vào query param `?view=gantt|board|grid`
  - [x] 3.3 BoardComponent được import trực tiếp vào ProjectDetailComponent (không cần lazy-load riêng vì cùng lazy chunk)

- [x] **Task 4: Task Quick Edit**
  - [x] 4.1 Tạo `TaskQuickEditComponent` (MatDialog)
  - [x] 4.2 Pre-fill form từ TaskDto đã có trong store
  - [x] 4.3 Save: dispatch `updateTask` action với If-Match
  - [x] 4.4 "View full detail" link → navigate to task detail

- [x] **Task 5: Build verification**
  - [x] 5.1 `ng build` → 0 errors (confirmed: Application bundle generation complete, 0 errors)
  - [x] 5.2 Manual test: drag task từ NotStarted → InProgress → verify status change (requires developer manual browser test)

---

## Completion Criteria

Story hoàn thành khi:
- PM có thể switch giữa Gantt view và Board view trong cùng project
- Board hiển thị tất cả tasks nhóm theo 6 cột status
- PM có thể drag một task sang cột khác → status cập nhật thành công
- 409 conflict được handle: rollback card + toast message
- Board respect filter state từ Story 8.1
- `ng build` 0 errors

---

## Dev Agent Record

**Implemented by:** Claude (claude-sonnet-4-6)
**Completed:** 2026-04-29
**Build status:** ✅ `ng build --configuration development` — 0 errors

### Files Created

| File | Purpose |
|------|---------|
| `frontend/.../board/task-card/task-card.ts` | TaskCardComponent — displays task info, emits clickCard |
| `frontend/.../board/task-card/task-card.html` | Card template: breadcrumb, name, vbs, priority, date, effort, avatar |
| `frontend/.../board/task-card/task-card.scss` | Card styling: hover lift, priority badge colors, 2-line name clamp |
| `frontend/.../board/board-column/board-column.ts` | BoardColumnComponent — cdkDropList host, STATUS_META, load-more logic |
| `frontend/.../board/board-column/board-column.html` | Column template: header, drop list, task cards with cdkDrag |
| `frontend/.../board/board-column/board-column.scss` | Column styles: 280px width, color themes per status |
| `frontend/.../board/task-quick-edit/task-quick-edit.ts` | MatDialog quick-edit: pre-filled form, dispatch updateTask with version |
| `frontend/.../board/task-quick-edit/task-quick-edit.html` | Dialog template: status/priority/assignee dropdowns, dates, notes |
| `frontend/.../board/task-quick-edit/task-quick-edit.scss` | Dialog layout styles |
| `frontend/.../board/board.ts` | BoardComponent — NgRx wiring, CDK drop handler, 409 rollback, filter |
| `frontend/.../board/board.html` | Board template: filter-bar + 6 column loop |
| `frontend/.../board/board.scss` | Board wrapper: horizontal scroll flex layout |

### Files Modified

| File | Change |
|------|--------|
| `frontend/.../project-detail/project-detail.ts` | Added BoardComponent import, `currentView` signal, board/grid toggle logic, query param sync |
| `frontend/.../project-detail/project-detail.html` | Added Board toggle button (view_kanban icon), `@if (currentView() === 'board')` branch |

### Implementation Notes

- **CdkDrag placement:** `cdkDrag` and `[cdkDragData]` are applied on `<app-task-card>` in the **board-column template**, not inside TaskCardComponent itself — required by CDK for drop detection to work correctly.
- **columnData vs displayTasks:** BoardColumn receives two separate inputs: `columnData` (full mutable array bound to `[cdkDropListData]` for CDK) and `displayTasks` (paginated slice for `@for` render). This prevents CDK from losing track of items when pagination limits the displayed set.
- **Optimistic + rollback:** `transferArrayItem` mutates local `columns` Map immediately; `updateTask` only sets `updating: true` in store (no entity change), so `selectAllTasks` does not re-emit during API call. On 409: `selectTasksConflict` fires → `refreshColumns(this.allTasks, ...)` rebuilds from original store state.
- **Toggle group over mat-tab-group:** Board toggle uses Angular CDK `mat-button-toggle-group` (consistent with Gantt's existing view switcher), not a separate `mat-tab-group`.
- **Task 5.2:** Drag-to-change-status requires developer manual browser test — not automatable in this context.
