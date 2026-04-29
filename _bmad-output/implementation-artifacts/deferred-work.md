# Deferred Work

Goals deferred from active specs to keep each spec within scope.

---

## [Deferred from spec-unify-view-layout-embed-gantt-project-detail] — 2026-04-29

### Task Type Extension + Board Form Unification + Type Icons

**Why deferred:** Scope tách khỏi layout unification để giữ spec ≤ 1600 tokens. Có thể ship độc lập.

**Suggested slug:** `spec-task-type-jira-board-form-unify.md`

#### [A] Task type extension (backend + frontend)

- `src/.../Domain/Enums/TaskType.cs` — Thêm `Story`, `Bug`, `Issue`, `Feature` vào enum sau `Task`
  - Không cần EF migration: DB lưu string (`HasConversion<string>()`)
  - `Enum.Parse<TaskType>()` trong `TasksController` sẽ nhận các value mới
- `frontend/.../models/task.model.ts` — Update `ProjectTask.type` union: `'Phase' | 'Milestone' | 'Task' | 'Story' | 'Bug' | 'Issue' | 'Feature'`

#### [B] Board dùng TaskFormComponent (source of truth duy nhất)

- `frontend/.../components/task-form/task-form.ts`:
  - Cập nhật `taskTypes = ['Phase','Milestone','Task','Story','Bug','Issue','Feature']`
  - Thêm `initialStatus?: string` vào `TaskFormData` interface
  - `ngOnInit` create mode: nếu `data.initialStatus` → `form.patchValue({ status: data.initialStatus })`
- `frontend/.../components/task-form/task-form.html` — Icon badge per type trong dropdown
- `frontend/.../components/board/board.ts`:
  - `openQuickEdit(task)` → mở `TaskFormComponent` (edit mode) thay `TaskQuickEditComponent`
  - `onOpenFullForm(event)` → mở `TaskFormComponent` (create mode, `parentId: event.phaseId, initialStatus: event.status`)
  - Xóa import `TaskQuickEditComponent`
- Xóa: `task-quick-edit/task-quick-edit.ts`, `.html`, `.scss` (3 files)

#### [C] Type icon badges (Jira-like visual)

Icon map (Material icons, size 14px, trước task name):
```
Phase     → layers                #78909c
Milestone → outlined_flag         #ff9800
Task      → check_circle_outline  #2196f3
Story     → bookmark              #4caf50
Bug       → bug_report            #f44336
Issue     → warning_amber         #ffc107
Feature   → auto_fix_high         #9c27b0
```

Files cần cập nhật:
- `frontend/.../components/task-tree/task-tree.html` — icon trên grid rows
- `frontend/.../components/board/task-card/task-card.html` — icon trong card header
- `frontend/.../features/gantt/components/gantt-left-panel/gantt-left-panel.html` — icon trong cột name

**Notes:**
- `TaskQuickCreateComponent` (inline quick-add row trên board) vẫn giữ nguyên; chỉ replace phần dialog "Form đầy đủ" và "click card để edit"
- Backend validator (`CreateTaskCommandValidator`) không validate type explicitly → không cần sửa validator
