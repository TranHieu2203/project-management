---
stepsCompleted: ['step-01-init', 'step-02-discovery', 'step-09-functional', 'step-12-complete']
inputDocuments: ['_bmad-output/planning-artifacts/prd.md', 'docs/DEMO-SCRIPT.md']
workflowType: 'prd'
briefCount: 0
researchCount: 0
brainstormingCount: 0
projectDocsCount: 2
classification:
  projectType: web_app
  domain: enterprise_project_management
  complexity: medium
  projectContext: brownfield
---

# Product Requirements Document — Gantt View Improvements

**Author:** HieuTV-Team-Project-Management
**Date:** 2026-04-27
**Loại:** Feature Amendment — bổ sung / sửa đổi tính năng Gantt hiện có
**Tham chiếu PRD gốc:** `_bmad-output/planning-artifacts/prd.md`

---

## Tóm tắt

Ba thay đổi tập trung vào Gantt view nhằm đơn giản hóa UI, làm rõ tiêu chí hiển thị, và tăng khả năng tùy chỉnh:

| # | Thay đổi | Tác động |
|---|---|---|
| 1 | Bỏ tính năng nối task (dependency arrows) | Simplification — loại bỏ code phức tạp |
| 2 | Làm rõ tiêu chí phân màu bar | Clarification — không thay đổi hành vi |
| 3 | Thêm custom column picker cho Gantt left panel | New feature — shared component với task-tree |

---

## Bối cảnh

Hệ thống project management Angular SPA (~20 người dùng nội bộ) đang có Gantt view dạng split-panel: task tree bên trái + SVG timeline bên phải. Gantt hiện có tính năng connect mode (kéo để tạo dependency giữa các task) và dependency arrows (SVG mũi tên FS/SS/FF/SF). Tính năng này chưa được sử dụng thực tế và gây phức tạp không cần thiết cho codebase.

---

## Thay đổi 1 — Loại bỏ Dependency Connection trên Gantt

### Mô tả

Bỏ hoàn toàn tính năng kéo-thả để tạo dependency giữa các task trên Gantt, và bỏ hiển thị dependency arrows (mũi tên SVG) giữa các bar.

### Lý do

- Tính năng drag-to-connect dễ tạo dependency ngẫu nhiên (lỗi UX)
- Dependency arrows trên timeline gây visual clutter, đặc biệt khi nhiều task chồng chéo
- PMs ưu tiên nhìn thấy *ai đang làm gì, task nào trễ* hơn là thấy dependency chain dạng mũi tên
- Dependency vẫn có thể quản lý qua task detail panel (hiển thị text "Phụ thuộc vào: X", "Ảnh hưởng đến: Y")

### Phạm vi thay đổi (Frontend)

**Xóa khỏi `gantt-timeline.ts`:**
- Interface `ConnectState`
- Input `connectMode`
- Output `dependencyAdded`, `dependencyRemoved`
- Methods: `onEndpointMouseDown`, `onEndpointMouseUp`, `onConnectMove`, `onConnectUp`, `onDependencyClick`, `clientToSvg`
- ViewChild `connectSvgLine`
- Method `buildDependencyPaths()`
- Interface `DependencyPath`

**Xóa khỏi `gantt-timeline.service.ts`:**
- Method `calculateArrowPath()`

**Xóa khỏi `gantt.model.ts`:**
- Interface `GanttDependency`
- Type `DependencyType`
- Field `predecessors: GanttDependency[]` trên `GanttTask`
- Field `newPredecessors?: GanttDependency[]` trên `GanttTaskEdit`

**Cập nhật `gantt.ts` (component cha):** Bỏ binding `connectMode`, `dependencyAdded`, `dependencyRemoved`.

**Cập nhật template `gantt-timeline.html`:** Bỏ SVG arrows, endpoint circles, connect line.

### Quyết định về Data Model

- **Backend:** Giữ nguyên field `predecessor` trên `ProjectTask` API — **không xóa field khỏi backend**
- **Frontend model:** Bỏ `predecessors[]` khỏi `GanttTask` (chỉ dùng trong Gantt view)
- **GanttAdapterService:** Không map predecessor data vào GanttTask

> Lý do giữ backend: Predecessor data có thể được hiển thị dưới dạng text trong task detail panel ở tương lai, và việc xóa backend cần migration phức tạp hơn.

### Acceptance Criteria

- AC1.1: Không còn endpoint circles (drag handle) trên Gantt bars
- AC1.2: Không còn SVG dependency arrows giữa các bars
- AC1.3: Không còn connect mode button/toggle trên Gantt toolbar
- AC1.4: `GanttTask` model không còn field `predecessors`
- AC1.5: Gantt render không có bất kỳ dependency-related DOM element nào
- AC1.6: `ng test --watch=false` → 0 failures sau khi xóa

---

## Thay đổi 2 — Làm rõ Tiêu chí Phân màu Gantt Bar

### Mô tả

Chuẩn hóa và tài liệu hóa rõ ràng logic phân màu bar trên Gantt timeline. Không thay đổi hành vi hiện tại — chỉ làm rõ quy tắc và xử lý các edge case còn mơ hồ.

### Priority Matrix (Decision Tree)

Logic áp dụng theo thứ tự ưu tiên từ trên xuống:

```
1. type === 'Phase'           → Xanh dương  #2196F3
2. type === 'Milestone'       → Cam          #FF9800  (diamond shape, không phải bar)
3. status === 'Completed'     → Xám          #9E9E9E
4. status === 'Delayed'       → Đỏ           #F44336
5. Mọi trường hợp còn lại    → Xanh lá      #4CAF50
   (NotStarted, InProgress, OnHold, Cancelled)
```

### Bảng màu đầy đủ

| Điều kiện | Hình dạng | Màu | Hex | Ghi chú |
|---|---|---|---|---|
| `type === 'Phase'` | Bar ngang | Xanh dương | `#2196F3` | Luôn xanh bất kể status |
| `type === 'Milestone'` | Diamond ◆ | Cam | `#FF9800` | Luôn cam bất kể status |
| `status === 'Completed'` | Bar ngang | Xám | `#9E9E9E` | Task thường — đã xong |
| `status === 'Delayed'` | Bar ngang | Đỏ | `#F44336` | Task thường — trễ hạn |
| `status === 'NotStarted'` | Bar ngang | Xanh lá | `#4CAF50` | Chưa bắt đầu |
| `status === 'InProgress'` | Bar ngang | Xanh lá | `#4CAF50` | Đang thực hiện |
| `status === 'OnHold'` | Bar ngang | Xanh lá | `#4CAF50` | Tạm dừng |
| `status === 'Cancelled'` | Bar ngang | Xanh lá | `#4CAF50` | Đã hủy |

### Xử lý Edge Cases

| Edge case | Hành vi |
|---|---|
| Phase + status Delayed | → Xanh dương (Phase ưu tiên — cấu trúc quan trọng hơn status tức thời) |
| Phase + status Completed | → Xanh dương (Phase ưu tiên) |
| Milestone + status Delayed | → Cam diamond (Milestone ưu tiên) |
| `plannedStart` hoặc `plannedEnd` là null | → Không vẽ bar (skip rendering) |
| Status là string không xác định | → Xanh lá (fallback mặc định) |

### Ghi chú thiết kế

- Màu sắc phân biệt **loại công việc** (type) và **trạng thái** (status) theo hai tầng:
  - Tầng 1 (ưu tiên cao): Phase và Milestone có màu cố định theo cấu trúc
  - Tầng 2: Task thường phân màu theo status
- `OnHold` và `Cancelled` hiện tại là xanh lá — chấp nhận vì 2 trạng thái này hiếm gặp và không cần cảnh báo trực quan riêng ở giai đoạn này

### Acceptance Criteria

- AC2.1: `getBarColor()` trả về đúng màu theo priority matrix trên, có test cho tất cả 8 cases
- AC2.2: Phase bar luôn xanh dương bất kể status (kể cả khi Phase.status = 'Delayed')
- AC2.3: Milestone luôn render dạng diamond cam
- AC2.4: Task null status → xanh lá (fallback)
- AC2.5: Unit test `gantt-timeline.service.spec.ts` cover 100% các cases trên

---

## Thay đổi 3 — Custom Column Picker cho Gantt Left Panel

### Mô tả

Thêm tính năng ẩn/hiện cột cho grid bên trái Gantt (gantt-left-panel), sử dụng shared service và component được tách ra từ logic hiện có ở `task-tree`. Column `name` là bắt buộc và không thể ẩn.

### Phân tích Shared Component

**Chỉ áp dụng shared column picker cho 2 components:**
- `task-tree` (màn `project/{id}`) — refactor để dùng shared service
- `gantt-left-panel` — tích hợp mới

**Không áp dụng cho:** `resource-list`, `rate-list`, `vendor-list`, `time-entry-list`, `audit-log` (MatTable với fixed columns, không có nhu cầu toggle), `timesheet-grid` (data entry matrix), `my-tasks` (card/grouped view).

### Kiến trúc — 2 Layer

```
Layer 1: ColumnPickerService (shared state & persistence)
  src/app/shared/services/column-picker.service.ts

Layer 2: ColumnPickerComponent (dumb UI component)
  src/app/shared/components/column-picker/column-picker.component.ts
```

### Interfaces

```typescript
// src/app/shared/services/column-picker.service.ts

export interface ColumnDef {
  id: string;
  label: string;
  defaultVisible: boolean;
  required?: boolean;   // true → luôn hiện, không toggle được
}

export interface ColumnPickerConfig {
  componentId: string;  // dùng làm localStorage key suffix
  columns: ColumnDef[];
}
```

**LocalStorage key format:** `column-visibility-{componentId}`
- task-tree: `column-visibility-task-tree`
- gantt-left-panel: `column-visibility-gantt-left-panel`

### Columns cho Gantt Left Panel

| Cột | ID | Default | Required |
|---|---|---|---|
| Tên task | `name` | ✅ Visible | ✅ Bắt buộc |
| Trạng thái | `status` | ✅ Visible | |
| Người thực hiện | `assignee` | ✅ Visible | |
| KH Kết thúc | `plannedEnd` | ✅ Visible | |
| Loại | `type` | Hidden | |
| Ưu tiên | `priority` | Hidden | |
| KH Bắt đầu | `plannedStart` | Hidden | |
| % Hoàn thành | `percentComplete` | Hidden | |

> **Lý do default 4 cột:** Gantt left panel hẹp (~350-400px). 4 cột mặc định đủ để PM biết *ai đang làm gì và deadline khi nào* mà không cần scroll.

### UX — Column Picker Trigger

| Context | Vị trí trigger | Icon |
|---|---|---|
| **Gantt left panel** | Inline cạnh header title panel | `tune` (Material Icons) |
| **Task-tree** | Top-right corner của grid header | `tune` (Material Icons) |

- Tooltip: *"Tùy chỉnh cột"*
- Click trigger → dropdown/popover hiện danh sách cột với checkbox
- Required columns (`name`): checkbox disabled, luôn checked
- Thay đổi được lưu ngay vào localStorage (không cần nút Save)

### Files tạo mới

```
src/app/shared/services/column-picker.service.ts
src/app/shared/services/column-picker.service.spec.ts
src/app/shared/components/column-picker/
  ├── column-picker.component.ts
  ├── column-picker.component.html
  ├── column-picker.component.scss
  └── column-picker.component.spec.ts
```

### Files sửa

```
src/app/features/gantt/components/gantt-left-panel/gantt-left-panel.ts
  → Thêm ColumnPickerService, ColumnPickerComponent
  → Thêm gridCols getter tính CSS grid template động

src/app/features/projects/components/task-tree/task-tree.ts
  → Refactor: replace inline column logic → ColumnPickerService
  → Zero breaking change trên @Input/@Output public API
```

### Acceptance Criteria

**ColumnPickerService:**
- AC3.1: `loadColumns(config)` đọc từ localStorage, fallback về `defaultVisible` nếu chưa có
- AC3.2: `toggleColumn(componentId, columnId)` cập nhật state và lưu localStorage ngay
- AC3.3: Column có `required: true` không thể toggle (bị bỏ qua nếu gọi toggle)
- AC3.4: `getVisibleColumnIds(componentId)` trả về mảng id các cột đang hiện
- AC3.5: `getGridTemplate(config)` trả về CSS grid template string phù hợp

**ColumnPickerComponent:**
- AC3.6: Render checkbox per column; required column thì disabled
- AC3.7: Thay đổi checkbox emit event và gọi service.toggleColumn ngay
- AC3.8: "Reset về mặc định" button → gọi service reset, xóa localStorage entry

**Gantt Left Panel:**
- AC3.9: Hiển thị trigger icon `tune` ở header panel
- AC3.10: Default: name, status, assignee, plannedEnd visible; type, priority, plannedStart, percentComplete hidden
- AC3.11: Trạng thái column visibility persist qua refresh (localStorage)
- AC3.12: Column `name` luôn hiện, không có checkbox

**Task-Tree Refactor:**
- AC3.13: Sau refactor, task-tree hoạt động đúng như trước (zero regression)
- AC3.14: localStorage key đổi thành `column-visibility-task-tree` (migration: nếu có key cũ `task-tree-columns-v1` → đọc và migrate một lần)

**Testing:**
- AC3.15: `ng test --watch=false` → 0 failures
- AC3.16: Service spec cover: load/save/toggle/reset/required-column protection
- AC3.17: Component spec cover: render, toggle interaction, disabled state, reset

---

## Non-Functional Requirements

| NFR | Mô tả |
|---|---|
| NFR-G1 | Bỏ dependency arrows không làm Gantt render chậm hơn (expected: nhanh hơn do bỏ SVG path calculation) |
| NFR-G2 | Column picker dropdown mở/đóng trong < 100ms |
| NFR-G3 | localStorage read/write không block UI thread |
| NFR-G4 | Shared service không inject state giữa task-tree và gantt-left-panel (mỗi instance độc lập theo componentId) |

---

## Phụ lục — Danh sách file bị ảnh hưởng

### Thay đổi 1 (Bỏ dependency arrows)
```
MODIFY: frontend/.../gantt/components/gantt-timeline/gantt-timeline.ts
MODIFY: frontend/.../gantt/components/gantt-timeline/gantt-timeline.html
MODIFY: frontend/.../gantt/components/gantt/gantt.ts
MODIFY: frontend/.../gantt/models/gantt.model.ts
MODIFY: frontend/.../gantt/services/gantt-timeline.service.ts
MODIFY: frontend/.../gantt/services/gantt-adapter.service.ts
DELETE: (logic xóa inline, không xóa file)
```

### Thay đổi 2 (Phân màu)
```
MODIFY: frontend/.../gantt/services/gantt-timeline.service.ts  ← getBarColor() + spec
```

### Thay đổi 3 (Column picker)
```
CREATE: frontend/.../shared/services/column-picker.service.ts
CREATE: frontend/.../shared/services/column-picker.service.spec.ts
CREATE: frontend/.../shared/components/column-picker/column-picker.component.ts
CREATE: frontend/.../shared/components/column-picker/column-picker.component.html
CREATE: frontend/.../shared/components/column-picker/column-picker.component.scss
CREATE: frontend/.../shared/components/column-picker/column-picker.component.spec.ts
MODIFY: frontend/.../gantt/components/gantt-left-panel/gantt-left-panel.ts
MODIFY: frontend/.../gantt/components/gantt-left-panel/gantt-left-panel.html
MODIFY: frontend/.../projects/components/task-tree/task-tree.ts
```
