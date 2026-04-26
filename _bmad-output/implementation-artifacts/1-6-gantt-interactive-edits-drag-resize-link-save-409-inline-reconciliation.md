# Story 1.6: Gantt Interactive Edits (Drag/Resize/Link) + Save + 409 Inline Reconciliation

Status: review

**Story ID:** 1.6
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 2
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want chỉnh kế hoạch trực tiếp trên Gantt (kéo/resize/link) và lưu an toàn,
So that tôi có thể lập kế hoạch nhanh nhưng không bị ghi đè dữ liệu của người khác.

## Acceptance Criteria

1. **Given** user là member của `{projectId}` và Gantt đã render (Story 1.5)
   **When** user kéo task bar để đổi `plannedStartDate`/`plannedEndDate` hoặc resize bar
   **Then** UI hiển thị trạng thái "chưa lưu" (dirty badge) và cho phép lưu thay đổi

2. **Given** task có dependency
   **When** user tạo/sửa link dependency (FS/SS/FF/SF) bằng cách click endpoint của bar
   **Then** UI update link arrow và lưu theo cùng cơ chế optimistic locking

3. **Given** user bấm "Lưu"
   **When** client gửi `PUT /api/v1/projects/{projectId}/tasks/{taskId}` với `If-Match` đúng version
   **Then** server trả `200` + `ETag` mới
   **And** UI refresh GanttTask state theo bản mới nhất + xóa dirty flag

4. **Given** client update nhưng thiếu `If-Match`
   **When** server nhận request
   **Then** trả `412 ProblemDetails`
   **And** UI hiển thị thông báo lỗi và reload toàn bộ Gantt data

5. **Given** client update với `If-Match` không khớp version
   **When** server phát hiện conflict
   **Then** trả `409 ProblemDetails` kèm `extensions.current` (TaskDto) + `extensions.eTag`
   **And** UI mở ConflictDialogComponent cho phép chọn: "Dùng bản mới nhất" (discard local) hoặc "Áp lại thay đổi của tôi" (re-apply diff)

6. **Given** user không phải member của `{projectId}`
   **When** thao tác save từ Gantt
   **Then** backend trả `404` và UI hiển thị thông báo lỗi, không leak dữ liệu dự án

## Tasks / Subtasks

- [x] **Task 1: Drag Bar để đổi ngày (FE)**
  - [x] 1.1 Dùng native SVG mouse events thay vì CDK DragDrop (CDK không hỗ trợ SVGElement)
  - [x] 1.2 Implement `onBarMouseDown(event, task, 'move')`, `handleMouseMove`, `handleMouseUp` trong `GanttTimelineComponent`
  - [x] 1.3 Khi drag kết thúc: tính `newPlannedStart` + `newPlannedEnd` từ delta X pixels → ngày
  - [x] 1.4 Mark task dirty (dispatch `GanttActions.markTaskDirty`, set `dirty: true`)
  - [x] 1.5 Hiển thị "ghost bar" trong khi drag (opacity 0.5), cập nhật vị trí realtime

- [x] **Task 2: Resize Bar để đổi End Date (FE)**
  - [x] 2.1 Thêm resize handle SVG `<rect>` ở right edge của mỗi bar
  - [x] 2.2 Implement mouse drag trên resize handle → `onBarMouseDown(event, task, 'resize')`
  - [x] 2.3 Minimum bar width = 1 ngày (không cho resize nhỏ hơn 1 ngày)
  - [x] 2.4 Mark task dirty sau khi resize

- [x] **Task 3: Dirty State + Toolbar Save (FE)**
  - [x] 3.1 `dirtyTasks: Record<string, GanttTaskEdit>` trong `GanttState`
  - [x] 3.2 Dirty count hiển thị trong nút "Lưu (N)" trong toolbar
  - [x] 3.3 Nút "Lưu tất cả" trong toolbar → dispatch `saveGanttEdits`
  - [x] 3.4 Nút "Hoàn tác" → dispatch `discardGanttEdits`
  - [x] 3.5 Actions: `markTaskDirty`, `saveGanttEdits`, `saveGanttEditsSuccess`, `saveGanttEditsFailure`, `discardGanttEdits`, `ganttConflict`, `resolveConflict`

- [x] **Task 4: Save Effects + API calls (FE)**
  - [x] 4.1 Effect `saveGanttEdits$` → gọi `TasksApiService.updateTask()` cho từng dirty task (forkJoin với per-request catchError)
  - [x] 4.2 `originalVersion` trong `GanttTaskEdit` dùng cho `If-Match` header
  - [x] 4.3 On success: dispatch `saveGanttEditsSuccess` → xóa dirty flag, cập nhật version mới
  - [x] 4.4 On 409: dispatch `ganttConflict` → mở `ConflictDialogComponent`
  - [x] 4.5 On 412: dispatch `loadGanttData` → reload toàn bộ Gantt data
  - [x] 4.6 On 404: dispatch `saveGanttEditsFailure` với message "Không có quyền truy cập dự án"

- [x] **Task 5: 409 Conflict Reconciliation (FE)**
  - [x] 5.1 Khi nhận 409: mở `ConflictDialogComponent` với server state và localEdit
  - [x] 5.2 Option "Dùng bản mới nhất": `discardGanttEdits` + reload
  - [x] 5.3 Option "Áp lại thay đổi của tôi": `resolveConflict` + `markTaskDirty` với server version mới + retry save

- [x] **Task 6: Dependency Link Drawing (FE)**
  - [x] 6.1 Connect mode toggle trong toolbar (icon `link`)
  - [x] 6.2 Khi connect mode ON: endpoint circles hiển thị trên bar
  - [x] 6.3 Mouse drag từ endpoint của task A → thả vào endpoint của task B → tạo dependency
  - [x] 6.4 Temporary SVG dashed line trong khi đang drag connection
  - [x] 6.5 Khi kết nối hoàn tất: tạo FS dependency mặc định → mark dirty
  - [x] 6.6 Click vào existing arrow → xóa dependency → mark dirty

- [x] **Task 7: Update GanttTask Model + Store (FE)**
  - [x] 7.1 `dirty: boolean` và `version: number` trong `GanttTask`
  - [x] 7.2 `GanttAdapter.adapt()` map `task.version` vào `GanttTask.version`
  - [x] 7.3 `GanttTaskEdit` interface
  - [x] 7.4 `GanttState` với `dirtyTasks`, `saving`, `conflict`

- [x] **Task 8: Tests (FE)**
  - [x] 8.1 Unit test drag calculation: `deltaX → daysDelta → newDate` (9 tests)
  - [x] 8.2 Unit test dirty state reducer: mark dirty → save success → clean (4 tests)
  - [x] 8.3 Unit test conflict reducer: 409 response → conflict state set (2 tests)
  - [x] 8.4 Unit test load/discard reducer (3 tests)

---

## Dev Notes

### Những gì đã có sẵn (KHÔNG viết lại)

| File/Pattern | Trạng thái | Ghi chú |
|---|---|---|
| `GanttAdapterService` | ✅ Story 1.5 | Cần update để map `version` vào `GanttTask` |
| `GanttTimelineComponent` | ✅ Story 1.5 | Thêm drag/resize event handlers |
| `GanttComponent` | ✅ Story 1.5 | Thêm toolbar buttons: Save/Discard/Connect |
| `TasksApiService.updateTask()` | ✅ Story 1.4 | Đã có If-Match header handling |
| `ConflictDialogComponent` | ✅ Story 1.3 | Dùng lại cho 409 Gantt conflict |
| `gantt.actions/reducer/effects` | ✅ Story 1.5 | Extend thêm dirty/save/conflict actions |
| Angular CDK DragDrop | ✅ installed | `@angular/cdk` — import `DragDropModule` |

---

### Update GanttTask Model

```typescript
// Cập nhật gantt.model.ts — THÊM CÁC FIELD MỚI
export interface GanttTask {
  // ... fields hiện tại ...
  version: number;     // THÊM: dùng cho If-Match header
  dirty: boolean;      // THÊM: local edit chưa lưu
}

export interface GanttTaskEdit {
  taskId: string;
  originalVersion: number;
  newPlannedStart?: Date;
  newPlannedEnd?: Date;
  newPredecessors?: GanttDependency[];
}

export interface GanttConflictState {
  taskId: string;
  serverTask: ProjectTask;  // server state từ 409 response
  localEdit: GanttTaskEdit;
  serverETag: string;
}

// Update GanttState
export interface GanttState {
  // ... fields hiện tại ...
  dirtyTasks: Record<string, GanttTaskEdit>;
  saving: boolean;
  conflict: GanttConflictState | null;
}
```

---

### Drag Implementation — SVG Native (không dùng CDK DragDrop)

Vì drag xảy ra trong SVG coordinate space, dùng native SVG mouse events thay vì CDK DragDrop (CDK tối ưu cho HTML elements):

```typescript
// GanttTimelineComponent
onBarMouseDown(event: MouseEvent, task: GanttTask): void {
  this.dragging = { task, startX: event.clientX, originalStart: task.plannedStart!, originalEnd: task.plannedEnd! };
  this.svgRef.nativeElement.addEventListener('mousemove', this.onMouseMove);
  this.svgRef.nativeElement.addEventListener('mouseup', this.onMouseUp);
}

onMouseMove = (event: MouseEvent): void => {
  if (!this.dragging) return;
  const deltaX = event.clientX - this.dragging.startX;
  const deltaDays = Math.round(deltaX / this.pixelsPerDay);
  // Update ghost position
  this.ghostTask = { ...this.dragging.task, plannedStart: addDays(this.dragging.originalStart, deltaDays), plannedEnd: addDays(this.dragging.originalEnd, deltaDays) };
};

onMouseUp = (event: MouseEvent): void => {
  if (!this.dragging) return;
  const deltaX = event.clientX - this.dragging.startX;
  const deltaDays = Math.round(deltaX / this.pixelsPerDay);
  this.taskEdited.emit({ task: this.dragging.task, deltaDays });
  this.dragging = null;
  this.ghostTask = null;
};
```

---

### Save Flow

```typescript
// gantt.effects.ts — saveGanttEdits$
saveGanttEdits$ = createEffect(() =>
  this.actions$.pipe(
    ofType(GanttActions.saveGanttEdits),
    withLatestFrom(this.store.select(selectGanttState)),
    switchMap(([_, state]) => {
      const dirtyEdits = Object.values(state.dirtyTasks);
      // Lấy ProjectTask hiện tại từ tasks store để build UpdateTaskPayload
      const saves$ = dirtyEdits.map(edit => this.buildSaveRequest(edit, state));
      return forkJoin(saves$).pipe(
        map(() => GanttActions.saveGanttEditsSuccess()),
        catchError(err => {
          if (err.status === 409) {
            return of(GanttActions.ganttConflict({
              taskId: err.taskId,
              serverTask: err.error.current,
              localEdit: err.localEdit,
              serverETag: err.error.eTag,
            }));
          }
          if (err.status === 412) return of(GanttActions.loadGanttData({ projectId: state.projectId! }));
          return of(GanttActions.saveGanttEditsFailure({ error: err?.error?.detail ?? 'Lỗi lưu Gantt' }));
        })
      );
    })
  )
);
```

---

### UpdateTask Payload từ GanttTaskEdit

Khi save, cần build `UpdateTaskPayload` đầy đủ (API yêu cầu all fields trong PUT):
- Lấy task gốc từ NgRx `tasks` state (đã có từ Story 1.4)
- Override `plannedStartDate` / `plannedEndDate` / `predecessors` từ `GanttTaskEdit`
- Dùng `originalVersion` làm `If-Match`

```typescript
private buildUpdatePayload(edit: GanttTaskEdit, originalTask: ProjectTask): UpdateTaskPayload {
  return {
    parentId: originalTask.parentId,
    type: originalTask.type,
    vbs: originalTask.vbs ?? undefined,
    name: originalTask.name,
    priority: originalTask.priority,
    status: originalTask.status,
    notes: originalTask.notes ?? undefined,
    plannedStartDate: edit.newPlannedStart
      ? formatDateOnly(edit.newPlannedStart)
      : originalTask.plannedStartDate ?? undefined,
    plannedEndDate: edit.newPlannedEnd
      ? formatDateOnly(edit.newPlannedEnd)
      : originalTask.plannedEndDate ?? undefined,
    // ... remaining fields ...
    sortOrder: originalTask.sortOrder,
    predecessors: (edit.newPredecessors ?? originalTask.predecessors).map(p => ({
      predecessorId: p.predecessorId ?? (p as any).predecessorId,
      dependencyType: p.dependencyType ?? (p as any).type,
    })),
  };
}

function formatDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}
```

---

### ConflictDialogComponent — Reuse Pattern (Story 1.3)

```typescript
// Mở dialog khi có 409
const dialogRef = this.dialog.open(ConflictDialogComponent, {
  data: {
    message: `Task "${task.name}" đã thay đổi. Server version: ${serverTask.version}`,
    currentState: serverTask,
  }
});

dialogRef.afterClosed().subscribe(result => {
  if (result === 'use-server') {
    this.store.dispatch(GanttActions.discardTaskEdit({ taskId }));
  } else if (result === 'keep-mine') {
    // Re-apply edit với server version mới
    this.store.dispatch(GanttActions.retryTaskSave({ taskId, newVersion: serverTask.version }));
  }
});
```

---

### Lỗi cần tránh

1. **SVG coordinate vs DOM coordinate**: drag cần convert clientX → SVG coordinate dùng `SVGElement.getScreenCTM().inverse()`
2. **Không dùng CDK DragDrop cho SVG**: CDK hoạt động với HTMLElement, không phải SVGElement — dùng native mousemove/mouseup
3. **Memory leak**: removeEventListener trong `ngOnDestroy` và sau mỗi mouseup
4. **forkJoin fails fast**: nếu 1 task save fail (409), forkJoin hủy các request còn lại — cần `catchError` per-request rồi merge results

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống — chưa implement)_

### Completion Notes List

- Dùng native SVG mouse events thay vì CDK DragDrop (CDK không hỗ trợ SVGElement)
- `forkJoin` với per-request `catchError` để capture 409/412/404 per task (không fail-fast)
- Conflict dialog reuses `ConflictDialogComponent` từ Story 1.3 với `ConflictDialogData`
- `selectTaskEntities` từ TasksState để build `UpdateTaskPayload` đầy đủ trong effect
- SVG coordinate conversion via `getScreenCTM().inverse()` cho connect mode

### File List

- `src/app/features/gantt/models/gantt.model.ts` — GanttTask, GanttTaskEdit, GanttConflictState, GanttState (updated Story 1.5)
- `src/app/features/gantt/services/gantt-adapter.service.ts` — version + dirty field mapping (updated)
- `src/app/features/gantt/store/gantt.actions.ts` — markTaskDirty, saveGanttEdits*, ganttConflict, resolveConflict, discardGanttEdits (updated)
- `src/app/features/gantt/store/gantt.reducer.ts` — full dirty/save/conflict handling (updated)
- `src/app/features/gantt/store/gantt.effects.ts` — saveGanttEdits$ effect (updated)
- `src/app/features/gantt/store/gantt.selectors.ts` — selectDirtyTasks, selectDirtyTasksCount, selectSaving, selectConflict (updated)
- `src/app/features/gantt/components/gantt-timeline/gantt-timeline.ts` — drag/resize/connect mode
- `src/app/features/gantt/components/gantt-timeline/gantt-timeline.html` — ghost bar, endpoint circles, connect line, dirty indicators
- `src/app/features/gantt/components/gantt-timeline/gantt-timeline.scss` — connect-mode cursor
- `src/app/features/gantt/components/gantt/gantt.ts` — toolbar actions, conflict dialog wiring
- `src/app/features/gantt/components/gantt/gantt.html` — Save/Discard/Connect toolbar buttons
- `src/app/features/gantt/components/gantt/gantt.scss` — toolbar-center, toolbar-right
- `src/app/features/gantt/store/gantt.reducer.spec.ts` — 13 unit tests (new)
- `src/app/features/gantt/components/gantt-timeline/gantt-timeline.spec.ts` — 9 drag calculation tests (new)
