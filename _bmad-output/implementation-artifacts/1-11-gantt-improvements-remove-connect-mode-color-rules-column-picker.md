# Story 1.11: Gantt Improvements — Remove Connect Mode + Clarify Color Rules + Column Picker

Status: review

**Story ID:** 1.11
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 4
**Date Created:** 2026-04-27

---

## Story

As a PM,
I want Gantt view được dọn dẹp UI (bỏ tính năng nối task không dùng), phân màu bar rõ ràng và có thể tùy chỉnh cột hiển thị ở left panel,
So that trải nghiệm Gantt gọn gàng hơn và mỗi PM có thể xem đúng thông tin cần thiết theo workflow của mình.

## Acceptance Criteria

### Phần A — Bỏ Connect Mode (Dependency Arrows)

1. **Given** user mở trang Gantt
   **When** trang render
   **Then** KHÔNG còn endpoint circles (drag handles) ở đầu/cuối các bar
   **And** KHÔNG còn SVG dependency arrows giữa các bar
   **And** KHÔNG còn connect mode button/toggle trên toolbar

2. **Given** code `gantt-timeline.ts`
   **When** dev xem code
   **Then** các thành phần sau đã bị xóa hoàn toàn: `ConnectState` interface, `connectMode` input, `dependencyAdded` output, `dependencyRemoved` output, methods `onEndpointMouseDown`, `onEndpointMouseUp`, `onConnectMove`, `onConnectUp`, `onDependencyClick`, `clientToSvg`, ViewChild `connectSvgLine`, method `buildDependencyPaths()`, interface `DependencyPath`

3. **Given** code `gantt-timeline.service.ts`
   **When** dev xem code
   **Then** method `calculateArrowPath()` đã bị xóa

4. **Given** code `gantt.model.ts`
   **When** dev xem code
   **Then** `GanttDependency` interface, `DependencyType` type, field `predecessors: GanttDependency[]` trên `GanttTask`, field `newPredecessors?: GanttDependency[]` trên `GanttTaskEdit` đã bị xóa

5. **Given** `GanttAdapterService.adapt()`
   **When** chạy adapter
   **Then** KHÔNG còn map `t.predecessors` vào GanttTask (bỏ dòng predecessors trong object trả về)
   **And** bỏ import `DependencyType`, `GanttDependency` khỏi adapter

6. **Given** `gantt.ts` (parent component)
   **When** dev xem code
   **Then** không còn binding `[connectMode]`, `(dependencyAdded)`, `(dependencyRemoved)` nào tới GanttTimelineComponent

7. **Given** backend API vẫn trả `predecessors[]` trong ProjectTask response
   **When** adapter nhận data
   **Then** field `predecessors` trên `ProjectTask` model giữ nguyên (KHÔNG xóa khỏi backend model)
   **And** chỉ bỏ việc map nó vào `GanttTask`

8. **Given** Gantt render 100 tasks sau khi xóa dependency arrows
   **When** đo render time
   **Then** không chậm hơn so với trước (expected: nhanh hơn do bỏ SVG path calculation)

### Phần B — Làm rõ Color Rules

9. **Given** function `getBarColor()` trong `gantt-timeline.service.ts`
   **When** dev xem code và test
   **Then** logic áp dụng đúng priority matrix theo thứ tự:
   1. `type === 'Phase'` → `#2196F3` (xanh dương) — bất kể status
   2. `type === 'Milestone'` → `#FF9800` (cam) — bất kể status
   3. `status === 'Completed'` → `#9E9E9E` (xám)
   4. `status === 'Delayed'` → `#F44336` (đỏ)
   5. Mọi trường hợp còn lại → `#4CAF50` (xanh lá)

10. **Given** edge case: Phase với status Delayed
    **When** `getBarColor()` được gọi
    **Then** trả `#2196F3` (Phase ưu tiên — type trumps status)

11. **Given** edge case: Milestone với status Completed
    **When** `getBarColor()` được gọi
    **Then** trả `#FF9800` (Milestone ưu tiên)

12. **Given** edge case: task với status null hoặc undefined
    **When** `getBarColor()` được gọi
    **Then** trả `#4CAF50` (fallback xanh lá, không throw error)

13. **Given** unit tests `gantt-timeline.service.spec.ts`
    **When** chạy test
    **Then** có test case cho tất cả 8 scenarios: Phase, Milestone, Completed, Delayed, NotStarted, InProgress, OnHold, Cancelled + 2 edge cases (Phase+Delayed, Milestone+Completed) + null status fallback

### Phần C — Column Picker cho Gantt Left Panel

14. **Given** shared service `ColumnPickerService` được tạo
    **When** dev inject service
    **Then** service expose:
    - `loadColumns(config: ColumnPickerConfig): void` — đọc từ localStorage, fallback về `defaultVisible`
    - `toggleColumn(componentId: string, columnId: string): void` — cập nhật state và lưu localStorage ngay, bỏ qua nếu column có `required: true`
    - `getVisibleColumnIds(componentId: string): string[]` — trả mảng id các cột đang visible
    - `getGridTemplate(componentId: string, allCols: ColumnDef[]): string` — trả CSS grid template string
    - `resetColumns(componentId: string, defaults: ColumnDef[]): void` — xóa localStorage, reset về defaults
    - localStorage key format: `column-visibility-{componentId}`

15. **Given** shared component `ColumnPickerComponent` được tạo
    **When** component render
    **Then** hiển thị checkbox list cho tất cả columns nhận được qua `@Input()`
    **And** columns có `required: true` render disabled (không thể uncheck)
    **And** có nút "Mặc định" để reset về defaultVisible
    **And** thay đổi checkbox được gọi ngay `service.toggleColumn()`

16. **Given** user mở Gantt left panel
    **When** trang render
    **Then** có icon trigger `tune` (Material Icons) ở header panel (bên cạnh tiêu đề)
    **And** click icon → dropdown/popover hiển thị ColumnPickerComponent

17. **Given** Gantt left panel columns được cấu hình
    **When** user lần đầu vào (không có localStorage)
    **Then** 4 cột mặc định VISIBLE: `name` (required), `status`, `assignee`, `plannedEnd`
    **And** 4 cột mặc định HIDDEN: `type`, `priority`, `plannedStart`, `percentComplete`

18. **Given** user toggle một cột trong column picker
    **When** check/uncheck
    **Then** grid layout thay đổi ngay (CSS grid template cập nhật)
    **And** trạng thái được lưu vào localStorage key `column-visibility-gantt-left-panel`
    **And** sau khi refresh, trạng thái được khôi phục đúng

19. **Given** column `name` trong Gantt left panel
    **When** user thấy column picker
    **Then** checkbox của `name` bị disabled (cannot uncheck)
    **And** `name` column luôn hiển thị

20. **Given** cột `assignee` visible trong Gantt left panel
    **When** task có `assigneeUserId`
    **Then** hiển thị display name của assignee (resolve từ members list)
    **And** nếu không resolve được thì show truncated userId (4 chars + "...")

21. **Given** task-tree component (`task-tree.ts`)
    **When** dev xem code sau refactor
    **Then** inline column logic cũ đã được thay bằng `ColumnPickerService`
    **And** localStorage key migrate: nếu tồn tại `task-tree-columns-v1`, đọc một lần và ghi vào `column-visibility-task-tree`, xóa key cũ
    **And** public `@Input()` và `@Output()` API không thay đổi

## Tasks / Subtasks

### Task 1 — Xóa Connect Mode (AC: 1-8)

- [x] **1.1** Xóa khỏi `gantt.model.ts`:
  - Interface `GanttDependency`
  - Type `DependencyType`
  - Field `predecessors: GanttDependency[]` trong `GanttTask`
  - Field `newPredecessors?: GanttDependency[]` trong `GanttTaskEdit`

- [x] **1.2** Xóa khỏi `gantt-timeline.ts`:
  - Interface `ConnectState` và `DependencyPath`
  - `@Input() connectMode = false`
  - `@Output() dependencyAdded` và `dependencyRemoved`
  - `@ViewChild('connectSvgLine')` và biến `connectLine`
  - Methods: `onEndpointMouseDown`, `onEndpointMouseUp`, `onConnectMove`, `onConnectUp`, `onDependencyClick`, `clientToSvg`
  - Method `buildDependencyPaths()` và field `dependencyPaths`
  - Bỏ `dependencyPaths` khỏi `recalculate()`

- [x] **1.3** Xóa khỏi `gantt-timeline.service.ts`:
  - Method `calculateArrowPath()`

- [x] **1.4** Cập nhật `gantt-timeline.html`:
  - Xóa SVG `<line>` connect element (`connectSvgLine`)
  - Xóa SVG `<path>` dependency arrows (loop qua `dependencyPaths`)
  - Xóa endpoint circles (drag handle dots) ở đầu/cuối bars
  - Xóa event bindings `(mousedown)="onEndpointMouseDown(...)"` và `(mouseup)="onEndpointMouseUp(...)"`
  - Xóa click handler `(click)="onDependencyClick(dep)"` trên arrows

- [x] **1.5** Cập nhật `gantt-adapter.service.ts`:
  - Xóa dòng `predecessors: t.predecessors.map(...)` trong `adapt()`
  - Xóa import `DependencyType`, `GanttDependency` (không còn cần)
  - **GIỮ NGUYÊN** `t.predecessors` field trong `ProjectTask` model — không chạm vào backend models

- [x] **1.6** Cập nhật `gantt.ts` (parent component):
  - Xóa import `GanttDependency` nếu không dùng ở nơi khác
  - Xóa binding `[connectMode]`, `(dependencyAdded)`, `(dependencyRemoved)` khỏi template gantt.html

- [x] **1.7** Cập nhật `gantt-adapter.service.spec.ts` nếu có test cho predecessors mapping

### Task 2 — Làm rõ Color Rules (AC: 9-13)

- [x] **2.1** Cập nhật `getBarColor()` trong `gantt-timeline.service.ts`:
  ```typescript
  getBarColor(task: GanttTask): string {
    if (task.type === 'Phase')     return '#2196F3';
    if (task.type === 'Milestone') return '#FF9800';
    if (task.status === 'Completed') return '#9E9E9E';
    if (task.status === 'Delayed')   return '#F44336';
    return '#4CAF50';
  }
  ```
  Logic này đã gần đúng — chỉ cần điều chỉnh thứ tự: `Completed` trước `Delayed`.

- [x] **2.2** Cập nhật `gantt-timeline.service.spec.ts` — thêm/update test coverage đầy đủ:
  - `type Phase` → `#2196F3` (bất kể status)
  - `type Milestone` → `#FF9800` (bất kể status)
  - `type Task, status Completed` → `#9E9E9E`
  - `type Task, status Delayed` → `#F44336`
  - `type Task, status NotStarted` → `#4CAF50`
  - `type Task, status InProgress` → `#4CAF50`
  - `type Task, status OnHold` → `#4CAF50`
  - `type Task, status Cancelled` → `#4CAF50`
  - `type Phase, status Delayed` → `#2196F3` (Phase trumps Delayed)
  - `type Milestone, status Completed` → `#FF9800` (Milestone trumps Completed)
  - `status null/undefined` → `#4CAF50` (fallback, no error)

### Task 3 — Tạo Shared ColumnPickerService (AC: 14)

- [x] **3.1** Tạo `src/app/shared/services/column-picker.service.ts`:
  ```typescript
  export interface ColumnDef {
    id: string;
    label: string;
    defaultVisible: boolean;
    required?: boolean;
  }

  export interface ColumnPickerConfig {
    componentId: string;
    columns: ColumnDef[];
  }

  @Injectable({ providedIn: 'root' })
  export class ColumnPickerService {
    private state = new Map<string, Set<string>>();

    private storageKey(componentId: string): string {
      return `column-visibility-${componentId}`;
    }

    loadColumns(config: ColumnPickerConfig): void {
      const saved = localStorage.getItem(this.storageKey(config.componentId));
      if (saved) {
        try {
          const parsed: Record<string, boolean> = JSON.parse(saved);
          const visible = new Set(config.columns
            .filter(c => c.required || parsed[c.id] !== false)
            .map(c => c.id));
          this.state.set(config.componentId, visible);
          return;
        } catch { /* use defaults */ }
      }
      this.state.set(config.componentId,
        new Set(config.columns.filter(c => c.defaultVisible || c.required).map(c => c.id)));
    }

    toggleColumn(componentId: string, columnId: string, columns: ColumnDef[]): void {
      const col = columns.find(c => c.id === columnId);
      if (col?.required) return; // required columns cannot be toggled
      const visible = this.state.get(componentId) ?? new Set();
      const next = new Set(visible);
      next.has(columnId) ? next.delete(columnId) : next.add(columnId);
      this.state.set(componentId, next);
      this.save(componentId, columns);
    }

    getVisibleColumnIds(componentId: string): string[] {
      return [...(this.state.get(componentId) ?? new Set())];
    }

    isVisible(componentId: string, columnId: string): boolean {
      return this.state.get(componentId)?.has(columnId) ?? false;
    }

    getGridTemplate(componentId: string, columns: ColumnDef[], colWidths: Record<string, string>): string {
      const visible = this.state.get(componentId) ?? new Set();
      return columns.filter(c => visible.has(c.id)).map(c => colWidths[c.id] ?? '1fr').join(' ');
    }

    resetColumns(componentId: string, columns: ColumnDef[]): void {
      this.state.set(componentId,
        new Set(columns.filter(c => c.defaultVisible || c.required).map(c => c.id)));
      localStorage.removeItem(this.storageKey(componentId));
    }

    private save(componentId: string, columns: ColumnDef[]): void {
      const visible = this.state.get(componentId) ?? new Set();
      const obj: Record<string, boolean> = {};
      columns.forEach(c => { obj[c.id] = visible.has(c.id); });
      try { localStorage.setItem(this.storageKey(componentId), JSON.stringify(obj)); } catch { /* ignore */ }
    }
  }
  ```

- [x] **3.2** Tạo `src/app/shared/services/column-picker.service.spec.ts`:
  - Test `loadColumns`: fresh (no localStorage) → defaultVisible, with saved localStorage → restore, with corrupted JSON → fallback defaults
  - Test `toggleColumn`: toggle non-required col → state flips + localStorage updated, toggle required col → no change
  - Test `getVisibleColumnIds`: returns correct array
  - Test `isVisible`: true/false correctly
  - Test `getGridTemplate`: returns correct CSS string
  - Test `resetColumns`: clears localStorage + restores defaults

### Task 4 — Tạo Shared ColumnPickerComponent (AC: 15)

- [x] **4.1** Tạo `src/app/shared/components/column-picker/column-picker.component.ts` (standalone):
  ```typescript
  @Component({
    selector: 'app-column-picker',
    standalone: true,
    imports: [FormsModule, MatCheckboxModule, MatButtonModule, MatIconModule],
    templateUrl: './column-picker.component.html',
    styleUrl: './column-picker.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
  })
  export class ColumnPickerComponent {
    @Input() componentId = '';
    @Input() columns: ColumnDef[] = [];
    @Output() changed = new EventEmitter<void>();

    private readonly service = inject(ColumnPickerService);
    private readonly cdr = inject(ChangeDetectorRef);

    isVisible(colId: string): boolean {
      return this.service.isVisible(this.componentId, colId);
    }

    toggle(colId: string): void {
      this.service.toggleColumn(this.componentId, colId, this.columns);
      this.changed.emit();
      this.cdr.markForCheck();
    }

    reset(): void {
      this.service.resetColumns(this.componentId, this.columns);
      this.changed.emit();
      this.cdr.markForCheck();
    }
  }
  ```

- [x] **4.2** Tạo template `column-picker.component.html`:
  ```html
  <div class="col-picker-panel">
    <div class="col-picker-header">
      <span>Tùy chỉnh cột</span>
      <button mat-button (click)="reset()">Mặc định</button>
    </div>
    <div class="col-picker-list">
      @for (col of columns; track col.id) {
        <label class="col-picker-item" [class.required]="col.required">
          <input type="checkbox"
                 [checked]="isVisible(col.id)"
                 [disabled]="col.required === true"
                 (change)="toggle(col.id)" />
          {{ col.label }}
        </label>
      }
    </div>
  </div>
  ```

- [x] **4.3** Tạo `column-picker.component.scss` với style phù hợp (panel nhỏ gọn, max-width 200px, border, shadow)

- [x] **4.4** Tạo `column-picker.component.spec.ts`:
  - Test required column renders disabled
  - Test toggle emits `changed`
  - Test reset emits `changed`

### Task 5 — Tích hợp Column Picker vào Gantt Left Panel (AC: 16-20)

- [x] **5.1** Cập nhật `gantt-left-panel.ts`:
  - Inject `ColumnPickerService`, `ChangeDetectorRef`
  - Định nghĩa column config:
    ```typescript
    readonly COMPONENT_ID = 'gantt-left-panel';
    readonly COLUMNS: ColumnDef[] = [
      { id: 'name',        label: 'Tên task',         defaultVisible: true,  required: true },
      { id: 'status',      label: 'Trạng thái',        defaultVisible: true  },
      { id: 'assignee',    label: 'Người thực hiện',   defaultVisible: true  },
      { id: 'plannedEnd',  label: 'KH Kết thúc',       defaultVisible: true  },
      { id: 'type',        label: 'Loại',              defaultVisible: false },
      { id: 'priority',    label: 'Ưu tiên',            defaultVisible: false },
      { id: 'plannedStart',label: 'KH Bắt đầu',        defaultVisible: false },
      { id: 'percent',     label: '% Hoàn thành',      defaultVisible: false },
    ];
    readonly colPickerOpen = signal(false);
    ```
  - Thêm `@Input() members: ProjectMember[] = []`
  - Trong constructor: `this.columnPickerService.loadColumns({ componentId: this.COMPONENT_ID, columns: this.COLUMNS })`
  - Methods: `isColVisible(id: string)`, `toggleColPicker()`, `onColumnChanged()`
  - `assigneeLabel(userId: string | null)` — resolve displayName từ members, fallback userId.substring(0,4) + '...'

- [x] **5.2** Thêm CSS grid cho gantt-left-panel dựa theo visible columns:
  ```typescript
  get gridCols(): string {
    return this.columnPickerService.getGridTemplate(this.COMPONENT_ID, this.COLUMNS, {
      name:         '1fr',
      status:       '110px',
      assignee:     '100px',
      plannedEnd:   '76px',
      type:         '60px',
      priority:     '80px',
      plannedStart: '76px',
      percent:      '44px',
    });
  }
  ```

- [x] **5.3** Cập nhật `gantt-left-panel.html`:
  - Header: thêm trigger button `<button mat-icon-button (click)="toggleColPicker()" matTooltip="Tùy chỉnh cột"><mat-icon>tune</mat-icon></button>`
  - Thêm `<app-column-picker>` với `@if (colPickerOpen())` (click outside để đóng)
  - Wrap từng column cell trong `@if (isColVisible('...'))` tương ứng
  - Apply `[style.grid-template-columns]="gridCols"` vào header và row elements
  - Thêm cột `assignee`: `{{ assigneeLabel(task.assigneeUserId) }}`

- [x] **5.4** Cập nhật `gantt-left-panel.scss`:
  - Thêm styles cho column picker trigger
  - Thêm `.col-type`, `.col-priority`, `.col-assignee`, `.col-planned-end`, `.col-planned-start`

- [x] **5.5** Cập nhật `gantt.ts` (parent): pass members vào `<app-gantt-left-panel>`:
  - Đã có `MembersApiService` inject — dùng `this.membersApi` hoặc từ store để lấy members
  - Thêm `members = signal<ProjectMember[]>([])` và load khi project load
  - Binding: `[members]="members()"`

### Task 6 — Refactor task-tree để dùng shared ColumnPickerService (AC: 21)

- [x] **6.1** Cập nhật `task-tree.ts`:
  - Inject `ColumnPickerService`
  - Thêm `readonly COMPONENT_ID = 'task-tree'`
  - Định nghĩa lại `COLUMNS` sử dụng `ColumnDef` interface từ shared service
  - Constructor: migrate localStorage nếu tìm thấy key cũ:
    ```typescript
    const oldKey = 'task-tree-columns-v1';
    const legacy = localStorage.getItem(oldKey);
    if (legacy) {
      localStorage.setItem(`column-visibility-task-tree`, legacy);
      localStorage.removeItem(oldKey);
    }
    this.columnPickerService.loadColumns({ componentId: this.COMPONENT_ID, columns: this.COLUMNS });
    ```
  - Replace inline `visibleCols: Set<string>` với `isColVisible(id: string)` từ service
  - Replace `toggleCol()` với `this.columnPickerService.toggleColumn(this.COMPONENT_ID, key, this.COLUMNS)`
  - Replace `gridCols` getter với `this.columnPickerService.getGridTemplate(...)`
  - Xóa `loadColVisibility()`, `saveColVisibility()`, `colPickerOpen = signal(false)` inline — thay bằng service/component
  - Đưa `<app-column-picker>` vào template thay cho picker logic inline hiện tại
  - **KHÔNG thay đổi bất kỳ `@Input()` hay `@Output()` nào**

- [x] **6.2** Cập nhật `task-tree.html`:
  - Thay picker trigger + dropdown cũ bằng `<app-column-picker [componentId]="COMPONENT_ID" [columns]="COLUMNS" (changed)="cdr.markForCheck()">`
  - Cập nhật `@if` conditions dùng `isColVisible('...')` thay vì `visibleCols.has('...')`

- [x] **6.3** Cập nhật `task-tree.spec.ts`:
  - Mock `ColumnPickerService`
  - Đảm bảo tất cả existing tests vẫn pass

### Task 7 — Tests tổng thể (AC: tất cả)

- [x] **7.1** Chạy `ng test --watch=false` → mục tiêu 0 failures mới
- [x] **7.2** Pre-existing failures: `app.spec.ts` và `login.spec.ts` (9 tests — Vitest+Angular templateUrl issue từ Story 1.5) — bỏ qua, không tính
- [x] **7.3** Đảm bảo `gantt-timeline.service.spec.ts` pass tất cả cases màu mới
- [x] **7.4** Đảm bảo `gantt-adapter.service.spec.ts` pass (nếu có — update để không expect predecessors)
- [x] **7.5** Đảm bảo `column-picker.service.spec.ts` pass 100%
- [x] **7.6** Đảm bảo `column-picker.component.spec.ts` pass 100%

## Dev Notes

### ⚠️ Những Lỗi Phải Tránh (Học từ Stories trước)

1. **KHÔNG dùng `createFeature`** — type error, dùng `createReducer` trực tiếp (Story 1.4 lesson)
2. **KHÔNG dùng `*ngIf`/`*ngFor`** — Angular 17+ dùng `@if`/`@for` (project convention)
3. **KHÔNG hardcode màu** — dùng CSS variables `--table-header-bg`, `--border-color-light`, `--docker-blue-light`, `--text-primary` (Story 1.10 lesson)
4. **OnPush + state change** — luôn gọi `cdr.markForCheck()` sau khi thay đổi state trong OnPush component
5. **KHÔNG xóa `predecessors` khỏi `ProjectTask` model** — chỉ bỏ mapping trong adapter; backend API giữ nguyên
6. **Signal cho picker open state** — dùng `signal(false)` + `.set()`, không dùng plain boolean với OnPush
7. **Subscriptions** — dùng `takeUntilDestroyed()` từ `@angular/core/rxjs-interop`

### Existing Patterns — REUSE, ĐỪNG VIẾT LẠI

| Pattern | Location | Dùng cho |
|---|---|---|
| `ColumnDef`, inline column picker logic | `task-tree.ts` | Tham khảo để extract vào shared service |
| `signal(false)` picker open state | `task-tree.ts:103` | Dùng tương tự trong gantt-left-panel |
| `isColVisible` / `gridCols` getter | `task-tree.ts:138-153` | Copy pattern, refactor sang service call |
| `editable-cell`, `inline-input`, `inline-select` CSS | `gantt-left-panel.scss` | Đã có từ Story 1.10 |
| `assigneeLabel()` method | `task-tree.ts:355-360` | Copy sang gantt-left-panel |
| `MatIconModule` import | `gantt-left-panel.ts` | Đã có trong imports |
| `MatButtonModule` import | `gantt-left-panel.ts` | Đã có trong imports |
| `MatTooltipModule` import | `gantt-left-panel.ts` | Đã có trong imports |
| `MembersApiService` | `gantt.ts:77` | Đã inject trong parent — pass members qua @Input |
| `ProjectMember` model | `features/projects/models/project.model.ts` | Dùng cho members input |
| `ConfirmDialogComponent` pattern | `shared/components/confirm-dialog/` | Template cho ColumnPickerComponent |
| Click outside to close | Có thể dùng `@HostListener` hoặc overlay CDK | Đóng column picker khi click ngoài |

### File Structure — Đầy đủ danh sách files cần thay đổi

```
frontend/project-management-web/src/app/
│
├── shared/
│   ├── services/
│   │   ├── column-picker.service.ts          ← NEW
│   │   └── column-picker.service.spec.ts     ← NEW
│   └── components/
│       └── column-picker/
│           ├── column-picker.component.ts    ← NEW (standalone)
│           ├── column-picker.component.html  ← NEW
│           ├── column-picker.component.scss  ← NEW
│           └── column-picker.component.spec.ts ← NEW
│
├── features/gantt/
│   ├── models/
│   │   └── gantt.model.ts                   ← MODIFY: xóa GanttDependency, DependencyType,
│   │                                              predecessors, newPredecessors
│   ├── services/
│   │   ├── gantt-adapter.service.ts         ← MODIFY: xóa predecessors mapping + imports
│   │   └── gantt-timeline.service.ts        ← MODIFY: xóa calculateArrowPath, update getBarColor
│   └── components/
│       ├── gantt/
│       │   ├── gantt.ts                     ← MODIFY: xóa connect bindings, thêm members signal
│       │   └── gantt.html                   ← MODIFY: xóa connect bindings, add [members]
│       ├── gantt-timeline/
│       │   ├── gantt-timeline.ts            ← MODIFY: xóa toàn bộ connect mode code
│       │   ├── gantt-timeline.html          ← MODIFY: xóa SVG arrows, endpoints, connect line
│       │   └── gantt-timeline.service.spec.ts ← MODIFY: thêm test cases màu đầy đủ
│       └── gantt-left-panel/
│           ├── gantt-left-panel.ts          ← MODIFY: thêm column picker, members input
│           ├── gantt-left-panel.html        ← MODIFY: column picker trigger + @if visibility
│           └── gantt-left-panel.scss        ← MODIFY: styles cho picker + columns mới
│
└── features/projects/components/
    └── task-tree/
        ├── task-tree.ts                     ← MODIFY: refactor sang ColumnPickerService
        ├── task-tree.html                   ← MODIFY: dùng ColumnPickerComponent
        └── task-tree.spec.ts               ← MODIFY: mock ColumnPickerService
```

### Thứ tự Implement Khuyến Nghị

1. **Bắt đầu từ Task 1** (xóa connect mode) — isolated, ít risk, test ngay
2. **Task 2** (fix color rules) — 2 dòng code + tests
3. **Task 3** (ColumnPickerService) — pure logic, test riêng dễ
4. **Task 4** (ColumnPickerComponent) — UI đơn giản
5. **Task 5** (Gantt left panel integration) — phức tạp nhất
6. **Task 6** (task-tree refactor) — cuối cùng, sau khi shared service đã ổn định
7. **Task 7** (run all tests)

### Members trong Gantt Left Panel

`gantt.ts` đã inject `MembersApiService` và có:
```typescript
private readonly membersApi = inject(MembersApiService);
```

Thêm vào `ngOnInit`:
```typescript
this.membersApi.getMembers(this.projectId).subscribe(members => {
  this.members.set(members);
});
```

Pass xuống left panel:
```html
<app-gantt-left-panel
  [tasks]="tasks"
  [members]="members()"
  ... />
```

### ColumnPickerComponent — Click Outside Pattern

Dùng đơn giản với host listener hoặc CDK overlay. Option đơn giản nhất:

```typescript
@HostListener('document:click', ['$event'])
onDocumentClick(event: MouseEvent): void {
  if (!this.el.nativeElement.contains(event.target)) {
    this.colPickerOpen.set(false);
    this.cdr.markForCheck();
  }
}
```

Inject `ElementRef` trong gantt-left-panel để có `this.el`.

### Scope rõ ràng

**TRONG story này:**
- Xóa connect mode (UI + code + model)
- Clarify color rules + tests
- Tạo shared ColumnPickerService + ColumnPickerComponent
- Thêm column picker vào Gantt left panel
- Refactor task-tree dùng shared service
- Thêm cột assignee vào Gantt left panel (cần members input)

**KHÔNG làm trong story này:**
- Backend API changes (không xóa predecessor field)
- Database migration
- Thay đổi task-tree @Input/@Output API
- Thêm inline editing cho assignee trong Gantt
- Resize/reorder columns (chỉ show/hide)
- SSO/auth changes
- Bất kỳ Epic 2-8 features nào

### References

- PRD Gantt Update: [_bmad-output/planning-artifacts/prd-gantt-update.md](_bmad-output/planning-artifacts/prd-gantt-update.md)
- Existing gantt model: [frontend/.../gantt/models/gantt.model.ts](frontend/project-management-web/src/app/features/gantt/models/gantt.model.ts)
- Existing color logic: [frontend/.../gantt/services/gantt-timeline.service.ts#L141](frontend/project-management-web/src/app/features/gantt/services/gantt-timeline.service.ts)
- Connect mode code (to delete): [frontend/.../gantt/components/gantt-timeline/gantt-timeline.ts#L26-L308](frontend/project-management-web/src/app/features/gantt/components/gantt-timeline/gantt-timeline.ts)
- task-tree column picker pattern: [frontend/.../projects/components/task-tree/task-tree.ts#L27-L153](frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.ts)
- Story 1.10 (Gantt inline edit patterns): [_bmad-output/implementation-artifacts/1-10-gantt-ui-consistency-inline-edit.md](_bmad-output/implementation-artifacts/1-10-gantt-ui-consistency-inline-edit.md)
- Architecture decisions: [_bmad-output/planning-artifacts/architecture.md](_bmad-output/planning-artifacts/architecture.md)

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Removed `GanttDependency` from model cascaded to 6 additional store/spec files (gantt.actions, gantt.reducer, gantt.effects, gantt.reducer.spec, gantt-adapter.service.spec)
- Pre-existing `deadline-alert.service.spec.ts` had broken Date mock using `jest.spyOn(global, 'Date')` which caused TypeScript compilation failure; fixed to `vi.spyOn(globalThis, 'Date')` to allow all tests to run. 3 Date-mock tests still fail at runtime due to fundamentally broken mock implementation (pre-existing).
- `gridCols` getter in `task-tree.ts` built manually (not via `getGridTemplate`) because `name` column position is fixed between `vbs` and `priority` in the column order, whereas `getGridTemplate` appends all visible columns in COLUMNS array order.

### Completion Notes List

- All 7 tasks completed: connect mode removed, color rules fixed, shared ColumnPickerService + ColumnPickerComponent created, integrated into gantt-left-panel, task-tree refactored
- Test results: 152 pass, 3 fail (all 3 in pre-existing deadline-alert.service.spec.ts — broken Date mock, not introduced by this story)
- localStorage migration: `task-tree-columns-v1` → `column-visibility-task-tree` implemented in constructor
- `members` @Input added to GanttLeftPanelComponent; passed from gantt.ts via `[members]="ganttMembers()"`

### File List

- `frontend/project-management-web/src/app/features/gantt/models/gantt.model.ts`
- `frontend/project-management-web/src/app/features/gantt/services/gantt-timeline.service.ts`
- `frontend/project-management-web/src/app/features/gantt/services/gantt-timeline.service.spec.ts`
- `frontend/project-management-web/src/app/features/gantt/services/gantt-adapter.service.ts`
- `frontend/project-management-web/src/app/features/gantt/services/gantt-adapter.service.spec.ts`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-timeline/gantt-timeline.ts`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-timeline/gantt-timeline.html`
- `frontend/project-management-web/src/app/features/gantt/components/gantt/gantt.ts`
- `frontend/project-management-web/src/app/features/gantt/components/gantt/gantt.html`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-left-panel/gantt-left-panel.ts`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-left-panel/gantt-left-panel.html`
- `frontend/project-management-web/src/app/features/gantt/components/gantt-left-panel/gantt-left-panel.scss`
- `frontend/project-management-web/src/app/features/gantt/store/gantt.actions.ts`
- `frontend/project-management-web/src/app/features/gantt/store/gantt.reducer.ts`
- `frontend/project-management-web/src/app/features/gantt/store/gantt.reducer.spec.ts`
- `frontend/project-management-web/src/app/features/gantt/store/gantt.effects.ts`
- `frontend/project-management-web/src/app/shared/services/column-picker.service.ts` (NEW)
- `frontend/project-management-web/src/app/shared/services/column-picker.service.spec.ts` (NEW)
- `frontend/project-management-web/src/app/shared/components/column-picker/column-picker.component.ts` (NEW)
- `frontend/project-management-web/src/app/shared/components/column-picker/column-picker.component.html` (NEW)
- `frontend/project-management-web/src/app/shared/components/column-picker/column-picker.component.scss` (NEW)
- `frontend/project-management-web/src/app/shared/components/column-picker/column-picker.component.spec.ts` (NEW)
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.ts`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.html`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.scss`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.spec.ts`
- `frontend/project-management-web/src/app/features/projects/services/deadline-alert.service.spec.ts` (bugfix: jest→vi)
