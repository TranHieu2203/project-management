---
title: 'Task Tree – Inline Quick Edit (Jira-style)'
type: 'feature'
created: '2026-04-26'
status: 'in-review'
baseline_commit: '2f0fdb0020a3945f7904e60883beeadfafe86b66'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Màn "Danh sách Tasks" yêu cầu mở dialog đầy đủ để sửa bất kỳ trường nào, gây chậm trọng tâm khi cần thay đổi nhanh status, priority hay assignee.

**Approach:** Cho phép click trực tiếp vào 5 field (`name`, `status`, `priority`, `assigneeUserId`, `percentComplete`) trên mỗi hàng task để chỉnh sửa inline; Esc hủy, Enter/blur lưu và dispatch `updateTask` action đã có sẵn.

## Boundaries & Constraints

**Always:**
- Chỉ dispatch `TasksActions.updateTask` — không tạo action mới.
- Toàn bộ payload `UpdateTaskPayload` phải đầy đủ (copy task gốc + ghi đè field đổi) vì API là PUT full-replace.
- Khi 1 ô đang edit: row đó phải `draggable="false"` để tránh drag khi gõ.
- Nút "Chỉnh sửa" (dialog) và "Xóa" giữ nguyên — inline edit bổ sung, không thay thế.

**Ask First:**
- Nếu cần thêm field nào ngoài 5 field trên (ví dụ: dates, effort hours).

**Never:**
- Không dùng `MatSelect` standalone-form-field nặng trong table row — dùng `<select>` native với `FormsModule`/`[(ngModel)]` để tránh layout phức tạp.
- Không lưu giá trị chưa thay đổi (nếu giá trị = gốc thì không dispatch).
- Không chạm vào store, effects, API service.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Click field để sửa | User click vào `name`/`status`/`priority`/`assignee`/`percent` trên 1 hàng | Cell đó chuyển sang control edit; row không còn draggable | — |
| Lưu bằng Enter hoặc blur | Giá trị mới ≠ gốc | Dispatch `updateTask` với full payload; cell về display mode | — |
| Hủy bằng Esc | Đang edit | Cell về display mode, không dispatch | — |
| Giá trị không đổi | User click rồi blur/Enter ngay | Không dispatch (tránh PUT thừa) | — |
| Click field khác khi đang edit | Đang edit cell A, click cell B | Cell A commit, Cell B mở edit | — |
| Conflict 409 | Server trả 409 khi updateTask | Effects đã xử lý `updateTaskConflict` — behavior không đổi | Hiển thị conflict theo flow hiện tại |

</frozen-after-approval>

## Code Map

- `frontend/.../features/projects/components/task-tree/task-tree.ts` -- Component chính, thêm inline-edit state + methods
- `frontend/.../features/projects/components/task-tree/task-tree.html` -- Template, switch display↔input theo `editingCell`
- `frontend/.../features/projects/components/task-tree/task-tree.scss` -- Style cho editable cells và inline controls
- `frontend/.../features/projects/components/project-detail/project-detail.ts` -- Parent, thêm handler `onQuickUpdateTask()`
- `frontend/.../features/projects/components/project-detail/project-detail.html` -- Thêm binding `(quickUpdateTask)`
- `frontend/.../features/projects/models/task.model.ts` -- Tham chiếu types, không sửa

## Tasks & Acceptance

**Execution:**
- [x] `task-tree.ts` -- Thêm `FormsModule` import; thêm `EditingCell` interface; thêm `editingCell`/`editingValue` state; thêm `startEdit()`, `commitEdit()`, `cancelEdit()`, `onEditKeydown()`, `isEditing()` helpers; thêm `@Output() quickUpdateTask`; thêm `@Input() projectId`
- [x] `task-tree.html` -- Wrap 5 field bằng `@if (isEditing(...))`/`@else` block: display span click-to-edit / input hoặc select native; `[draggable]` conditional; stopPropagation trên control để không trigger drag
- [x] `task-tree.scss` -- Thêm `.editable-cell` (cursor pointer, hover underline-dashed), `.inline-input`, `.inline-select` (style hòa hợp với row)
- [x] `project-detail.ts` -- Thêm `onQuickUpdateTask(event)` handler build full payload + dispatch; import type mới nếu cần
- [x] `project-detail.html` -- Thêm `(quickUpdateTask)="onQuickUpdateTask($event)"` vào `<app-task-tree>`

**Acceptance Criteria:**
- Given task row trong danh sách, when user click vào field `name`, then text input xuất hiện với giá trị hiện tại và row không còn draggable
- Given đang edit `status`, when user chọn giá trị mới rồi blur, then `updateTask` được dispatch với status mới và các field khác không đổi
- Given đang edit bất kỳ field nào, when user nhấn Esc, then cell trở về display mode và không có dispatch nào
- Given giá trị không thay đổi, when user blur/Enter, then không có dispatch nào (no spurious PUT)
- Given đang edit cell A, when user click cell B, then cell A commit trước, cell B mở edit
- Given edit, when user kéo row khác, then drag chỉ xảy ra trên các row không đang edit

## Spec Change Log

## Design Notes

`commitEdit` nhận `task: ProjectTask` (object gốc từ `_flatNodes`) để có đủ dữ liệu build full PUT payload mà không cần lookup thêm.

Thứ tự xử lý "click cell B khi đang edit cell A": Angular `(blur)` của input A fires trước `(click)` của cell B, nên `commitEdit(taskA)` chạy trước `startEdit(taskB, ...)` tự nhiên — không cần xử lý thêm.

## Verification

**Commands:**
- `cd frontend/project-management-web && npx ng build --configuration development` -- expected: build thành công, 0 errors

**Manual checks (if no CLI):**
- Click vào field `name` của 1 task → input xuất hiện, gõ tên mới, Enter → tên cập nhật trong danh sách
- Click vào `status` → select dropdown native, chọn "Hoàn thành" → blur → status cập nhật
- Nhấn Esc → không có thay đổi
- Kéo một row khác trong lúc không có ô nào đang edit → drag hoạt động bình thường
