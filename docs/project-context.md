# Project Context — project-management

## Tổng quan

Internal web application thay thế Excel để quản lý mixed workforce (inhouse + outsource multi-vendor) trên nhiều dự án. Mục tiêu dài hạn: Full Jira feature parity.

**Stack:** Angular 21 + NgRx | .NET 10 Modular Monolith + CQRS/MediatR | PostgreSQL + EF Core | Bryntum Gantt (adapter layer)

---

## Quy tắc bắt buộc cho Dev Agent (Amelia)

### QT-01: Test — 100% pass, không ngoại lệ

- Mọi story chỉ được đánh dấu **Done** khi toàn bộ test suite (unit + integration) pass 100%.
- Mỗi Acceptance Criteria (AC) phải có ít nhất 1 unit test tương ứng trước khi đánh dấu subtask complete.
- Không được khai báo test "pass" nếu test chưa thực sự tồn tại và chạy được.
- Test DB: dùng **PostgreSQL Testcontainers** (không dùng in-memory EF Core) để đảm bảo SQL-level behavior đúng.
- FE test: **Vitest** + Angular Testing Library. Mọi component mới phải có test file đi kèm.

### QT-02: UI — Phải kiểm tra trên trình duyệt thực qua MCP Playwright

- Mọi story có thay đổi UI **bắt buộc** phải được kiểm tra trên trình duyệt thực bằng **MCP Playwright server** trước khi đánh dấu Done.
- Các tool được dùng: `mcp__playwright__browser_navigate`, `mcp__playwright__browser_snapshot`, `mcp__playwright__browser_click`, `mcp__playwright__browser_fill_form`, `mcp__playwright__browser_take_screenshot`.
- Quy trình tối thiểu:
  1. Start dev server (`ng serve`)
  2. `browser_navigate` đến feature URL
  3. `browser_snapshot` để capture DOM/accessibility tree
  4. Thực hiện golden-path interaction (click, form fill, v.v.)
  5. `browser_take_screenshot` để confirm visual result
  6. Kiểm tra edge case ít nhất 1 case
- Nếu MCP Playwright không khả dụng trong môi trường hiện tại: **ghi rõ trong Dev Agent Record** rằng browser test chưa thực hiện và tạo task follow-up.

### QT-03: UI Design System — Màu trắng/đen chủ đạo

- **Màu chủ đạo:** Trắng (`#FFFFFF`) và đen (`#000000` / `#111111`) là nền tảng của toàn bộ design system.
- **Màu accent duy nhất được phép:** Một màu accent duy nhất (mặc định `#1a1a2e` hoặc xanh đậm trung tính `#1565C0`) — dùng cho CTA, active state, link.
- **Không được dùng:** Màu nền sặc sỡ, gradient nhiều màu, background colorful cho cards/panels.
- **Typography:** Tất cả text trên nền trắng → màu đen/xám đậm; text trên nền tối → trắng/xám nhạt.
- **Status colors (exception):** Các màu trạng thái (đỏ = lỗi, xanh = success, vàng = warning) chỉ được dùng cho badge/icon nhỏ, không phải background toàn bộ section.
- **Consistency:** Dùng Angular Material theme với custom palette black/white. Không import component styles bên ngoài theme đã định nghĩa.
- Trước khi submit story: chụp screenshot qua Playwright và xác nhận không có màu background không phù hợp.

---

## Kiến trúc & Convention

### Backend (.NET 10)
- **Naming:** DB tables `snake_case` plural; API endpoints `/api/v1/{resource}` kebab-case plural; JSON fields `camelCase`
- **Migration naming:** `V{epic}{seq}_{description}` — ví dụ `V008_001_add_issue_columns`
- **CQRS:** Query → DTO; Command → `Result<Guid>` hoặc `Result<Dto>`
- **Immutability:** TimeEntry không có UPDATE — điều chỉnh qua bản ghi mới (`supersedes_id`)
- **Optimistic locking:** ETag/If-Match; 409 Conflict với inline reconciliation dialog

### Frontend (Angular 21)
- **State management:** NgRx store/effects từ Sprint 1 — không dùng service-level state
- **Components:** Standalone components, lazy-loaded feature routes
- **Naming:** Angular 21 style — file ngắn gọn, kebab-case
- **Gantt:** Bryntum adapter layer bắt buộc — không call Bryntum API trực tiếp từ component

### Database (PostgreSQL)
- **Issue model (Phase 2+):** Table `issues` (rename từ `project_tasks` qua expand-contract). Không dùng `project_tasks` trực tiếp sau V008_003.
- **Custom fields:** JSONB column trên `issues` — không dùng EAV
- **Full-text search:** `tsvector` + GIN index — không Elasticsearch cho MVP

---

## Phase Overview

| Phase | Epics | Stories | Mô tả |
|---|---|---|---|
| Phase 1 | 1-7 | 36 | Core PM tool: Auth, Gantt, Workforce, TimeEntry, Overload, Capacity, Cost, Notifications |
| Phase 2 | 8-11 | 31 | Agile: Issue Migration (BLOCKER 8.0), Board, Collaboration, Workflow Engine |
| Phase 3 | 12-14 | 18 | Search/FTS, Agile Reports, Custom Fields/Labels |
| Phase 4 | 15 | 7 | Automation, Webhooks, Permissions |

**Critical path:** Story 8.0 (Issue Migration) phải hoàn thành trước BẤT KỲ story nào của Epics 9-15.
