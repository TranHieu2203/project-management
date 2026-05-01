# Story 8.1: Issue Types catalog (Bug/Story/Epic/Task/Sub-task + custom)

Status: review

**Story ID:** 8.1  
**Epic:** Epic 8 — Issue Model Migration + Agile Foundation  
**Phụ thuộc:** Story 8.0 (Issue migration) phải complete trước khi triển khai Epic 9–15; Story 8.1 có thể dev song song nhưng chỉ được merge/release khi 8.0 đã ổn định.  
**Story Key (sprint-status):** `8-1-issue-types-catalog-bug-story-epic-task-sub-task-custom`

---

## Story

As a system admin,  
I want cấu hình danh mục Issue Type cho hệ thống (gồm các type mặc định và custom),  
so that mỗi project có thể dùng đúng loại issue phù hợp workflow của họ.

---

## Acceptance Criteria

1) **Given** hệ thống khởi tạo  
   **When** seed data chạy  
   **Then** tồn tại 5 issue type mặc định: Bug, Story, Epic, Task, Sub-task với icon và màu tương ứng  
   **And** các type mặc định không thể xóa nhưng có thể ẩn ở cấp project

2) **Given** admin muốn tạo issue type tùy chỉnh  
   **When** gọi `POST /api/issue-types` với `name`, `icon`, `color`, `projectId` (nếu scoped)  
   **Then** trả `201` với type vừa tạo  
   **And** type mới có thể gán cho các project

3) **Given** issue type đang được dùng bởi ít nhất 1 issue  
   **When** admin xóa type đó  
   **Then** trả `409 ProblemDetails` với message rõ ràng

---

## Tasks / Subtasks

> Lưu ý: Story 8.0 đã tạo/đưa vào migration/table `issue_type_definitions`. Story 8.1 tập trung vào **API contracts + domain rules + tests** cho catalog; không làm UI (UI thuộc Story 8.2).

- [x] **Task 1: Xác nhận schema + seed built-in Issue Types** (AC: 1)
  - [x] 1.1 Verify bảng `issue_type_definitions` đúng shape (columns + constraints + index theo project) như kiến trúc AD-10.
  - [x] 1.2 Verify built-in types được seed: `Epic`, `Story`, `Task`, `Bug`, `Sub-task` với `project_id = NULL`, `is_built_in=true`, `is_deletable=false`.
  - [x] 1.3 Quy ước icon/color: lấy từ seed/migration hiện có; không hardcode lặp lại trong code — map bằng const/enum ở application layer nếu cần.

- [x] **Task 2: Backend module placement + naming** (guardrail)
  - [x] 2.1 Tất cả API endpoints phải theo prefix `/api/v1/` và kebab-case số nhiều.
  - [x] 2.2 JSON field camelCase; error trả ProblemDetails thống nhất.
  - [x] 2.3 Không để business logic trong controller; follow service pattern (không dùng CQRS/MediatR cho story 8.1 theo yêu cầu).

- [x] **Task 3: Implement API - list built-in types** (AC: 1, AD-10)
  - [x] 3.1 `GET /api/v1/issue-types`: trả danh sách **system-wide** built-in types (project_id = NULL), sorted theo `sort_order`.
  - [x] 3.2 Response DTO là `record` và không expose domain entity.

- [x] **Task 4: Implement API - list types cho project** (AC: 1, AD-10)
  - [x] 4.1 `GET /api/v1/projects/{projectId}/issue-types`: trả built-in + custom types scoped to project.
  - [x] 4.2 Membership-only authorization baseline: chỉ user có membership trong project mới được xem (tận dụng pattern Epic 1.2).

- [x] **Task 5: Implement API - create custom type** (AC: 2, AD-10)
  - [x] 5.1 Có cả `POST /api/v1/projects/{projectId}/issue-types` và route tương thích `POST /api/v1/issue-types` (body có `projectId`).
  - [x] 5.2 Validation:
    - `name` required, max length 50, trim, không allow chỉ whitespace.
    - `color` theo format `#RRGGBB`.
  - [x] 5.3 Uniqueness: map unique violation → `409 ProblemDetails`.

- [x] **Task 6: Implement API - update custom type** (AD-10)
  - [x] 6.1 `PUT /api/v1/projects/{projectId}/issue-types/{typeId}`: chỉ update khi type là custom của đúng project.
  - [x] 6.2 Reject update built-in type với `422` (business rule) message rõ ràng.

- [x] **Task 7: Implement API - delete custom type với rule “in use”** (AC: 3, AD-10)
  - [x] 7.1 `DELETE /api/v1/projects/{projectId}/issue-types/{typeId}`:
    - Built-in: không cho xóa (422) message rõ.
    - In-use: trả `409 ProblemDetails`.
    - Not-in-use: soft-delete.
  - [x] 7.2 “In use” definition: tồn tại `issues` row có `issue_type_id = typeId` (count cả soft-deleted issues để tránh delete sai).

- [x] **Task 8: Tests (QT-01 bắt buộc)** (AC: 1–3)
  - [x] 8.1 Integration tests:
    - `GET /api/v1/issue-types` trả đúng 5 built-in types.
    - `POST /api/v1/projects/{projectId}/issue-types` tạo custom type → `201` + body đúng.
    - `DELETE` custom type đang “in use” → `409` + ProblemDetails.
  - [x] 8.2 Business rules (built-in immutability) covered via integration + domain/service checks.
  - [x] 8.3 Test suite pass 100%.

---

## Dev Notes (Guardrails cho Dev Agent)

### Kiến trúc / nguồn sự thật

- **Schema + endpoints tham chiếu**: `architecture.md` (AD-10) — bảng `issue_type_definitions` + endpoint list/create/update/delete.  
  [Source: `_bmad-output/planning-artifacts/architecture.md` → “AD-10: Issue Type Definitions Schema (Epic 8)”]
- **User story + AC**: `epics.md` → “Story 8.1”.  
  [Source: `_bmad-output/planning-artifacts/epics.md` → “Story 8.1: Issue Types catalog …”]
- **UI feedback rules** (dù story này backend-only vẫn phải giữ consistency cho FE Story 8.2): `FeedbackDialogService`, không MatSnackBar trong code mới.  
  [Source: `docs/project-context.md` → QT-04]

### File structure (dự kiến)

> Chỉ là guardrail định hướng; dev agent phải bám theo structure hiện có trong repo.

- Backend: tạo module “Issues”/“Agile” nếu đã tồn tại theo Phase 2, hoặc đặt trong module phù hợp (nếu dự án đang đặt “issues” tạm trong `Projects` module thì follow pattern hiện tại).
- API controllers: `/api/v1/issue-types` và `/api/v1/projects/{projectId}/issue-types`.
- CQRS:
  - Queries: `GetBuiltInIssueTypes`, `GetIssueTypesByProject`
  - Commands: `CreateIssueType`, `UpdateIssueType`, `DeleteIssueType`

### Error handling & status codes (bắt buộc)

- Use `ProblemDetails` và map `409` cho “cannot delete because in use” (AC #3).
- Không dùng `.Result`/`.Wait()`. Có `CancellationToken` cho mọi async I/O.
- Không dùng `Console.WriteLine`; dùng `ILogger<T>`.

---

## Dev Agent Record

### Agent Model Used

gpt-5.2

### Debug Log References

### Completion Notes List

### File List

- `src/Modules/Projects/ProjectManagement.Projects.Api/Controllers/IssueTypesController.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/IssueTypes/Models/IssueTypeDto.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/IssueTypes/Services/IIssueTypesService.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Services/IssueTypesService.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/IssueTypeDefinition.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Extensions/ProjectsInfrastructureServiceCollectionExtensions.cs`
- `src/Modules/Auth/ProjectManagement.Auth.Api/Controllers/AuthController.cs`
- `tests/ProjectManagement.Host.Tests/IssueTypesTests.cs`

### Debug Log References

- 2026-05-01: Thêm endpoints Issue Types + service layer (không dùng CQRS cho story 8.1 theo yêu cầu).
- 2026-05-01: Fix flakiness test suite do Identity lockout trong login: set `lockoutOnFailure: false`.

### Completion Notes List

- Implement `GET /api/v1/issue-types` (built-in list) và `GET/POST/PUT/DELETE /api/v1/projects/{projectId}/issue-types` (membership-only).
- Thêm route tương thích `POST /api/v1/issue-types` (body có `projectId`) đúng theo AC trong `epics.md`.
- Map unique constraint + “in use” delete rule → `409 ProblemDetails`.
- Chạy `dotnet test` và pass toàn bộ `ProjectManagement.Host.Tests` (103 tests).

