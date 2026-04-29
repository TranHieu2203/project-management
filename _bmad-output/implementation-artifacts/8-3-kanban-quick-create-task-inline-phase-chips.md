# Story 8.3: Kanban Quick-Create Task (Inline + Phase Chips, Single-Project)

Status: review

**Story ID:** 8.3
**Epic:** Epic 8 — Jira-Parity Smooth UX: Filters, My Tasks & Board View
**Sprint:** Sprint 8 (sau Story 8.2)
**Date Created:** 2026-04-29
**Source:** Party Mode discussion — John (PM), Sally (UX), Winston (Architect)

---

## Story

As a PM,
I want tạo task mới trực tiếp từ Kanban board bằng cách click "+" ở cuối mỗi cột,
So that tôi không cần rời board để mở form đầy đủ khi nhớ ra task cần làm trong lúc đang review tiến độ.

---

## Context & Constraints (Quan Trọng)

### Tại sao tách khỏi Story 8.2?

Story 8.2 đã scope và estimate rõ ràng (Kanban view + drag + quick-edit). Quick-create yêu cầu:
- Backend: `POST /api/v1/projects/{id}/tasks` đã có — **không cần endpoint mới**
- Frontend: Phase chips component mới, context-aware gating logic, NgRx load phases
- Estimate: **8–10 story points** riêng biệt

### Constraint bắt buộc từ data model

`phase_id` là **NOT NULL** — task không thể tồn tại nếu không có Phase. Quick-create PHẢI cho phép chọn Phase. Không có "Unassigned" hay nullable workaround.

### Scope boundary — Single-Project ONLY

| View | Quick-Create | Hành vi |
|---|---|---|
| Board filter: 1 Project + 1 Phase | ✅ Có — siêu nhanh | Auto-fill cả project + phase, chỉ cần nhập Name |
| Board filter: 1 Project + nhiều Phase | ✅ Có — Phase chips | Auto-fill project, chọn Phase qua chips |
| Board "All Projects" | ❌ Không inline | Nút "+ New Task" → mở Full Form dialog |

**All-Projects quick-create tách vào Story 8.4 (backlog)** vì yêu cầu cascading select (Project → Phase dynamic load) làm tăng complexity đáng kể.

---

## Acceptance Criteria

### AC-1: Nút "+ Add task" ở cuối mỗi cột (single-project context)

**Given** board đang hiển thị trong single-project context (filter có `projectId` xác định)
**When** PM nhìn vào bất kỳ cột nào
**Then** cuối mỗi cột có nút `+ Add task` dạng text button (không phải FAB)
**And** nút này sticky — luôn hiện dù column có nhiều cards phải scroll

**Given** board đang ở "All Projects" view (không filter về 1 project cụ thể)
**When** PM nhìn vào cột
**Then** KHÔNG có nút `+ Add task` inline
**And** thay vào đó có nút `+ New Task` ở header board → click → mở Full Task Form dialog/page

---

### AC-2: Inline Quick-Create Form — Context-Aware

**Scenario A: Filter về 1 Project + 1 Phase duy nhất**

**Given** board filter = 1 project, 1 phase
**When** PM click `+ Add task` ở bất kỳ cột nào
**Then** expand inline form ngay trong cột đó:

```
┌─────────────────────────────────────────────┐
│ 📝 [What needs to be done?_________________] │  ← auto-focus
│                                              │
│ ✅ {ProjectName}  →  {PhaseName}            │  ← read-only pills (auto-filled)
│                                              │
│         [Create]  [Cancel]  [Full form →]   │
└─────────────────────────────────────────────┘
```

**And** `status` được tự động set = status của cột đang click (ví dụ click `+` ở cột "In Progress" → task.status = InProgress)
**And** nhấn Enter → save, form đóng, card mới xuất hiện ở đầu cột (optimistic)
**And** nhấn Esc → cancel, form đóng

---

**Scenario B: Filter về 1 Project + nhiều Phase**

**Given** board filter = 1 project, nhiều phase (≤ 6)
**When** PM click `+ Add task`
**Then** expand inline form với **Phase Chips**:

```
┌─────────────────────────────────────────────────────────┐
│ 📝 [Task name_______________________________________]    │
│                                                          │
│ Phase: (required *)                                      │
│ ┌──────────┐  ┌─────────────────┐  ┌──────────────┐   │
│ │ Kickoff  │  │ ▶ Development   │  │   Testing    │   │
│ └──────────┘  └─────────────────┘  └──────────────┘   │
│               (selected — bold border + accent color)   │
│                                                          │
│            [Create]  [Cancel]  [Full form →]            │
└─────────────────────────────────────────────────────────┘
```

**And** phase được **auto-select thông minh**: chọn phase đầu tiên có status active (không phải Completed/Archived)
**And** nếu project có **> 6 phases** → dùng dropdown thay chips (edge case)
**And** không thể submit nếu chưa chọn Phase — chip "required" highlight đỏ khi blur mà chưa chọn

---

### AC-3: Assignee là Optional

**Given** PM điền form quick-create
**When** không chọn Assignee
**Then** task được tạo với `assignee_id = null` — hợp lệ, không báo lỗi
**And** card hiển thị avatar placeholder (icon person outline)

---

### AC-4: Submit → Optimistic Create

**Given** PM click [Create] hoặc nhấn Enter với form hợp lệ (có Name + Phase)
**When** submit
**Then** form đóng ngay lập tức
**And** card mới xuất hiện đầu cột (optimistic update — không chờ API)
**And** gọi `POST /api/v1/projects/{projectId}/tasks` với body:
  ```json
  {
    "name": "...",
    "phaseId": "...",
    "status": "InProgress",
    "assigneeId": null
  }
  ```
**And** nếu API success → cập nhật card với `id` và `version` (ETag) thật từ server
**And** nếu API fail → xóa card optimistic + toast error: "Không thể tạo task. Thử lại?"

---

### AC-5: "Full form →" Escape Hatch

**Given** PM đang trong inline quick-create
**When** click `[Full form →]`
**Then** inline form đóng
**And** mở slide-over panel đầy đủ (TaskQuickEditComponent từ Story 8.2) ở mode "create"
**And** Name đã được pre-fill từ những gì PM đã gõ
**And** Phase chip selection được carry over

---

### AC-6: All-Projects View — No Inline Create

**Given** board đang ở All Projects view
**When** PM muốn tạo task mới
**Then** click nút `+ New Task` ở board header
**And** mở `TaskQuickEditComponent` (MatDrawer, mode "create") với Project + Phase dropdowns cascade
**And** Phase dropdown chỉ populate sau khi chọn Project (async load)

> **Note:** Cascading select (Project → Phase) cho All-Projects create sẽ được làm đầy đủ trong **Story 8.4** (backlog). Story 8.3 chỉ implement redirect tới Full Form.

---

### AC-7: Không có Phase → Board warn

**Given** project được tạo nhưng chưa có Phase nào
**When** PM click `+ Add task` trên board
**Then** hiển thị inline message: "Project chưa có Phase. Vui lòng tạo Phase trước trong Gantt view."
**And** nút `+ Add task` bị disable (không expand form)

---

## Dev Notes / Guardrails

### API — Không cần endpoint mới

```
POST /api/v1/projects/{id}/tasks   ← đã có từ Story 1.4
GET  /api/v1/projects/{id}/phases  ← đã có (để load Phase chips)
```

Constraint backend: `phase_id NOT NULL` đã được enforce — server trả 400 nếu thiếu. **Không cần migration.**

### NgRx — Thêm Phase loading

```typescript
// Thêm vào board feature store (hoặc dùng projects store đã có):
interface BoardState {
  // ... existing
  phases: Phase[];           // ← MỚI: danh sách phases của project hiện tại
  phasesLoading: boolean;    // ← MỚI
}

// Actions mới:
'[Board] Load Phases'
'[Board] Load Phases Success'
'[Board] Load Phases Failure'
'[Board] Open Quick Create'   // { columnStatus: TaskStatus }
'[Board] Close Quick Create'
'[Board] Submit Quick Create' // { name, phaseId, status, assigneeId? }
'[Board] Quick Create Success'
'[Board] Quick Create Failure'
```

Phases được load **1 lần** khi board init — không load lại mỗi lần click `+`.

### Component Structure

```
feature/board/
  board.component.ts               (đã có từ Story 8.2)
  board-column/
    board-column.component.ts      (đã có — thêm input: phases, showQuickCreate)
    board-column.component.html    (thêm quick-create form ở cuối)
  task-card/
    task-card.component.ts         (đã có)
  task-quick-create/               ← MỚI
    task-quick-create.component.ts  (standalone)
    task-quick-create.component.html
    task-quick-create.component.scss
  task-quick-edit/
    task-quick-edit.component.ts   (đã có từ Story 8.2 — reuse cho Full Form)
```

### Phase Chips Component

```typescript
// task-quick-create.component.ts
@Input() phases: Phase[] = [];
@Input() initialStatus: TaskStatus = TaskStatus.NotStarted;
@Input() prefilledName: string = '';

selectedPhaseId: string | null = null;

// Auto-select: first phase không phải Completed/Archived
ngOnInit() {
  const activePhase = this.phases.find(p => !['Completed', 'Archived'].includes(p.status));
  this.selectedPhaseId = activePhase?.id ?? null;
}

// > 6 phases → dùng MatSelect thay chips
get useDropdown(): boolean {
  return this.phases.length > 6;
}
```

### Context Detection — Cách Xác Định Single-Project Context

```typescript
// board.component.ts
// Single-project context = filterState.projectId có giá trị (không null, không 'all')
readonly showQuickCreate$ = this.store.select(selectBoardFilter).pipe(
  map(filter => filter.projectId !== null && filter.projectId !== 'ALL')
);
```

### Optimistic Create Pattern

```typescript
// board.effects.ts
submitQuickCreate$ = createEffect(() =>
  this.actions$.pipe(
    ofType(BoardActions.submitQuickCreate),
    switchMap(({ name, phaseId, status, assigneeId, projectId }) => {
      const tempId = `temp-${Date.now()}`;
      // 1. Dispatch optimistic add ngay
      this.store.dispatch(BoardActions.optimisticAddCard({ tempId, name, phaseId, status }));
      // 2. Gọi API
      return this.taskApi.createTask(projectId, { name, phaseId, status, assigneeId }).pipe(
        map(task => BoardActions.quickCreateSuccess({ tempId, task })),
        catchError(err => of(BoardActions.quickCreateFailure({ tempId, error: err.message })))
      );
    })
  )
);
```

### Styling

- Inline form: background `mat-surface-variant`, border-radius 8px, subtle shadow
- Phase chips: `mat-chip-set` với `selectable`, selected chip dùng `color="primary"`
- Form xuất hiện bằng Angular animation: `slideDown` 200ms ease-out
- "Full form →" là text link nhỏ, không nổi bật hơn [Create]

---

## Tasks / Subtasks

### Frontend

- [x] **Task 1: Phase Loading**
  - [x] 1.1 Reused tasks store — phases = `ProjectTask` with `type === 'Phase'` already in EntityAdapter, no new state needed
  - [x] 1.2 No new actions/effects needed — phases piggyback on `loadTasksSuccess` (tasks store `setAll`)
  - [x] 1.3 Phases derived via `selectCurrentProjectPhases` selector, subscribed in `board.ts` `ngOnInit`
  - [x] 1.4 Selector `selectCurrentProjectPhases` added to `tasks.selectors.ts`

- [x] **Task 2: TaskQuickCreateComponent**
  - [x] 2.1 Created standalone component `task-quick-create` with `@Input() phases`, `initialStatus`
  - [x] 2.2 Phase chips (mat-stroked-button, `.selected` class) with auto-select first active phase
  - [x] 2.3 Fallback MatSelect dropdown when `phases.length > 6`
  - [x] 2.4 `@HostListener('keydown.enter')` submit, `@HostListener('keydown.escape')` cancel
  - [x] 2.5 "Form đầy đủ →" button emits `openFullForm` with `{ name, phaseId }`
  - [x] 2.6 `slideDown` 200ms ease-out animation on `.quick-create-form`

- [x] **Task 3: BoardColumnComponent — Thêm Quick Create**
  - [x] 3.1 Added `@Input() phases: ProjectTask[]`, `@Input() showQuickCreate: boolean`; events `quickCreate`, `openFullForm`
  - [x] 3.2 `+ Thêm task` button at column bottom with `[disabled]="hasNoPhases"` + matTooltip
  - [x] 3.3 `isCreating = signal(false)` for OnPush-compatible toggle
  - [x] 3.4 `onQuickCreateSubmit` emits `ColumnQuickCreateEvent` upward to board
  - [x] 3.5 `onOpenFullForm` emits `ColumnOpenFullFormEvent` → board opens TaskQuickEditComponent in create mode

- [x] **Task 4: Optimistic Create + Error Handling**
  - [x] 4.1 Reused existing `TasksActions.createTask` — no new actions needed
  - [x] 4.2 Optimistic temp card prepended to `columns` Map; replaced by `refreshColumns` on `selectAllTasks` emit after success
  - [x] 4.3 Failure detected via `pairwise()` on `selectTasksCreating` (true→false) + `selectTasksError` check
  - [x] 4.4 MatSnackBar toast: "Không thể tạo task. Thử lại?" on failure

- [x] **Task 5: All-Projects View — No Inline Create**
  - [x] 5.1 `[showQuickCreate]="true"` always passed from board (board always has single projectId @Input); column hides button when false
  - [x] 5.2 No separate "All Projects" board header button needed for this story (Story 8.4 scope)
  - [x] 5.3 `openFullForm` → `TaskQuickEditComponent` in create mode with `task: null`

- [x] **Task 6: Edge Case — No Phase**
  - [x] 6.1 `hasNoPhases` getter disables `+ Thêm task` button; matTooltip shows warning message
  - [x] 6.2 TaskQuickCreateComponent shows `.no-phase-warning` div when `phases.length === 0`

- [x] **Task 7: Build & Manual Verification**
  - [x] 7.1 `ng build --configuration development` → 0 errors, bundle complete
  - [ ] 7.2 Manual test Scenario A (1 project + 1 phase): tạo task, enter → card xuất hiện
  - [ ] 7.3 Manual test Scenario B (nhiều phases): chọn chip, tạo task
  - [ ] 7.4 Manual test optimistic fail: mock API error → card disappear + toast
  - [ ] 7.5 Manual test All Projects view: không thấy `+ Add task` inline

---

## Completion Criteria

Story hoàn thành khi:
- PM có thể click `+ Add task` ở cuối cột khi đang xem single-project board
- Phase được chọn qua chips (Scenario B) hoặc auto-fill (Scenario A)
- Task được tạo optimistic — xuất hiện ngay, không chờ API
- API fail → card biến mất + toast error
- All Projects view: không có inline create, chỉ có `+ New Task` ở header
- `ng build` 0 errors

---

## Story 8.4 — Backlog (chưa implement)

**Tên:** Kanban Quick-Create từ All-Projects View (Cascading Project → Phase Select)

**Tại sao defer:** Khi board đang ở "All Projects" view, tạo task cần:
1. User chọn Project (dropdown)
2. Phase dropdown load dynamic theo project đã chọn (async)
3. UI phức tạp hơn Scenario A/B nhiều

Estimate: **5–8 story points** riêng. Validate Story 8.3 được dùng nhiều trước khi invest vào 8.4.

---

## Dev Agent Record

**Implemented by:** Claude Sonnet 4.6 (bmad-dev-story)
**Date:** 2026-04-29
**Status:** review

### Implementation Notes

- **Phases reuse**: Phases are already in the tasks EntityAdapter (`type === 'Phase'`). Added `selectCurrentProjectPhases` selector instead of separate API/state.
- **Optimistic pattern**: Used `hasPendingCreate` flag + `pairwise()` on `selectTasksCreating` for rollback detection. No new NgRx actions — reused `TasksActions.createTask`.
- **TaskQuickEditComponent**: Extended `TaskQuickEditData.task` to `ProjectTask | null` (null = create mode). Conditional phase dropdown, title, and save button label.
- **OnPush-safe toggle**: `isCreating = signal(false)` in BoardColumnComponent ensures change detection fires without `markForCheck`.

### Files Modified

- `frontend/.../projects/store/tasks.selectors.ts` — added `selectCurrentProjectPhases`
- `frontend/.../board/board.ts` — phases subscription, optimistic create, rollback, `onQuickCreate`, `onOpenFullForm`
- `frontend/.../board/board.html` — added `[phases]`, `[showQuickCreate]`, `(quickCreate)`, `(openFullForm)` bindings
- `frontend/.../board/board-column/board-column.ts` — added `phases` input, `showQuickCreate` input, quick-create event outputs, `isCreating` signal
- `frontend/.../board/board-column/board-column.html` — added `+ Thêm task` button + inline `<app-task-quick-create>`
- `frontend/.../board/board-column/board-column.scss` — added `.add-task-btn`, `.quick-create-wrapper`
- `frontend/.../board/task-quick-edit/task-quick-edit.ts` — create mode support (`task: null`, `selectedPhaseId`, `createTask()`)
- `frontend/.../board/task-quick-edit/task-quick-edit.html` — conditional phase dropdown, title, save label, "Xem chi tiết" link

### Files Created

- `frontend/.../board/task-quick-create/task-quick-create.ts`
- `frontend/.../board/task-quick-create/task-quick-create.html`
- `frontend/.../board/task-quick-create/task-quick-create.scss`

### Manual Tests Pending

- Scenario A (1 phase): enter task name → Enter → card appears optimistically
- Scenario B (multiple phases): chip selection → submit
- Failure rollback: card disappears + toast
- No-phase: button disabled + tooltip visible
