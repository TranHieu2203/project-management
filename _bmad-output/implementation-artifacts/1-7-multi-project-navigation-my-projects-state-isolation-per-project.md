# Story 1.7: Multi-project Navigation (My Projects) + State Isolation per Project

Status: review

**Story ID:** 1.7
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 2
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want chuyển đổi nhanh giữa các dự án và không bị lẫn dữ liệu,
So that tôi có thể quản lý nhiều dự án trong một giao diện thống nhất thay Excel.

## Acceptance Criteria

1. **Given** user đã đăng nhập
   **When** mở trang "My Projects" (`/projects`)
   **Then** hiển thị danh sách projects (membership-only) với nút "Mở Gantt" trực tiếp trên mỗi card

2. **Given** user đang ở Gantt của Project A
   **When** user chuyển sang Project B (qua "My Projects" hoặc route trực tiếp)
   **Then** UI reset state Tasks và Gantt của Project A trước khi tải dữ liệu Project B
   **And** không có dữ liệu Project A xuất hiện trong UI của Project B

3. **Given** user truy cập trực tiếp URL `/projects/{projectId}/gantt`
   **When** `projectId` không thuộc membership của user (backend trả 404)
   **Then** UI hiển thị trang/banner fallback "Không tìm thấy dự án" với nút quay về My Projects

## Tasks / Subtasks

- [x] **Task 1: My Projects UX Enhancement (FE)**
  - [x] 1.1 Rename page title từ "Danh Sách Dự Án" → "My Projects" + subtitle membership-only
  - [x] 1.2 Thêm nút "Gantt" (icon `timeline`, mat-raised-button color=primary) → routerLink `['/projects', project.id, 'gantt']`
  - [x] 1.3 Thêm nút "Chi tiết" (icon `folder_open`) để giữ lại link đến project-detail
  - [x] 1.4 Cải thiện card layout: bỏ routerLink trên card, dùng buttons riêng; empty-state icon

- [x] **Task 2: Tasks State Isolation (FE)**
  - [x] 2.1 Thêm action `'Clear Tasks': emptyProps()` vào `TasksActions`
  - [x] 2.2 Xử lý `clearTasks` trong `tasksReducer` → `tasksAdapter.removeAll({ ...initialState })`; export `initialState`
  - [x] 2.3 `ProjectDetailComponent.ngOnDestroy()` dispatch `TasksActions.clearTasks()`
  - [x] 2.4 Verified `GanttComponent.ngOnDestroy()` đã dispatch `GanttActions.clearGantt()` (đã có từ Story 1.5)

- [x] **Task 3: Gantt 404 Fallback (FE)**
  - [x] 3.1 `gantt.effects.ts` — 404 produces message "Dự án không tồn tại hoặc bạn không có quyền truy cập."
  - [x] 3.2 `gantt.html` — error banner có nút "Về My Projects" → routerLink `/projects`
  - [x] 3.3 State đã được clear: `loadGanttData` dispatch clears dirtyTasks + conflict; `clearGantt` on destroy

- [x] **Task 4: Tests (FE)**
  - [x] 4.1 Unit test tasksReducer: `clearTasks` → 6 assertions (entities rỗng, projectId null, error null, loading false, conflict null, idempotent)
  - [x] 4.2 Unit test tasksReducer: `loadTasks` + `loadTasksSuccess` — 2 thêm assertions; ganttReducer clearGantt tests vẫn pass (77 total)

---

## Dev Notes

### Những gì đã có sẵn (KHÔNG viết lại)

| File/Pattern | Trạng thái | Ghi chú |
|---|---|---|
| `ProjectListComponent` | ✅ Story 1.3 | Cần thêm Gantt button + rename title |
| `ProjectDetailComponent` | ✅ Story 1.4 | Cần thêm `ngOnDestroy` dispatch `clearTasks` |
| `GanttComponent.ngOnDestroy()` | ✅ Story 1.5 | Đã dispatch `clearGantt()` — không sửa |
| `ganttReducer: clearGantt` | ✅ Story 1.5 | Reset về initialState — không sửa |
| `TasksActions` (createActionGroup) | ✅ Story 1.4 | Thêm `'Clear Tasks': emptyProps()` |
| `tasksReducer` | ✅ Story 1.4 | Thêm `on(TasksActions.clearTasks, ...)` |
| `gantt.effects.ts loadGanttData$` | ✅ Story 1.5/1.6 | 404 đã đi vào `loadGanttDataFailure` |

---

### Task 2 Detail: clearTasks Action + Reducer

```typescript
// tasks.actions.ts — THÊM vào createActionGroup events:
'Clear Tasks': emptyProps(),

// tasks.reducer.ts — THÊM on():
on(TasksActions.clearTasks, () =>
  tasksAdapter.removeAll({ ...initialState })),
```

```typescript
// project-detail.ts — THÊM ngOnDestroy:
ngOnDestroy(): void {
  this.store.dispatch(TasksActions.clearTasks());
}
// Nhớ import OnDestroy, thêm vào implements
```

---

### Task 1 Detail: Project Card với Gantt Button

Project list card cần 2 action buttons riêng biệt:
- "Mở Gantt" → `/projects/:id/gantt` (icon: `timeline`, color: primary)
- "Chi tiết / Task" → `/projects/:id` (icon: `folder_open`)
- "Sửa" + "Xóa" giữ nguyên

Card KHÔNG còn routerLink trên toàn card (tránh nhầm route). Thay vào đó dùng buttons rõ ràng.

```html
<!-- Trong mat-card-actions align="end" -->
<button mat-button [routerLink]="['/projects', project.id]" title="Chi tiết">
  <mat-icon>folder_open</mat-icon> Chi tiết
</button>
<button mat-raised-button color="primary"
  [routerLink]="['/projects', project.id, 'gantt']" title="Mở Gantt">
  <mat-icon>timeline</mat-icon> Gantt
</button>
<button mat-icon-button (click)="openEditDialog(project, $event)" title="Chỉnh sửa">
  <mat-icon>edit</mat-icon>
</button>
<button mat-icon-button color="warn" (click)="deleteProject(project, $event)" title="Xóa">
  <mat-icon>delete</mat-icon>
</button>
```

---

### Task 3 Detail: Gantt 404 Handling

`loadGanttDataFailure` đã catch mọi lỗi kể cả 404. Cần phân biệt 404 vs lỗi khác để show đúng UI:

```typescript
// gantt.effects.ts — update catchError trong loadGanttData$:
catchError(err => {
  const msg = err?.status === 404
    ? 'Dự án không tồn tại hoặc bạn không có quyền truy cập.'
    : (err?.error?.detail ?? err?.message ?? 'Lỗi tải dữ liệu Gantt');
  return of(GanttActions.loadGanttDataFailure({ error: msg }));
})
```

```html
<!-- gantt.html — trong error block, thêm nút quay về khi là 404: -->
@if (error$ | async; as error) {
  <div class="gantt-error">
    <mat-icon>error_outline</mat-icon>
    <span>{{ error }}</span>
    <button mat-button routerLink="/projects">Về My Projects</button>
  </div>
}
```

Lưu ý: error hiện tại chỉ show text, không navigate tự động (tránh UX không tốt khi user đang xem error).

---

### Lỗi cần tránh

1. **Không dispatch clearTasks 2 lần**: `ProjectDetailComponent.ngOnDestroy` đủ — không thêm vào `loadProjects` effect hay app shell
2. **Không sửa `selectAllTasks`**: tasks của project khác sẽ không leak vì `loadTasks` dùng `setAll` (replace toàn bộ), clearTasks chỉ giải quyết trường hợp người dùng thấy dữ liệu cũ trong thời gian load
3. **Không remove routerLink trên toàn card nếu khó**: có thể giữ routerLink trên card nhưng thêm nút "Gantt" riêng ở actions
4. **`tasksAdapter.removeAll(initialState)` vs `getInitialState()`**: dùng `tasksAdapter.removeAll({ ...initialState })` để reset về full initial state kể cả các custom fields

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống — chưa implement)_

### Completion Notes List

- My Projects page đã rename title + subtitle; bỏ routerLink trên card, thêm buttons "Chi tiết" và "Gantt" riêng rõ ràng
- `clearTasks` action + reducer reset toàn bộ TasksState về initialState (entities empty, currentProjectId null, conflict null)
- `initialState` export từ tasks.reducer.ts để spec có thể import
- ProjectDetailComponent.ngOnDestroy dispatch clearTasks đảm bảo khi navigate khỏi project-detail không còn data cũ
- GanttComponent.ngOnDestroy đã dispatch clearGantt từ Story 1.5 — không cần sửa
- 404 error message được phân biệt rõ khỏi các lỗi khác trong loadGanttData$ effect

### File List

- `frontend/project-management-web/src/app/features/projects/components/project-list/project-list.html` — My Projects UX
- `frontend/project-management-web/src/app/features/projects/components/project-list/project-list.scss` — header, empty-state icon, remove cursor:pointer
- `frontend/project-management-web/src/app/features/projects/components/project-detail/project-detail.ts` — ngOnDestroy + clearTasks
- `frontend/project-management-web/src/app/features/projects/store/tasks.actions.ts` — clearTasks action
- `frontend/project-management-web/src/app/features/projects/store/tasks.reducer.ts` — clearTasks handler, export initialState
- `frontend/project-management-web/src/app/features/gantt/store/gantt.effects.ts` — 404 specific error message
- `frontend/project-management-web/src/app/features/gantt/components/gantt/gantt.html` — "Về My Projects" button in error banner
- `frontend/project-management-web/src/app/features/gantt/components/gantt/gantt.scss` — error span flex:1
- `frontend/project-management-web/src/app/features/projects/store/tasks.reducer.spec.ts` — 8 unit tests (new)
