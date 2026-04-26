# Story 1.10: Gantt Page UI Consistency + Left Panel Inline Editing

Status: review

**Story ID:** 1.10
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 3
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want trang Gantt có giao diện nhất quán với trang Project Detail và cho phép chỉnh sửa inline trực tiếp trên left panel,
So that trải nghiệm chỉnh sửa task trên Gantt mượt mà và đồng bộ với phần còn lại của ứng dụng.

## Acceptance Criteria

1. **Given** user mở trang Gantt (`/projects/{id}/gantt`)
   **When** trang render
   **Then** toolbar hiển thị tên project (từ projects store)
   **And** layout toolbar có cùng phong cách với trang Project Detail (back button, view toggle, thêm task button)

2. **Given** user nhìn vào Gantt left panel
   **When** hover vào ô tên task, status, ngày bắt đầu, ngày kết thúc, % hoàn thành
   **Then** ô đó highlight giống `editable-cell` pattern của `task-tree` (background blue-tinted, border hint)

3. **Given** user click vào ô tên task trong left panel
   **When** click
   **Then** ô chuyển thành `<input>` inline (giống `task-tree`)
   **And** Escape → hủy, Enter hoặc blur → commit và dispatch `GanttActions.markTaskDirty` với `newName`
   **And** task được đánh dấu dirty, dirty count tăng lên, nút "Lưu" xuất hiện

4. **Given** user click vào ô status trong left panel
   **When** click
   **Then** ô chuyển thành `<select>` với các options: NotStarted / InProgress / Completed / OnHold / Cancelled / Delayed
   **And** khi chọn xong (change/blur) → commit → dispatch `markTaskDirty` với `newStatus`

5. **Given** user click vào ô ngày BĐ hoặc KT trong left panel
   **When** click
   **Then** ô chuyển thành `<input type="date">` inline
   **And** blur/Enter → commit → dispatch `markTaskDirty` với `newPlannedStart`/`newPlannedEnd`
   **And** giá trị cũ được giữ nếu user Escape hoặc input không hợp lệ

6. **Given** user click vào ô % hoàn thành trong left panel
   **When** click
   **Then** ô chuyển thành `<input type="number" min="0" max="100">` inline
   **And** blur/Enter → commit → dispatch `markTaskDirty` với `newPercentComplete`

7. **Given** user bấm "Lưu" sau khi inline edit
   **When** effect `saveGanttEdits$` xử lý
   **Then** payload PUT gửi đúng `name`, `status`, `percentComplete` từ edit
   **And** sau khi save thành công, GanttTask trong store được cập nhật đúng (name, status, percentComplete, version mới)

8. **Given** user bấm "Hoàn tác"
   **When** dispatch `discardGanttEdits`
   **Then** ngoài clearing `dirtyTasks`, effect phải reload lại gantt data (dispatch `loadGanttData`) để reset tất cả task về giá trị server
   **And** dirty count về 0, task hiển thị giá trị gốc

9. **Given** user bấm nút Xóa trên một task trong left panel
   **When** click
   **Then** mở `ConfirmDialogComponent` (MatDialog) thay vì `window.confirm()`
   **And** chỉ xóa khi user xác nhận trong dialog

10. **Given** CSS của gantt-left-panel
    **When** dev xem code
    **Then** không còn màu hardcode (#f5f5f5, #e0e0e0, #e8f4fd) mà dùng CSS variables (`--table-header-bg`, `--border-color-light`, `--docker-blue-light`)

## Tasks / Subtasks

- [x] **Task 1: Extend GanttTaskEdit model** (AC: 3, 4, 5, 6, 7)
  - [x] 1.1 Trong `gantt.model.ts`: thêm 3 optional fields vào `GanttTaskEdit`: `newName?: string`, `newStatus?: string`, `newPercentComplete?: number`

- [x] **Task 2: Cập nhật gantt.reducer.ts** (AC: 3, 4, 5, 6, 7, 8)
  - [x] 2.1 Trong handler `markTaskDirty`: áp `newName ?? t.name`, `newStatus ?? t.status`, `newPercentComplete ?? t.percentComplete` lên task state (tương tự `plannedStart`/`plannedEnd` hiện tại)
  - [x] 2.2 Trong handler `saveGanttEditsSuccess`: cập nhật thêm `name`, `status`, `percentComplete` từ `updatedTasks` (cần thêm các field này vào `UpdatedTaskSummary` type)

- [x] **Task 3: Cập nhật gantt.effects.ts** (AC: 7, 8)
  - [x] 3.1 Trong `buildUpdatePayload`: dùng `edit.newName ?? original.name`, `edit.newStatus ?? original.status`, `edit.newPercentComplete !== undefined ? edit.newPercentComplete : (original.percentComplete ?? undefined)`
  - [x] 3.2 Trong `saveGanttEditsSuccess` result mapping: thêm `name: updated.name`, `status: updated.status`, `percentComplete: updated.percentComplete` vào object trả về
  - [x] 3.3 Thêm effect `discardGanttEdits$`: sau khi `discardGanttEdits` dispatch, đọc `ganttState.projectId` và dispatch thêm `loadGanttData({ projectId })` để reset task state về server values

- [x] **Task 4: Tạo ConfirmDialogComponent** (AC: 9)
  - [x] 4.1 Tạo file `shared/components/confirm-dialog/confirm-dialog.ts` (standalone, inline template)
  - [x] 4.2 Interface `ConfirmDialogData { message: string; confirmLabel?: string; cancelLabel?: string }`
  - [x] 4.3 Result: `true` (confirmed) | `false` / undefined (cancelled)
  - [x] 4.4 Style: tương tự `ConflictDialogComponent` — MatDialog title/content/actions pattern

- [x] **Task 5: Cập nhật gantt.ts (GanttComponent)** (AC: 1, 8, 9)
  - [x] 5.1 Thêm selector `selectProjectByGanttId` trong `gantt.selectors.ts`
  - [x] 5.2 Inject `Store` đã có sẵn, thêm `project$: Observable<Project | undefined> = this.store.select(selectProjectByGanttId)`
  - [x] 5.3 Trong toolbar HTML: hiển thị `{{ (project$ | async)?.name }}` làm title
  - [x] 5.4 Xử lý `onInlineEdit(event: GanttInlineEditEvent)`: dispatch `GanttActions.markTaskDirty` với task id, originalVersion, và các field mới
  - [x] 5.5 Replace `confirm()` trong `deleteGanttTask`: mở `ConfirmDialogComponent`, chỉ dispatch xóa khi `result === true`
  - [x] 5.6 Fix memory leak: `conflict$` subscription dùng `takeUntilDestroyed(this.destroyRef)`
  - [x] 5.7 Sau khi `onDiscard()`: effect sẽ tự reload (Task 3.3), không cần thêm logic trong component

- [x] **Task 6: Cập nhật gantt.html** (AC: 1)
  - [x] 6.1 Thêm hiển thị project name trong toolbar-left (sau view-toggle và sep)
  - [x] 6.2 Thêm `(inlineEdit)="onInlineEdit($event)"` vào `<app-gantt-left-panel>`

- [x] **Task 7: Cập nhật GanttLeftPanelComponent** (AC: 2, 3, 4, 5, 6)
  - [x] 7.1 Thêm type `GanttEditableField` và interface `GanttInlineEditEvent`
  - [x] 7.2 Thêm `@Output() inlineEdit = new EventEmitter<GanttInlineEditEvent>()`
  - [x] 7.3 Thêm state: `editingTaskId`, `editingField`, `editingValue`
  - [x] 7.4 Implement `startEdit(task, field, event)`
  - [x] 7.5 Implement `commitEdit(task)`: validate, emit, clear state, `cdr.markForCheck()`
  - [x] 7.6 Implement `cancelEdit()`: clear state, `cdr.markForCheck()`
  - [x] 7.7 Implement `onEditKeydown`: Enter → commit, Escape → cancel
  - [x] 7.8 Inject `ChangeDetectorRef`
  - [x] 7.9 `isEditing(taskId, field)` helper
  - [x] 7.10 `statusOptions` array và `statusLabel()` method

- [x] **Task 8: Cập nhật gantt-left-panel.html** (AC: 2, 3, 4, 5, 6)
  - [x] 8.1 Cột Name: editable-cell + inline input khi editing
  - [x] 8.2 Cột Status badge: click → inline select với statusOptions
  - [x] 8.3 Cột BĐ (plannedStart): click → inline date input
  - [x] 8.4 Cột KT (plannedEnd): click → inline date input
  - [x] 8.5 Thêm cột % (percentComplete): click → inline number input

- [x] **Task 9: Cập nhật gantt-left-panel.scss** (AC: 10)
  - [x] 9.1 `.left-panel-header`: dùng CSS vars `--table-header-bg`, `--border-color-light`
  - [x] 9.2 `.left-panel-row:hover`: dùng `--docker-blue-light`
  - [x] 9.3 Thêm `.editable-cell`, `.inline-input`, `.inline-select`
  - [x] 9.4 Thêm `.col-percent` 44px

- [x] **Task 10: Cập nhật gantt.scss** (AC: 10)
  - [x] 10.1 Thêm `.project-title` CSS

---

## Dev Notes

### ⚠️ Những Lỗi Cần Tránh

1. **KHÔNG dùng `createFeature`** — đã học từ Story 1.4: type error. Dùng `createReducer` trực tiếp.
2. **KHÔNG dùng `*ngIf`/`*ngFor`** — dùng Angular 17+ `@if`/`@for`.
3. **Selector cho project name**: KHÔNG dispatch `loadProjects` mới. Dùng `selectProjectEntities` từ projects store — project có thể đã được load khi user navigate từ project list. Nếu chưa có thì show `projectId` làm fallback.
4. **`discardGanttEdits` hiện tại chỉ clear `dirtyTasks` dict, KHÔNG reset task state** — đây là bug. Sau khi discard, task vẫn hiển thị giá trị bị thay đổi. Fix bằng effect mới reload data.
5. **`conflict$.subscribe` trong `ngOnInit` không unsubscribe** — memory leak. Fix bằng `takeUntilDestroyed()`.
6. **`ChangeDetectorRef`**: `GanttLeftPanelComponent` dùng `OnPush`, nên sau khi set state editing, phải gọi `cdr.markForCheck()`.
7. **KHÔNG sửa `task-tree` hay `project-detail`** — story này chỉ sửa Gantt components.

### Existing Patterns — REUSE, ĐỪNG VIẾT LẠI

| Pattern | Location | Dùng cho |
|---|---|---|
| `editable-cell` CSS + hover | `task-tree.scss` | Copy CSS sang `gantt-left-panel.scss` |
| `inline-input` / `inline-select` CSS | `task-tree.scss` | Copy CSS sang `gantt-left-panel.scss` |
| `startEdit/commitEdit/onEditKeydown` logic | `task-tree.ts` | Áp dụng tương tự vào `GanttLeftPanelComponent` |
| `ConflictDialogComponent` pattern | `shared/components/conflict-dialog/` | Template cho `ConfirmDialogComponent` mới |
| `GanttActions.markTaskDirty` | `gantt.actions.ts` | Dùng để queue inline edits |
| CSS variables | `styles.scss` | `--table-header-bg`, `--border-color-light`, `--docker-blue-light`, `--text-primary` |
| `takeUntilDestroyed()` | `@angular/core/rxjs-interop` | Fix memory leak trong `ngOnInit` |
| `selectProjectEntities` | `features/projects/store/projects.selectors.ts` | Lấy tên project |
| `selectGanttProjectId` | `features/gantt/store/gantt.selectors.ts` | Lấy projectId từ gantt state |

### File Structure — Files cần sửa

```
frontend/project-management-web/src/app/
├── shared/components/
│   └── confirm-dialog/
│       └── confirm-dialog.ts          ← NEW (standalone, inline template)
│
├── features/gantt/
│   ├── models/
│   │   └── gantt.model.ts             ← MODIFY: extend GanttTaskEdit
│   ├── store/
│   │   ├── gantt.reducer.ts           ← MODIFY: markTaskDirty + saveGanttEditsSuccess
│   │   └── gantt.effects.ts           ← MODIFY: buildUpdatePayload + discard effect
│   └── components/
│       ├── gantt/
│       │   ├── gantt.ts               ← MODIFY: project name, onInlineEdit, confirm dialog, memory leak fix
│       │   ├── gantt.html             ← MODIFY: project name display, inlineEdit binding
│       │   └── gantt.scss             ← MODIFY: add .project-title
│       └── gantt-left-panel/
│           ├── gantt-left-panel.ts    ← MODIFY: inline edit logic
│           ├── gantt-left-panel.html  ← MODIFY: editable cells
│           └── gantt-left-panel.scss  ← MODIFY: CSS vars + editable-cell styles
```

### Model Changes Detail

#### `GanttTaskEdit` — Extended (gantt.model.ts)

```typescript
export interface GanttTaskEdit {
  taskId: string;
  originalVersion: number;
  newPlannedStart?: Date;
  newPlannedEnd?: Date;
  newPredecessors?: GanttDependency[];
  // NEW:
  newName?: string;
  newStatus?: string;
  newPercentComplete?: number;
}
```

#### `GanttInlineEditEvent` — New interface (define trong gantt-left-panel.ts)

```typescript
export interface GanttInlineEditEvent {
  taskId: string;
  version: number;
  field: 'name' | 'status' | 'plannedStart' | 'plannedEnd' | 'percentComplete';
  value: string | number | null;
}
```

### Reducer Changes Detail

#### `markTaskDirty` handler update (gantt.reducer.ts)

```typescript
on(GanttActions.markTaskDirty, (s, { edit }) => ({
  ...s,
  dirtyTasks: { ...s.dirtyTasks, [edit.taskId]: edit },
  tasks: s.tasks.map(t => t.id === edit.taskId
    ? {
        ...t,
        dirty: true,
        plannedStart: edit.newPlannedStart ?? t.plannedStart,
        plannedEnd: edit.newPlannedEnd ?? t.plannedEnd,
        predecessors: edit.newPredecessors ?? t.predecessors,
        name: edit.newName ?? t.name,               // NEW
        status: edit.newStatus ?? t.status,          // NEW
        percentComplete: edit.newPercentComplete !== undefined
          ? edit.newPercentComplete
          : t.percentComplete,                       // NEW
      }
    : t
  ),
})),
```

#### `saveGanttEditsSuccess` handler update (gantt.reducer.ts)

Cần thêm `name`, `status`, `percentComplete` vào `updatedTasks` items:

```typescript
on(GanttActions.saveGanttEditsSuccess, (s, { updatedTasks }) => ({
  ...s,
  saving: false, dirtyTasks: {}, conflict: null,
  tasks: s.tasks.map(t => {
    const updated = updatedTasks.find(u => u.id === t.id);
    return updated
      ? { ...t,
          version: updated.version, dirty: false,
          plannedStart: updated.plannedStart, plannedEnd: updated.plannedEnd,
          predecessors: updated.predecessors,
          name: updated.name ?? t.name,               // NEW
          status: updated.status ?? t.status,          // NEW
          percentComplete: updated.percentComplete !== undefined
            ? updated.percentComplete : t.percentComplete,  // NEW
        }
      : t;
  }),
})),
```

Cần cập nhật action `saveGanttEditsSuccess` để `updatedTasks` chứa thêm các field mới. Xem `gantt.actions.ts` để update action payload type.

### Effects Changes Detail

#### `buildUpdatePayload` update (gantt.effects.ts)

```typescript
function buildUpdatePayload(edit: GanttTaskEdit, original: ProjectTask): UpdateTaskPayload {
  return {
    parentId: original.parentId,
    type: original.type,
    vbs: original.vbs ?? undefined,
    name: edit.newName ?? original.name,                          // CHANGED
    priority: original.priority,
    status: edit.newStatus ?? original.status,                    // CHANGED
    notes: original.notes ?? undefined,
    plannedStartDate: edit.newPlannedStart
      ? formatDateOnly(edit.newPlannedStart)
      : original.plannedStartDate ?? undefined,
    plannedEndDate: edit.newPlannedEnd
      ? formatDateOnly(edit.newPlannedEnd)
      : original.plannedEndDate ?? undefined,
    actualStartDate: original.actualStartDate ?? undefined,
    actualEndDate: original.actualEndDate ?? undefined,
    plannedEffortHours: original.plannedEffortHours ?? undefined,
    percentComplete: edit.newPercentComplete !== undefined        // CHANGED
      ? edit.newPercentComplete
      : original.percentComplete ?? undefined,
    assigneeUserId: original.assigneeUserId ?? undefined,
    sortOrder: original.sortOrder,
    predecessors: edit.newPredecessors
      ? edit.newPredecessors.map(p => ({ predecessorId: p.predecessorId, dependencyType: p.type }))
      : original.predecessors.map(p => ({ predecessorId: p.predecessorId, dependencyType: p.dependencyType })),
  };
}
```

#### New `discardGanttEdits$` effect

```typescript
discardGanttEdits$ = createEffect(() =>
  this.actions$.pipe(
    ofType(GanttActions.discardGanttEdits),
    withLatestFrom(this.store.select(selectGanttState)),
    switchMap(([_, ganttState]) => {
      const projectId = ganttState.projectId;
      if (!projectId) return of(); // no-op
      return of(GanttActions.loadGanttData({ projectId }));
    })
  )
);
```

### Left Panel Inline Edit Logic

```typescript
// gantt-left-panel.ts (thêm vào class)

private editingTaskId: string | null = null;
private editingField: GanttEditableField | null = null;
editingValue = '';

isEditing(taskId: string, field: string): boolean {
  return this.editingTaskId === taskId && this.editingField === field;
}

startEdit(task: GanttTask, field: GanttEditableField, event: Event): void {
  event.stopPropagation();
  this.editingTaskId = task.id;
  this.editingField = field;
  // set initial value
  switch (field) {
    case 'name': this.editingValue = task.name; break;
    case 'status': this.editingValue = task.status; break;
    case 'plannedStart': this.editingValue = task.plannedStart ? this.toDateInputValue(task.plannedStart) : ''; break;
    case 'plannedEnd': this.editingValue = task.plannedEnd ? this.toDateInputValue(task.plannedEnd) : ''; break;
    case 'percentComplete': this.editingValue = String(task.percentComplete ?? 0); break;
  }
  this.cdr.markForCheck();
}

commitEdit(task: GanttTask): void {
  if (!this.editingField) return;
  const field = this.editingField;
  const value = this.editingValue;
  this.cancelEdit();

  let emitValue: string | number | null = value;
  if (field === 'percentComplete') {
    const n = Number(value);
    emitValue = isNaN(n) ? task.percentComplete : Math.max(0, Math.min(100, n));
  } else if ((field === 'plannedStart' || field === 'plannedEnd') && !value) {
    emitValue = null;
  }

  this.inlineEdit.emit({ taskId: task.id, version: task.version, field, value: emitValue });
}

cancelEdit(): void {
  this.editingTaskId = null;
  this.editingField = null;
  this.editingValue = '';
  this.cdr.markForCheck();
}

onEditKeydown(event: KeyboardEvent, task: GanttTask): void {
  if (event.key === 'Enter') { event.preventDefault(); this.commitEdit(task); }
  if (event.key === 'Escape') { this.cancelEdit(); }
}

private toDateInputValue(date: Date): string {
  // Format: yyyy-MM-dd for <input type="date">
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

readonly statusOptions = ['NotStarted', 'InProgress', 'Completed', 'OnHold', 'Cancelled', 'Delayed'];
statusLabel(s: string): string {
  const map: Record<string, string> = {
    NotStarted: 'Chưa bắt đầu', InProgress: 'Đang làm',
    Completed: 'Hoàn thành', OnHold: 'Tạm dừng',
    Cancelled: 'Đã hủy', Delayed: 'Trễ hạn',
  };
  return map[s] ?? s;
}
```

### `onInlineEdit` trong GanttComponent

```typescript
onInlineEdit(event: GanttInlineEditEvent): void {
  const edit: GanttTaskEdit = {
    taskId: event.taskId,
    originalVersion: event.version,
  };
  switch (event.field) {
    case 'name':             edit.newName = event.value as string; break;
    case 'status':           edit.newStatus = event.value as string; break;
    case 'percentComplete':  edit.newPercentComplete = event.value as number; break;
    case 'plannedStart':
      edit.newPlannedStart = event.value ? new Date(event.value as string) : undefined; break;
    case 'plannedEnd':
      edit.newPlannedEnd = event.value ? new Date(event.value as string) : undefined; break;
  }
  this.store.dispatch(GanttActions.markTaskDirty({ edit }));
}
```

### ConfirmDialogComponent (confirm-dialog.ts)

```typescript
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface ConfirmDialogData {
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Xác nhận</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close(false)">{{ data.cancelLabel ?? 'Hủy' }}</button>
      <button mat-flat-button color="warn" (click)="dialogRef.close(true)">{{ data.confirmLabel ?? 'Xác nhận' }}</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
```

### deleteGanttTask thay confirm()

```typescript
deleteGanttTask(task: GanttTask): void {
  this.dialog.open(ConfirmDialogComponent, {
    data: { message: `Xóa task "${task.name}"? Hành động này không thể hoàn tác.`, confirmLabel: 'Xóa' } as ConfirmDialogData,
    width: '360px',
  }).afterClosed().subscribe((confirmed: boolean) => {
    if (!confirmed) return;
    this.store.dispatch(TasksActions.deleteTask({
      projectId: this.projectId,
      taskId: task.id,
      version: task.version,
    }));
    setTimeout(() => this.reloadGantt(), 500);
  });
}
```

### Project Name Selector

Thêm selector vào `gantt.selectors.ts` hoặc define inline trong `gantt.ts`:

```typescript
// Trong gantt.selectors.ts:
import { selectProjectEntities } from '../../projects/store/projects.selectors';

export const selectProjectByGanttId = createSelector(
  selectProjectEntities,
  selectGanttProjectId,
  (entities, projectId) => projectId ? entities[projectId] : undefined
);
```

```typescript
// Trong gantt.ts:
project$ = this.store.select(selectProjectByGanttId);
```

### Memory Leak Fix

```typescript
// Trong gantt.ts:
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DestroyRef } from '@angular/core';

// Trong class:
private destroyRef = inject(DestroyRef);

// Trong ngOnInit:
this.conflict$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(conflict => {
  if (conflict) this.openConflictDialog(conflict);
});
```

### CSS Changes — gantt-left-panel.scss

```scss
.left-panel-header {
  // TRƯỚC: background: #f5f5f5; border-bottom: 2px solid #e0e0e0;
  background: var(--table-header-bg, #f5f8fb);
  border-bottom: 2px solid var(--border-color-light, #eef2f7);
}

.left-panel-row:hover {
  // TRƯỚC: background: #e8f4fd;
  background: var(--docker-blue-light, #e8f4fd);
}

// THÊM MỚI — giống task-tree.scss:
.editable-cell {
  cursor: pointer;
  border-radius: 3px;
  padding: 2px 4px;

  &:hover {
    background: rgba(36, 150, 237, 0.08);
    box-shadow: 0 0 0 1px rgba(36, 150, 237, 0.3);
    border-radius: 3px;
  }
}

.inline-input {
  width: 100%;
  box-sizing: border-box;
  border: 1.5px solid var(--docker-blue, #2496ED);
  border-radius: 4px;
  padding: 4px 7px;
  font-size: 13px;
  font-family: inherit;
  color: var(--text-primary);
  background: #fff;
  outline: none;
  box-shadow: 0 0 0 3px rgba(36, 150, 237, 0.18);

  &[type='number'] { text-align: right; }
  &.date-input     { padding: 3px 5px; font-size: 12px; }
}

.inline-select {
  width: 100%;
  box-sizing: border-box;
  border: 1.5px solid var(--docker-blue, #2496ED);
  border-radius: 4px;
  padding: 4px 6px;
  font-size: 12px;
  font-family: inherit;
  color: var(--text-primary);
  background: #fff;
  outline: none;
  box-shadow: 0 0 0 3px rgba(36, 150, 237, 0.18);
  cursor: pointer;
}

.col-percent {
  width: 44px;
  min-width: 44px;
  text-align: center;
  font-size: 11px;
  color: var(--docker-blue, #2496ED);
  font-weight: 600;
}
```

### gantt.html — Project Name + InlineEdit binding

```html
<!-- Trong toolbar-left, sau view-toggle và toolbar-sep: -->
<span class="project-title">{{ (project$ | async)?.name }}</span>

<!-- Trong <app-gantt-left-panel> binding: -->
<app-gantt-left-panel
  [tasks]="tasks"
  [scrollTop]="scrollTop"
  (scrollChange)="onScrollChange($event)"
  (addChild)="openAddTaskDialog($event)"
  (editTask)="openEditTaskDialog($event)"
  (deleteTask)="deleteGanttTask($event)"
  (inlineEdit)="onInlineEdit($event)" />
```

### gantt.scss — Project Title

```scss
.project-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary, #1a2332);
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
```

### gantt-left-panel.html — Inline editing template structure

Cho mỗi editable column, pattern là:
```html
<!-- Pattern cho name: -->
@if (isEditing(task.id, 'name')) {
  <input class="inline-input" [(ngModel)]="editingValue"
         (blur)="commitEdit(task)" (keydown)="onEditKeydown($event, task)"
         (click)="$event.stopPropagation()" (mousedown)="$event.stopPropagation()"
         autofocus />
} @else {
  <span class="task-name editable-cell" (click)="startEdit(task, 'name', $event)">
    <!-- existing content -->
  </span>
}
```

Pattern tương tự cho status (dùng `<select>`), plannedStart, plannedEnd (dùng `<input type="date">`), percentComplete (dùng `<input type="number">`).

Cần import `FormsModule` trong `imports` array của `GanttLeftPanelComponent`.

### Lưu ý quan trọng về Selector

- `selectProjectEntities` trả về `Dictionary<Project>` (EntityState từ NgRx entity)
- Nếu project chưa được load (user navigate trực tiếp vào Gantt URL), `entities[projectId]` sẽ là `undefined` → show fallback `projectId`
- KHÔNG dispatch `loadProjects` từ Gantt — để tránh side effect không cần thiết

### Scope rõ ràng

**TRONG story này:**
- Sửa Gantt left panel để có inline editing
- Sửa toolbar Gantt để có project name và UI nhất quán
- Sửa discard để reload data
- Tạo ConfirmDialogComponent dùng cho delete

**KHÔNG làm trong story này:**
- Sửa project-detail page (riêng biệt)
- Thêm inline editing cho assignee trong Gantt (phức tạp hơn, để sau)
- Thêm cột actual dates trong Gantt left panel
- Drag-to-reorder trong left panel

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Build pass: `npx ng build --configuration development` — không có TypeScript error
- Tests: 84/93 pass; 9 fail trong 2 file pre-existing (app.spec.ts + login.spec.ts — đã biết từ Story 1.5: Vitest+Angular templateUrl issue)
- Không có regression mới nào

### Completion Notes List

- Đã extend `GanttTaskEdit` với `newName?`, `newStatus?`, `newPercentComplete?`
- Đã cập nhật `gantt.actions.ts`: `SaveGanttEditsSuccess` action thêm `name?`, `status?`, `percentComplete?` vào `updatedTasks` items
- Đã cập nhật `gantt.reducer.ts`: `markTaskDirty` áp name/status/percent lên task state; `saveGanttEditsSuccess` restore các fields từ server
- Đã cập nhật `gantt.effects.ts`: `buildUpdatePayload` dùng inline edit values; `saveGanttEditsSuccess` map đầy đủ fields; thêm `discardGanttEdits$` effect reload data sau khi discard
- Đã tạo `ConfirmDialogComponent` tại `shared/components/confirm-dialog/`
- Đã thêm `selectProjectByGanttId` selector trong `gantt.selectors.ts`
- Đã cập nhật `gantt.ts`: project name observable, `onInlineEdit()`, ConfirmDialog cho delete, fix memory leak với `takeUntilDestroyed()`
- Đã cập nhật `gantt.html`: hiển thị project name trong toolbar, binding `(inlineEdit)`
- Đã cập nhật `gantt.scss`: thêm `.project-title`
- Đã cập nhật `gantt-left-panel.ts`: full inline edit logic (startEdit/commitEdit/cancelEdit/onEditKeydown/isEditing), inject ChangeDetectorRef, FormsModule
- Đã cập nhật `gantt-left-panel.html`: editable cells cho name, status, plannedStart, plannedEnd, percentComplete; thêm cột % mới
- Đã cập nhật `gantt-left-panel.scss`: CSS vars thay hardcode colors, thêm `.editable-cell`, `.inline-input`, `.inline-select`, `.col-percent`

### File List

**New files:**
- `frontend/project-management-web/src/app/shared/components/confirm-dialog/confirm-dialog.ts`

**Modified files:**
- `frontend/project-management-web/src/app/features/gantt/models/gantt.model.ts`
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
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
