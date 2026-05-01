# Story 8.2: Issue Type CRUD UI (admin configures issue types per project)

Status: in-progress

**Story ID:** 8.2  
**Epic:** Epic 8 — Issue Model Migration + Agile Foundation  
**Story Key (sprint-status):** `8-2-issue-type-crud-ui-admin-configures-issue-types-per-project`  
**Phụ thuộc / Lưu ý critical path:** Story 8.0 là BLOCKER cho mọi story Phase 2+ (merge/release chỉ khi 8.0 ổn định). Story 8.2 có thể dev song song, nhưng không được làm hỏng backward-compat `issues`/`project_tasks` từ 8.0.

---

## Story

As a project admin,  
I want cấu hình issue types được phép trong project của mình,  
so that team chỉ thấy những loại issue phù hợp, tránh nhầm lẫn.

---

## Acceptance Criteria

1) **Given** user có role Admin trong project  
   **When** vào trang Project Settings > Issue Types  
   **Then** thấy danh sách issue types đang active cho project, có thể bật/tắt từng type

2) **Given** admin bật/tắt issue type trong project  
   **When** lưu cấu hình  
   **Then** thay đổi có hiệu lực ngay; form tạo issue chỉ hiện types đang bật

---

## Scope Notes (để tránh dev lệch hướng)

- Story 8.1 đã có **catalog API** cho issue types (built-in + custom) và các rule built-in không xóa/sửa. Story 8.2 bổ sung **project-level enable/disable** + **UI** để cấu hình.
- Story 8.4 (Settings Hub) đang backlog. Ở story 8.2, cần tạo route/page cho Issue Types theo URL ổn định; Settings Hub sẽ “wrap/link” sau.
- “Admin” trong AC map vào role hiện có trong codebase:
  - `ProjectMemberRole.Manager` = Admin/Project Admin (quy ước trong story này).
  - `ProjectMemberRole.Member` = không được cấu hình.

---

## Tasks / Subtasks

### Backend — lưu cấu hình enabled/disabled theo project (để AC #2 có hiệu lực thật)

- [ ] **Task B1: DB schema cho cấu hình issue type theo project** (AC: 1–2)
  - [ ] Tạo bảng mới `project_issue_type_settings` (expand-contract friendly):
    - `project_id` (UUID, FK → `projects(id)`)
    - `issue_type_id` (UUID, FK → `issue_type_definitions(id)`)
    - `is_enabled` (boolean, NOT NULL, default true)
    - `created_at`, `created_by`, `updated_at`, `updated_by` (theo convention nếu có)
    - Unique: `(project_id, issue_type_id)`
  - [ ] Migration naming theo rule: `V{epic}{seq}_{description}`. Ví dụ: `V008_006_project_issue_type_settings`  
    [Source: `docs/project-context.md` → “Migration naming”]

- [ ] **Task B2: Model + EF configuration** (AC: 1–2)
  - [ ] Tạo domain entity `ProjectIssueTypeSetting` (đặt trong `Projects.Domain` theo pattern hiện có).
  - [ ] Thêm `DbSet<ProjectIssueTypeSetting>` vào `IProjectsDbContext` + `ProjectsDbContext` và EF config trong `Projects.Infrastructure/Persistence/Configurations/`.

- [ ] **Task B3: Authorization rule — chỉ Project Admin (Manager) mới được cấu hình** (AC: 1–2)
  - [ ] Tạo reusable checker/service để ensure “member + role Manager”:
    - **Không leak existence**: non-member phải 404 như `IMembershipChecker.EnsureMemberAsync` đang làm.
    - Member nhưng không Manager: trả 403 (hoặc DomainException map 403) theo standard của dự án.
  - [ ] Không dùng magic strings cho role; reuse `ProjectMemberRole`.

- [ ] **Task B4: API contracts cho settings** (AC: 1–2)
  - [ ] Tạo endpoints (kebab-case, `/api/v1/`, JSON camelCase):
    - `GET /api/v1/projects/{projectId}/issue-type-settings`
      - trả danh sách built-in + custom issue types cho project kèm `isEnabled`
      - default behavior:
        - nếu **không có record** trong `project_issue_type_settings` ⇒ `isEnabled = true`
    - `PUT /api/v1/projects/{projectId}/issue-type-settings/{typeId}`
      - body: `{ isEnabled: boolean }`
      - effect ngay lập tức
      - yêu cầu: project admin (Manager)
  - [ ] Lưu ý tương thích:
    - Không thay đổi shape của `GET /api/v1/projects/{projectId}/issue-types` (catalog) để tránh phá các consumer khác; settings API là endpoint mới.
  - [ ] Error handling:
    - Non-member: 404 (không leak project existence)
    - Not manager: 403
    - typeId không thuộc built-in hoặc project: 404
    - Dùng `ProblemDetails` nhất quán.

- [ ] **Task B5: Tests (QT-01 bắt buộc)** (AC: 1–2)
  - [ ] Integration tests (PostgreSQL Testcontainers):
    - Manager `GET issue-type-settings` trả list + `isEnabled` default true.
    - Manager `PUT .../{typeId}` set false, gọi lại `GET` thấy updated.
    - Member (không Manager) `PUT` bị từ chối (403).
    - Non-member `GET/PUT` trả 404.
  - [ ] Không được claim pass nếu chưa chạy `dotnet test` thật.  
    [Source: `docs/project-context.md` → QT-01]

### Frontend (Angular 21) — Project Settings > Issue Types UI

- [ ] **Task F1: Route & page placement** (AC: 1)
  - [ ] Tạo page theo route (ưu tiên lazy-load):
    - `projects/:projectId/settings/issue-types`
  - [ ] (Optional) Nếu chưa có Settings Hub (story 8.4), page vẫn phải truy cập được trực tiếp bằng URL.

- [ ] **Task F2: API client** (AC: 1–2)
  - [ ] Tạo service mới (hoặc extend existing) để gọi:
    - `GET /api/v1/projects/{projectId}/issue-type-settings`
    - `PUT /api/v1/projects/{projectId}/issue-type-settings/{typeId}`
  - [ ] RxJS rules: không nested subscribe; dùng `switchMap/mergeMap` và handle error rõ ràng.

- [ ] **Task F3: UI behavior** (AC: 1–2)
  - [ ] Hiển thị danh sách issue types với:
    - Name
    - Icon (nếu đã có component/pattern; nếu chưa thì hiển thị iconKey dạng text)
    - Color swatch nhỏ (không dùng background màu sặc sỡ cho cả row)
    - Toggle enable/disable (MatSlideToggle hoặc equivalent)
  - [ ] Khi toggle:
    - optimistic update UI
    - call API `PUT`
    - on error: revert toggle và show error qua `FeedbackDialogService`
  - [ ] Không dùng `MatSnackBar` trong code mới; bắt buộc dùng `FeedbackDialogService`.  
    [Source: `docs/project-context.md` → QT-04]

- [ ] **Task F4: UI tests (QT-01)** (AC: 1–2)
  - [ ] Vitest + Angular Testing Library:
    - render page với list items từ mock API
    - toggle gọi đúng API và cập nhật state
    - lỗi API ⇒ revert toggle và gọi `FeedbackDialogService.error(...)`

- [ ] **Task F5: Browser verification (QT-02)** (AC: 1–2)
  - [ ] Chạy app và verify trên browser thật:
    - vào URL `projects/:projectId/settings/issue-types`
    - toggle 1 type và refresh page để confirm state persisted
  - [ ] Nếu không thể chạy browser MCP trong môi trường này, ghi rõ trong Dev Agent Record và tạo follow-up task.  
    [Source: `docs/project-context.md` → QT-02]

---

## Dev Notes (Guardrails cho Dev Agent)

### Source of truth / References

- **User story & AC**: `_bmad-output/planning-artifacts/epics.md` → “Story 8.2: Issue Type CRUD UI …”  
  [Source: `_bmad-output/planning-artifacts/epics.md`]
- **Issue Types catalog API (đã có từ 8.1)**: `_bmad-output/implementation-artifacts/8-1-issue-types-catalog-bug-story-epic-task-sub-task-custom.md`  
  [Source: `_bmad-output/implementation-artifacts/8-1-issue-types-catalog-bug-story-epic-task-sub-task-custom.md`]
- **Architecture (Issue Type Definitions schema + endpoints list)**: `_bmad-output/planning-artifacts/architecture.md` → “AD-10: Issue Type Definitions Schema (Epic 8)”  
  [Source: `_bmad-output/planning-artifacts/architecture.md`]
- **Project rules (tests, UI feedback, browser check)**: `docs/project-context.md` → QT-01, QT-02, QT-04  
  [Source: `docs/project-context.md`]

### Existing code anchors (đừng reinvent wheel)

- Backend Issue Types catalog đã tồn tại:
  - `src/Modules/Projects/ProjectManagement.Projects.Api/Controllers/IssueTypesController.cs`
  - `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Services/IssueTypesService.cs`
- Frontend feedback handling:
  - `frontend/project-management-web/src/app/shared/services/feedback-dialog.service.ts`

### Non-goals / Anti-regression

- Không chỉnh sửa/migrate lại các quyết định ở Story 8.1 trừ khi cần để thêm settings endpoint.
- Không đụng `MatSnackBar` trong code FE mới.
- Không tạo “magic strings” (route segments, role checks, constants) — dùng const/enum phù hợp.
- Không để logic authorization trong controller; tách service/checker theo pattern backend hiện có.

---

## Dev Agent Record

### Agent Model Used

gpt-5.2

### Debug Log References

### Completion Notes List

### File List

> File list là “dự kiến”; dev agent phải align theo structure thực tế và update lại list này khi hoàn thành.

- Backend (dự kiến):
  - `src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/ProjectIssueTypeSetting.cs`
  - `src/Modules/Projects/ProjectManagement.Projects.Application/IssueTypes/Models/IssueTypeSettingDto.cs`
  - `src/Modules/Projects/ProjectManagement.Projects.Application/IssueTypes/Services/IProjectIssueTypeSettingsService.cs`
  - `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Services/ProjectIssueTypeSettingsService.cs`
  - `src/Modules/Projects/ProjectManagement.Projects.Api/Controllers/ProjectIssueTypeSettingsController.cs`
  - `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/ProjectIssueTypeSettingConfiguration.cs`
  - `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/*_V008_006_project_issue_type_settings.cs`
  - `tests/ProjectManagement.Host.Tests/ProjectIssueTypeSettingsTests.cs`

- Frontend (dự kiến):
  - `frontend/project-management-web/src/app/features/projects/components/project-settings-issue-types/project-settings-issue-types.ts`
  - `frontend/project-management-web/src/app/features/projects/components/project-settings-issue-types/project-settings-issue-types.html`
  - `frontend/project-management-web/src/app/features/projects/components/project-settings-issue-types/project-settings-issue-types.spec.ts`
  - `frontend/project-management-web/src/app/features/projects/services/project-issue-type-settings-api.service.ts`
  - `frontend/project-management-web/src/app/features/projects/projects.routes.ts` (add child route)

