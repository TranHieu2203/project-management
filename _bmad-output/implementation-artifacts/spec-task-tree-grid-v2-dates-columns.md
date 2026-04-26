---
title: 'Task Tree v2 – Tree Readability, 4 Date Columns, Column Visibility'
type: 'feature'
created: '2026-04-26'
status: 'in-review'
baseline_commit: '2f0fdb0020a3945f7904e60883beeadfafe86b66'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Task tree khó đọc phân cấp; chỉ có 2 cột ngày kế hoạch không edit được; không có cách ẩn/hiện cột theo nhu cầu.

**Approach:** (1) Thêm tree-line connectors (└/├/│) dạng ký tự để thể hiện phân cấp rõ ràng + row tinting theo depth. (2) Mở rộng lên 4 cột ngày (plannedStart/End + actualStart/End) với inline date picker. (3) Thêm column visibility picker lưu localStorage.

## Boundaries & Constraints

**Always:**
- `name` và `actions` luôn hiển thị, không cho ẩn.
- Column visibility lưu key `task-tree-columns-v1` trong localStorage.
- `<input type="date">` trả về "YYYY-MM-DD" — tương thích trực tiếp với model.
- Khi value date là `''` (cleared) → emit `null`; handler trong `project-detail.ts` map `null` → `undefined` (consistent với form hiện tại).
- Grid template tính động bằng getter `gridCols`, bind vào `[style.grid-template-columns]` trên mỗi `.grid-row`.

**Ask First:**
- Nếu yêu cầu expand/collapse node (hiện tại không có trong scope).

**Never:**
- Không chạm store, effects, API.
- Không dùng Angular CDK overlay cho column picker — dùng absolute-positioned div đơn giản.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected | Error Handling |
|----------|-------|----------|----------------|
| Tree depth=2 | Node có grandparent | Hiển thị `│  └─` trước tên | — |
| Date edit | Click ngày → blur với date mới | Dispatch updateTask với date "YYYY-MM-DD" | — |
| Date cleared | User xóa date → blur | Emit `null`, payload field = `undefined` | — |
| Ẩn cột | Toggle "Trạng thái" off | Cột biến mất, grid cols cập nhật, localStorage lưu | — |
| Reload trang | localStorage có saved state | Columns phục hồi đúng state đã lưu | Nếu parse lỗi → dùng default |

</frozen-after-approval>

## Code Map

- `frontend/.../task-tree/task-tree.ts` -- Thêm FlatNode.isLast/ancestorIsLast, ColumnDef, visibleCols, gridCols getter, toggle methods, date EditableField
- `frontend/.../task-tree/task-tree.html` -- Tree line connectors, conditional column cells, date input, column picker UI
- `frontend/.../task-tree/task-tree.scss` -- Tree line styles, col picker styles, depth row tinting
- `frontend/.../project-detail/project-detail.ts` -- Không cần đổi (null→undefined đã có)

## Tasks & Acceptance

**Execution:**
- [x] `task-tree.ts` -- Mở rộng FlatNode với isLast/ancestorIsLast; EditableField thêm 4 date field; ColumnDef + COLUMNS constant; visibleCols Set + localStorage load/save; gridCols getter; toggleCol/toggleColPicker methods
- [x] `task-tree.html` -- Tree line spans trong name cell; column picker toolbar; header + data cells conditional trên visibleCols; 4 date columns với @if edit/display; [style.grid-template-columns] binding
- [x] `task-tree.scss` -- .tree-indent/.seg/.conn styles; .col-picker panel; .row-depth-0 tinting; loại bỏ hardcoded $grid-cols

**Acceptance Criteria:**
- Given task ở depth=2, when render, then hiển thị ký tự tree line rõ ràng (│ và └/├) trước tên
- Given click vào cột ngày, when user chọn date mới và blur, then updateTask dispatch với date "YYYY-MM-DD" mới
- Given click "Cột hiển thị", when uncheck "Trạng thái", then cột đó biến khỏi grid và state persist qua reload
- Given user reload trang, when localStorage có saved state, then columns phục hồi đúng

## Spec Change Log

## Verification

**Commands:**
- `cd frontend/project-management-web && npx ng build --configuration development` -- expected: 0 errors
