---
title: 'Unify View Layout — Embed Gantt into Project Detail'
type: 'refactor'
created: '2026-04-29'
status: 'draft'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Grid và Board view nằm trong `ProjectDetailComponent` (chung header, back button, view toggle), nhưng Gantt là route riêng (`/projects/:id/gantt`) với toolbar khác cấu trúc hoàn toàn — switch giữa các view bị giật cục, layout không nhất quán.

**Approach:** Nhúng Gantt vào `ProjectDetailComponent` như view thứ ba (tương tự Board). Bỏ route `/projects/:id/gantt`, chuyển sang `?view=gantt` query param. Merge gantt state/logic vào component, dùng `GanttLeftPanelComponent` + `GanttTimelineComponent` trực tiếp. Xóa `GanttComponent` (route entry + 3 files).

## Boundaries & Constraints

**Always:**
- Ba view chia sẻ cùng header: back button → title → toggle (Grid/Board/Gantt) → [gantt-only: separator + granularity + save/discard dirty buttons] → Add Task
- Khi `currentView === 'gantt'`: wrapper dùng `height: 100vh; overflow: hidden; padding: 0` để split panel scroll-sync hoạt động đúng
- URL gantt: `?view=gantt` — không còn route riêng
- `ngOnDestroy` dispatch `GanttActions.clearGantt()` + cleanup DOCUMENT resize listeners
- Switch sang gantt → dispatch `GanttActions.loadGanttData({ projectId })`
- Tất cả gantt subscriptions trong `ngOnInit` dùng `takeUntilDestroyed(this.destroyRef)`
- Reuse `members()` signal hiện có — không tạo `ganttMembers` riêng (tránh double API call)

**Ask First:**
- Nếu cần refactor thêm component khác ngoài scope đã liệt kê

**Never:**
- Giữ route `/projects/:id/gantt`
- Tách gantt thành feature module / lazy-loaded child route
- Thay đổi nội bộ `GanttLeftPanelComponent`, `GanttTimelineComponent`

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Switch to gantt | Click "Gantt" toggle | `currentView='gantt'`, dispatch loadGanttData, URL=`?view=gantt`, split panel full-height | - |
| Switch away | Click "Grid" / "Board" | dispatch clearGantt, URL update, layout trở về scrollable | - |
| Direct URL `?view=gantt` | Page load | ngOnInit detect param, dispatch loadGanttData, split panel hiện | - |
| Old URL `/projects/:id/gantt` | Navigate | `**` catch-all → redirect login | - |
| Gantt load error | API fail | Error banner trong content area, grid/board không ảnh hưởng | - |
| Dirty gantt + switch view | dirtyCount > 0 | Vẫn switch (không block), clearGantt, dirty state cleared | - |
| Add task khi ở gantt | Dialog close | `reloadGantt()` tự động sau close | - |
| Resize panel + navigate away | Mouse đang hold | ngOnDestroy cleanup → cursor không bị kẹt `col-resize` | - |

</frozen-after-approval>

## Code Map

- `frontend/.../features/projects/projects.routes.ts` -- Xóa route `':projectId/gantt'`
- `frontend/.../features/projects/components/project-detail/project-detail.ts` -- Merge toàn bộ gantt logic (state, subscriptions, handlers, resize)
- `frontend/.../features/projects/components/project-detail/project-detail.html` -- Thêm gantt section + header gantt-controls
- `frontend/.../features/projects/components/project-detail/project-detail.scss` -- `.view-gantt` full-height layout + split panel styles
- `frontend/.../features/gantt/components/gantt/gantt.ts` -- Xóa (không còn route)
- `frontend/.../features/gantt/components/gantt/gantt.html` -- Xóa
- `frontend/.../features/gantt/components/gantt/gantt.scss` -- Xóa

## Tasks & Acceptance

**Execution:**
- [ ] `frontend/.../features/projects/projects.routes.ts` -- Xóa entry `{ path: ':projectId/gantt', loadComponent: () => import('../gantt/...' }.then(m => m.GanttComponent) }` -- route không còn dùng
- [ ] `frontend/.../features/projects/components/project-detail/project-detail.ts` -- Merge gantt logic với các điểm sau:
  - `currentView` → `signal<'grid' | 'board' | 'gantt'>('grid')`
  - Inject thêm: `Store<AppState>`, `DestroyRef`, `TasksApiService`, `DOCUMENT`; `@ViewChild('leftContainer', { static: false }) leftContainer!: ElementRef<HTMLElement>`
  - Thêm observables: `ganttTasks$`, `ganttLoading$`, `ganttError$`, `ganttGranularity$`, `ganttDirtyCount$`, `ganttSaving$`, `ganttConflict$` từ gantt selectors
  - Thêm signals: `ganttCriteria = signal<FilterCriteria>({})`, `ganttVisibleMap = signal<Map<string,boolean>|null>(null)`, `ganttScrollTop = 0`
  - **ngOnInit**: (a) detect `qp['view']==='gantt'` → set currentView + dispatch load; (b) `ganttTasks$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(tasks => recomputeGanttVisibleMap())`; (c) `ganttConflict$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(c => { if (c && currentView()==='gantt') openGanttConflictDialog(c); })`
  - **ngOnDestroy**: thêm `GanttActions.clearGantt()`, `doc.removeEventListener` x2, reset `doc.body.style.cursor/userSelect`
  - **switchView**: gantt → set + dispatch load; rời gantt → dispatch clearGantt trước khi set view
  - **openAddTaskDialog** (modify): `const ref = dialog.open(...); if (currentView()==='gantt') ref.afterClosed().subscribe(() => reloadGantt())`
  - Thêm methods: `openGanttEditTaskDialog(taskId: string)`, `deleteGanttTask(task: GanttTask)`, `onInlineEdit`, `onGanttTaskEdited`, `onGranularityChange`, `onGanttSave`, `onGanttDiscard`, `reloadGantt`, `recomputeGanttVisibleMap`, `ganttTaskMatches`, `collectGanttDescendants`, `propagateGanttUpward`, `openGanttConflictDialog`
  - Resize: `onResizeStart/Move/End`, `boundMouseMove/Up` arrow functions (để removeEventListener đúng)
  - Imports: `GanttLeftPanelComponent`, `GanttInlineEditEvent` (`../../gantt/components/gantt-left-panel/...`); `GanttTimelineComponent`; `GanttActions`; gantt selectors; `GanttConflictState`, `GanttGranularity`, `GanttTask`, `GanttTaskEdit`; `ConflictDialogComponent`, `ConfirmDialogComponent`; `TasksApiService`; `AppState`; `MatBadgeModule`, `MatTooltipModule`; `DestroyRef`, `ElementRef`, `ViewChild`, `DOCUMENT`; `takeUntilDestroyed`
- [ ] `frontend/.../features/projects/components/project-detail/project-detail.html` -- (1) `[class.view-gantt]="currentView()==='gantt'"` trên wrapper; (2) Trong `.header` thêm `@if (currentView()==='gantt')`: `<div class="header-sep">` + granularity toggle + dirty save/discard buttons (trước nút Thêm Task); (3) Thêm block `@if (currentView()==='gantt')` ngoài tasks$ block: deadline banner (`ganttDeadlineCounts$`) + filter bar (`ganttCriteria()`, `members()`) + split panel (`#leftContainer`, resize handle, timeline) + empty state + loading overlay. Bind: `(editTask)="openGanttEditTaskDialog($event)"`, `(deleteTask)="deleteGanttTask($event)"`, `(addChild)="openAddTaskDialog($event)"`
- [ ] `frontend/.../features/projects/components/project-detail/project-detail.scss` -- Thêm: `.project-detail.view-gantt { height: 100vh; padding: 0; overflow: hidden; display: flex; flex-direction: column; .header { margin-bottom: 0; flex-shrink: 0; padding: 0 24px; height: 64px; border-bottom: 1px solid var(--border-color-light); background: var(--surface-card, #fff); } }`; `.gantt-split-panel { flex: 1; min-height: 0; overflow: hidden; display: flex; }`; copy `.gantt-left-container`, `.gantt-resize-handle`, `.gantt-right-container`, `.gantt-loading-overlay`, `.gantt-empty`, `.gantt-error` từ `gantt.scss`; `.header-sep { width: 1px; height: 20px; background: var(--border-color-light); margin: 0 4px; }`
- [ ] `frontend/.../features/gantt/components/gantt/gantt.ts` + `gantt.html` + `gantt.scss` -- Xóa 3 files

**Acceptance Criteria:**
- Given Grid view, khi click "Gantt" toggle, then URL=`?view=gantt`, split panel hiện full-height, header vẫn hiển thị với granularity toggle + save/discard (ẩn khi không dirty)
- Given Gantt view, khi click "Grid"/"Board", then switch mượt, gantt state cleared, layout trở về scrollable
- Given URL `?view=gantt` khi load, then gantt data load và split panel hiện đúng
- Given navigate tới `/projects/:id/gantt` (URL cũ), then redirect login
- Given switch giữa 3 views bất kỳ, then header (back + title + toggle) luôn nhất quán
- Given đang resize + navigate rời project, then cursor không kẹt `col-resize`
- Given thêm task từ gantt view, then gantt reload tự động sau dialog close

## Design Notes

**Layout gantt-mode** — `height: 100vh` trên `.project-detail.view-gantt` hoạt động vì `mat-sidenav-content` cũng có chiều cao viewport; không có scroll thừa. Split panel dùng `flex: 1; min-height: 0` fill phần còn lại sau header 64px.

**Subscriptions** — Tất cả `subscribe()` trong `ngOnInit` phải dùng `takeUntilDestroyed(this.destroyRef)`. Đây là điểm quan trọng nhất khi merge vào long-lived component.

**Members reuse** — `members()` signal đã load 1 lần trong ngOnInit cho grid/board. Dùng lại cho gantt filter bar, không cần gọi API thêm.

**Conflict dialog guard** — Subscription `ganttConflict$` phải kiểm tra `currentView() === 'gantt'` trước khi mở dialog, tránh stale conflict fire sau khi đã rời gantt view.

## Spec Change Log

## Verification

**Commands:**
- `cd frontend/project-management-web && npx ng build --configuration development 2>&1 | tail -5` -- expected: Build successful, 0 errors

**Manual checks:**
- `/projects/:id` → click Gantt → split panel full-height, URL=`?view=gantt`
- Click Grid từ gantt → layout scroll bình thường, header title hiện
- Click Board từ gantt → board hiện, không navigate ra ngoài
- F5 tại `?view=gantt` → gantt load lại đúng
- Resize split panel → rời project → cursor reset
