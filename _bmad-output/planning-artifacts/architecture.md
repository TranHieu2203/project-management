---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7]
inputDocuments: ['_bmad-output/planning-artifacts/prd.md', '_bmad-output/planning-artifacts/prd-dashboard.md']
workflowType: 'architecture'
project_name: 'project-management'
user_name: 'HieuTV-Team-Project-Management'
date: '2026-04-25'
lastUpdated: '2026-04-29'
---

# Tài Liệu Quyết Định Kiến Trúc

_Tài liệu này được xây dựng cộng tác qua từng bước khám phá. Các phần được bổ sung khi chúng ta cùng đi qua từng quyết định kiến trúc._

---

## Phần 1: Phân Tích Ngữ Cảnh Dự Án

### 1.1 Tổng Quan Dự Án

| Thuộc tính | Giá trị |
|---|---|
| Tên dự án | project-management |
| Loại ứng dụng | Angular SPA nội bộ |
| Quy mô người dùng | ~20 người (PM, Vendor Lead, Admin) |
| Mô hình triển khai | Greenfield |
| Quy mô team | 3-4 developers |
| Kế hoạch phát triển | Sprint 1–7+ (staged rollout) |
| Độ phức tạp | Medium-High |

### 1.2 Phân Tích Yêu Cầu Chức Năng (17 FRs)

#### Nhóm 1: Quản Lý Dự Án & Gantt
- **FR-01**: Gantt chart tương tác với drag-drop, resize task, milestone, phase grouping
- **FR-02**: Phụ thuộc task (FS, SS, FF, SF) với auto-scheduling cascade
- **FR-03**: Critical path highlighting, task progress visualization
- **FR-04**: Holiday calendar (admin-configurable, auto-shift deadline với cascade)

#### Nhóm 2: Quản Lý Thời Gian Đa Tầng
- **FR-05**: TimeEntry model 3 tầng: `estimated → pm-adjusted → vendor-confirmed`
- **FR-06**: Audit trail bất biến — mỗi thay đổi tạo bản ghi mới, không ghi đè
- **FR-07**: Rate snapshot tại thời điểm nhập — `rate_at_time` field, không tính toán hồi tố
- **FR-08**: Công thức chi phí: `Hourly Rate = Monthly Rate ÷ 176h`, rate chỉ thay đổi theo tháng

#### Nhóm 3: Phát Hiện Quá Tải
- **FR-09**: OL-01: >8h/ngày | OL-02: >40h/tuần | OL-03: quy tắc độc lập per-resource
- **FR-10**: Cross-project aggregation — phải tổng hợp trên TẤT CẢ dự án đồng thời
- **FR-11**: Predictive traffic-light overload (green/yellow/orange/red) TRƯỚC khi xác nhận gán task
- **FR-12**: 4-Week rolling capacity forecast (server-side precompute)

#### Nhóm 4: Quản Lý Vendor & Import
- **FR-13**: Vendor CSV import pipeline — async: upload → job_id → poll → confirm
- **FR-14**: Mapping template per vendor (tên cột khác nhau giữa vendors)
- **FR-15**: Smart Assignment Suggestion — rule-based engine (data model từ Sprint 1, UI Phase 2)

#### Nhóm 5: Báo Cáo & Xuất
- **FR-16**: PDF export via Puppeteer — async queue/worker, không đồng bộ HTTP
- **FR-17**: Cost tracking đa chiều: theo vendor, theo dự án, theo phase, theo tháng

### 1.3 Yêu Cầu Phi Chức Năng (NFRs)

| NFR | Mô tả | Threshold |
|---|---|---|
| NFR-01 | Hiệu năng Gantt | Render <2s với 500 tasks |
| NFR-02 | Đồng bộ dữ liệu | Polling 30-60s, không WebSocket |
| NFR-03 | Xác thực | JWT 8h session, SSO-ready (OIDC/SAML) |
| NFR-04 | Audit trail | Mọi mutation đều được ghi log, không thể xóa |
| NFR-05 | Đa trình duyệt | Chrome, Edge, Firefox latest |
| NFR-06 | Khả năng mở rộng | Kiến trúc sẵn sàng cho >20 users sau Phase 2 |
| NFR-07 | Bảo mật | Role-based: PM, Vendor Lead, Admin |

### 1.4 Thách Thức Kỹ Thuật Đặc Biệt

| # | Thách thức | Rủi ro | Ghi chú |
|---|---|---|---|
| T-01 | Custom Gantt Chart | **CAO** | Nếu tự build: 5-8 sprints; nếu dùng thư viện: 2-3 sprints |
| T-02 | Cross-project Overload Engine | **CAO** | Phải aggregation realtime trên nhiều dự án |
| T-03 | Bi-temporal Rate Model | **TRUNG BÌNH-CAO** | Rate snapshot + month-boundary change + hồi tố audit |
| T-04 | Vendor CSV Import Pipeline | **TRUNG BÌNH** | Async job, mapping template, error handling |
| T-05 | Conflict Resolution (Polling) | **TRUNG BÌNH** | Optimistic locking + inline reconciliation |
| T-06 | Puppeteer PDF Export | **TRUNG BÌNH** | Async queue, worker process riêng |

### 1.5 Ràng Buộc Kiến Trúc

- **Angular SPA** — đã quyết định, không thảo luận lại
- **Polling 30-60s** — không WebSocket (ràng buộc hạ tầng)
- **~20 users** — không cần WebSocket, nhưng cross-project visibility vẫn là thách thức
- **3-4 developers** — ưu tiên giải pháp giảm rủi ro, không tối ưu hóa sớm
- **Staged rollout** — Sprint 1-7+, MVP tập trung vào core flow

---

## Phần 2: Quyết Định Kiến Trúc Sơ Bộ (Từ Party Mode)

_8 quyết định kiến trúc quan trọng được xác định qua phân tích đa chiều với các chuyên gia BMAD._

### AD-01: Gantt Chart — Thư Viện vs Tự Build

**Quyết định**: Sử dụng thư viện Gantt thương mại (Bryntum) thay vì tự build

| Thư viện | License | Giá | Đánh giá |
|---|---|---|---|
| Bryntum | Commercial | ~$1,499/dev | Tốt nhất — đầy đủ tính năng, Angular native |
| dhtmlx-gantt | Commercial | ~$1,500/dev | Trưởng thành nhất, Angular wrapper phức tạp |
| GSTC | MIT | Free | Sparse docs, rủi ro support |
| ngx-gantt | MIT | Free | Angular-native nhưng tính năng hạn chế |
| frappe-gantt | MIT | Free | **Loại bỏ** — không đủ tính năng |

**Lý do**: Custom build tốn 5-8 sprints (quá nhiều cho team 3-4 người). Bryntum rút xuống 2-3 sprints.

**Yêu cầu bổ sung**: Bất kể thư viện nào, cần **Adapter Layer + Event Bridge + NgZone wrapper** để tích hợp an toàn với Angular Change Detection.

**Phụ thuộc**: Cần quyết định ngân sách license trước Sprint 1. Nếu không có ngân sách: chuyển sang GSTC/ngx-gantt với scope reduction.

---

### AD-02: State Management — NgRx vs RxJS Thuần

**Quyết định**: **NgRx** (không phải RxJS BehaviorSubject)

**Lý do**:
- Cross-project overload detection yêu cầu shared state giữa nhiều feature modules
- Polling 30-60s cần single source of truth cho cache invalidation
- Conflict resolution (optimistic locking) cần predictable state transitions
- Pain point của RxJS bắt đầu xuất hiện từ Sprint 3-5 khi cross-project state phức tạp

**Khi nào triển khai**: NgRx từ Sprint 1 — không refactor sau.

---

### AD-03: Rate Snapshot — Bi-temporal TimeEntry Model

**Quyết định**: `rate_at_time` field trong mỗi TimeEntry, snapshot tại thời điểm tạo

**Mô hình dữ liệu bắt buộc**:
```
TimeEntry {
  id, resource_id, project_id, task_id
  date, hours
  entry_type: 'estimated' | 'pm_adjusted' | 'vendor_confirmed'
  estimated_hours, adjusted_hours, actual_hours (computed)
  rate_at_time: Decimal  // SNAPSHOT — không thể null
  cost_at_time: Decimal  // = hours × rate_at_time
  entered_by: UserId     // người nhập (khác resource_id)
  created_at: Timestamp
  // Không có updated_at — immutable
}
```

**Quy tắc bất biến**: Không UPDATE TimeEntry. Chỉ INSERT bản ghi mới với `supersedes_id` nếu cần điều chỉnh.

**Công thức**: `Hourly Rate = Monthly Rate ÷ 176h` — chỉ thay đổi tại month boundary.

---

### AD-04: Cross-Project Resource Visibility

**Quyết định**: **Cần clarification session trước Sprint 1**

**Vấn đề chưa được giải quyết trong PRD**:
- PM của Project A có thể xem allocation của resource trên Project B không?
- Overload detection có hiển thị tên dự án gây quá tải không?
- Có role "Resource Manager" với quyền xem toàn bộ không?

**Tác động kiến trúc**: Row-level security, API permission model, UI disclosure rules — tất cả phụ thuộc vào quyết định này.

**Hành động**: Tổ chức clarification session với stakeholder trước Sprint 1 Planning.

---

### AD-05: Predictive Traffic-Light Overload

**Quyết định**: Deterministic rule-based (không phải ML), ship với "beta" label

**Ngưỡng**:
- 🟢 Green: <80% capacity
- 🟡 Yellow: 80-95%
- 🟠 Orange: 95-105%
- 🔴 Red: >105%

**Metric đánh giá**: False positive rate (không phải accuracy) — vì false negative nguy hiểm hơn false positive.

**Tracking cần thiết**: Log mỗi lần PM override cảnh báo → dữ liệu để tinh chỉnh ngưỡng sau.

---

### AD-06: Conflict Resolution (Polling-Based Sync)

**Quyết định**: Optimistic locking + inline reconciliation

**Cơ chế**:
1. Mỗi entity có `version` field (ETag-style)
2. Client gửi `If-Match: <version>` trong mỗi mutation request
3. Server từ chối `409 Conflict` nếu version không khớp
4. Client hiển thị inline reconciliation dialog: "Ai đó đã thay đổi X. Giá trị mới nhất: Y. Giữ thay đổi của bạn hay cập nhật?"
5. Audit trail ghi lại cả hai phiên bản

**Tần suất polling**: 30s cho dữ liệu critical (TimeEntry, Task status), 60s cho dữ liệu phụ (comments, metadata).

---

### AD-07: Re-scope Phase 1 — Defer UI Phức Tạp

**Quyết định**: Giảm scope Phase 1, defer các UI phức tạp sang Phase 2

| Feature | Phase 1 | Phase 2 |
|---|---|---|
| Smart Assignment | Data model + API | UI suggestions panel |
| 4-Week Forecast | Backend precompute | Frontend chart |
| Email Digest | Email template | Scheduling + preferences |
| Advanced Reporting | Raw data export | Visual dashboard |

**Lý do**: Team 3-4 người, Sprint 1-7 không đủ để ship tất cả với chất lượng cao. Ưu tiên core flow hoàn chỉnh hơn nhiều feature nửa vời.

---

### AD-08: Documentation Contract (P0 Before Sprint 1)

**Quyết định**: 3 tài liệu bắt buộc phải hoàn thành trước Sprint 1

1. **TimeEntry State Machine** — Sơ đồ trạng thái đầy đủ: `estimated → pm_adjusted → vendor_confirmed`, điều kiện transition, ai được phép transition
2. **Rate Snapshot Contract** — Worked examples với số liệu cụ thể: "Resource A tháng 3: $1,000/tháng → $5.68/h; TimeEntry ngày 15/3: 8h × $5.68 = $45.45"
3. **Overload Rules Specification** — Quy tắc OL-01/02/03 với edge cases: "Resource có partial allocation (0.5 FTE) trên 2 dự án thì tính thế nào?"

**Hình thức**: BDD Functional Spec (Given/When/Then) + Technical Contract (field-level types, invariants, error codes).

---

## Phần 3: Ngữ Cảnh Triển Khai Chi Tiết

### 3.1 Mô Hình Dữ Liệu Cốt Lõi

```
Project
  ├── Phase[]
  │   └── Task[]
  │       ├── TaskDependency[] (FS|SS|FF|SF)
  │       └── TimeEntry[] (immutable log)
  │           ├── estimated_hours
  │           ├── adjusted_hours (pm_adjusted)
  │           ├── actual_hours (vendor_confirmed, computed)
  │           ├── rate_at_time (snapshot)
  │           └── cost_at_time (snapshot)
  ├── Resource[]
  │   ├── MonthlyRate[] (bi-temporal)
  │   └── Allocation[] (cross-project)
  └── VendorImportJob[]
      ├── status: pending|processing|needs_review|completed|failed
      └── VendorImportRow[] (with mapping template)
```

### 3.2 Các Ranh Giới Module Angular

```
src/
├── core/                     # Guards, interceptors, auth, NgRx root store
├── shared/                   # Shared components, pipes, directives
├── features/
│   ├── gantt/                # Gantt chart + Adapter Layer
│   ├── time-tracking/        # TimeEntry CRUD + state machine
│   ├── capacity/             # Overload detection + forecast
│   ├── vendor-import/        # CSV pipeline + mapping
│   ├── assignment/           # Smart assignment (data model Phase 1)
│   └── reporting/            # PDF export + cost dashboard
└── admin/                    # Holiday calendar, user/vendor management
```

### 3.3 Những Điểm Cần Làm Rõ Trước Sprint 1

1. **Cross-project visibility** — PM Project A có xem được resource allocation trên Project B không?
2. **SSO requirement** — OIDC hay SAML? Có IdP sẵn (Azure AD, Okta) không?
3. **Gantt license budget** — Bryntum/dhtmlx hay MIT libraries?
4. **Overload partial allocation** — Resource 0.5 FTE trên 2 dự án: tính 4h/ngày hay 8h/ngày là limit?
5. **TimeEntry immutability exception** — Có trường hợp nào được phép xóa TimeEntry không (GDPR, sai nghiêm trọng)?

---

## Phần 4: Starter Template

### 4.1 Stack Kỹ Thuật Được Xác Nhận

| Layer | Công nghệ | Phiên bản (04/2026) |
|---|---|---|
| Frontend Framework | Angular CLI | **21.2.10** (LTS đến 05/2027) |
| UI Component Library | Angular Material | 21.x |
| State Management | NgRx | **21.1.0** |
| Backend Framework | ASP.NET Core | **.NET 10** |
| ORM | Entity Framework Core | **10.x** |
| Database | PostgreSQL | Latest stable |
| Test Runner (Frontend) | Vitest | Mặc định Angular 21 |
| Test Framework (Backend) | xUnit | .NET 10 bundled |

### 4.2 Quyết Định Cấu Trúc: Hướng B — Modular Monolith Thủ Công

**Quyết định**: Xây cấu trúc multi-module .NET solution từ đầu (không dùng Jason Taylor template làm base).

**Lý do**:
- Jason Taylor template tạo single-module (1 `ApplicationDbContext`, 1 `Domain`, 1 `Infrastructure`). Refactor sang multi-module tốn effort tương đương xây mới nhưng thêm nợ kỹ thuật.
- Mỗi module cần `DbContext` riêng biệt — không thể share. Tách sau = migration hell.
- Test hierarchy sạch từ ngày 0: `Modules.Projects.Application.Tests/` — không cần restructure khi tách module.
- Jason Taylor template vẫn được dùng làm **tài liệu tham khảo** cho CQRS/MediatR wiring pattern, không dùng làm base project.

**Điều kiện từ Party Mode (Winston + John đồng thuận)**:
- Sprint 1-2 phải có user-facing deliverable: Authentication + Projects CRUD + deployable lên staging
- Sprint 2 kết thúc bằng demo session 30 phút với stakeholder (họ tự tay dùng)
- Definition of "task list scope" phải được làm rõ trước Sprint 1 kick off

### 4.3 Cấu Trúc Solution Backend (.NET)

```
ProjectManagement.sln
├── src/
│   ├── Shared/
│   │   └── ProjectManagement.Shared/          ← ValueObjects, Interfaces, Exceptions, Base Entities
│   │
│   ├── Modules/
│   │   ├── Projects/
│   │   │   ├── Projects.Domain                ← Entities, Domain Events, Aggregates
│   │   │   ├── Projects.Application           ← CQRS Commands/Queries/Handlers (MediatR)
│   │   │   ├── Projects.Infrastructure        ← ProjectsDbContext, Repositories, EF Migrations
│   │   │   └── Projects.Api                   ← Controllers/Minimal API Endpoints (class lib)
│   │   │
│   │   ├── TimeTracking/
│   │   │   ├── TimeTracking.Domain
│   │   │   ├── TimeTracking.Application
│   │   │   ├── TimeTracking.Infrastructure    ← TimeTrackingDbContext (riêng biệt)
│   │   │   └── TimeTracking.Api
│   │   │
│   │   ├── Capacity/                          ← Overload detection + 4-week forecast
│   │   ├── VendorImport/                      ← CSV pipeline + async jobs
│   │   └── Reporting/                         ← PDF export + cost dashboard
│   │
│   └── Host/
│       └── ProjectManagement.Host             ← ASP.NET Core WebAPI
│           ← Add Project Reference: Projects.Api, TimeTracking.Api, Capacity.Api, ...
│           ← DI registration, middleware pipeline, health checks, Serilog
│
├── tests/
│   ├── Modules.Projects.Application.Tests/
│   ├── Modules.Projects.Infrastructure.Tests/
│   ├── Modules.TimeTracking.Application.Tests/
│   └── Integration.Tests/
│
└── frontend/
    └── project-management-web/                ← Angular 21 SPA (độc lập)
```

**Nguyên tắc microservice-ready**: Khi cần scale, tách `Projects.Api` + `Projects.Infrastructure` ra WebAPI host riêng mà không cần refactor code bên trong module.

### 4.4 Khởi Tạo Frontend (Angular)

```bash
ng new project-management-web \
  --style=scss \
  --ssr=false \
  --routing=true

cd project-management-web
ng add @angular/material
ng add @ngrx/store@latest
ng add @ngrx/effects@latest
ng add @ngrx/entity@latest
ng add @ngrx/devtools@latest
```

**Lưu ý Angular 21**:
- Naming style `2025`: file ngắn gọn (`app.ts`, `home.ts` thay vì `app.component.ts`)
- Standalone components mặc định (không NgModules)
- Vitest thay Karma/Jasmine

### 4.5 Pattern Sprint 1-2 (Living Reference)

```
Sprint 1: Shared + Host skeleton + Auth end-to-end + Projects list/detail (read-only)
Sprint 2: Projects CRUD + basic task list → deployable staging → demo session stakeholder
Sprint 3+: Replicate Projects module pattern cho TimeTracking, Capacity, v.v.
```

Module Projects trở thành **living reference implementation** — không phải external template.

---

## Phần 5: Quy Tắc Nhất Quán (Implementation Patterns)

_Các quy tắc này bắt buộc với tất cả AI agents và developers để đảm bảo code nhất quán xuyên suốt dự án._

### 5.1 Naming Conventions

#### Database (PostgreSQL)
| Đối tượng | Convention | Ví dụ |
|---|---|---|
| Table | `snake_case`, số nhiều | `time_entries`, `monthly_rates`, `vendor_import_jobs` |
| Column | `snake_case` | `resource_id`, `rate_at_time`, `created_at` |
| Primary key | `id` (UUID) | `id uuid PRIMARY KEY DEFAULT gen_random_uuid()` |
| Foreign key | `{table_singular}_id` | `project_id`, `resource_id` |
| Index | `ix_{table}_{columns}` | `ix_time_entries_resource_date` |
| Unique constraint | `uq_{table}_{columns}` | `uq_resources_email` |
| Schema | `snake_case`, theo module | `time_tracking`, `vendor_import` |

#### API (REST)
| Đối tượng | Convention | Ví dụ |
|---|---|---|
| Endpoint | `/api/v1/{resource}`, kebab-case, số nhiều | `/api/v1/time-entries`, `/api/v1/vendor-import-jobs` |
| Route param | `{camelCase}` | `/api/v1/projects/{projectId}` |
| Query param | `camelCase` | `?resourceId=&startDate=` |
| JSON field (request/response) | `camelCase` | `resourceId`, `rateAtTime`, `createdAt` |
| HTTP methods | Chuẩn REST: GET/POST/PUT/PATCH/DELETE | PATCH cho partial update TimeEntry |

**.NET JSON config bắt buộc** (trong `Program.cs`):
```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
```

#### C# Code (.NET)
| Đối tượng | Convention | Ví dụ |
|---|---|---|
| Class, Record, Interface | `PascalCase` | `TimeEntry`, `ITimeEntryRepository` |
| Method | `PascalCase` | `GetByResourceAsync()` |
| Public property | `PascalCase` | `RateAtTime`, `EntryType` |
| Private field | `_camelCase` | `_dbContext`, `_mediator` |
| Local variable / param | `camelCase` | `timeEntry`, `projectId` |
| Constant | `PascalCase` | `MaxWorkingHoursPerDay` |
| Interface prefix | `I` | `ITimeEntryRepository` |
| Command/Query suffix | `Command` / `Query` | `CreateTimeEntryCommand`, `GetProjectQuery` |
| Handler suffix | `Handler` | `CreateTimeEntryHandler` |
| Namespace | `ProjectManagement.{Module}.{Layer}` | `ProjectManagement.TimeTracking.Application` |

#### TypeScript/Angular
| Đối tượng | Convention | Ví dụ |
|---|---|---|
| Component class | `PascalCase` + suffix | `TimeEntryListComponent` |
| Component file (style 2025) | `kebab-case.ts` | `time-entry-list.ts` |
| Service class | `PascalCase` + `Service` | `TimeEntryService` |
| NgRx Action | `[Feature] Event Name` | `[TimeTracking] Load Entries Success` |
| NgRx Selector | `select` prefix | `selectCurrentEntries` |
| NgRx Effect | `loadEntries$` | Observable suffix `$` |
| Interface (model) | `PascalCase`, no `I` prefix | `TimeEntry`, `Project`, `Resource` |
| Enum | `PascalCase` | `EntryType`, `OverloadStatus` |
| Variable/param | `camelCase` | `timeEntry`, `projectId` |
| Private field | `camelCase` (no underscore) | `timeEntryService` |
| CSS class | `kebab-case`, BEM-light | `time-entry-card`, `time-entry-card--overloaded` |

---

### 5.2 Structure Patterns

#### CQRS Pattern (bắt buộc mọi Application layer)
```
Modules/{Module}/Application/
  ├── Commands/
  │   └── CreateTimeEntry/
  │       ├── CreateTimeEntryCommand.cs      ← record với properties
  │       ├── CreateTimeEntryCommandValidator.cs  ← FluentValidation
  │       └── CreateTimeEntryHandler.cs      ← IRequestHandler<Command, Result>
  ├── Queries/
  │   └── GetTimeEntriesByResource/
  │       ├── GetTimeEntriesByResourceQuery.cs
  │       ├── GetTimeEntriesByResourceHandler.cs
  │       └── TimeEntryDto.cs                ← DTO riêng cho query result
  └── Common/
      └── Interfaces/
          └── ITimeEntryRepository.cs
```

**Quy tắc bất biến**:
- Command không trả về domain object — trả về `Result<Guid>` hoặc `Result<Dto>`
- Query không dùng domain entity trực tiếp — dùng DTO
- Handler không inject Repository trực tiếp — inject qua Interface

#### Angular Feature Module Structure
```
src/features/{feature}/
  ├── {feature}.routes.ts          ← lazy load route definition
  ├── components/
  │   ├── {feature}-list/
  │   │   ├── {feature}-list.ts   ← standalone component
  │   │   ├── {feature}-list.html
  │   │   └── {feature}-list.scss
  │   └── {feature}-form/
  ├── services/
  │   └── {feature}.service.ts    ← HTTP calls only
  ├── store/
  │   ├── {feature}.actions.ts
  │   ├── {feature}.reducer.ts
  │   ├── {feature}.effects.ts
  │   └── {feature}.selectors.ts
  └── models/
      └── {feature}.model.ts      ← TypeScript interfaces
```

---

### 5.3 API Response Format

**Success — List:**
```json
{
  "items": [...],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 20
}
```

**Success — Single item:** trả thẳng object, không wrap.

**Error — ProblemDetails:**
```json
{
  "type": "ValidationError",
  "title": "Lỗi xác thực dữ liệu",
  "status": 400,
  "errors": {
    "hours": ["Số giờ phải lớn hơn 0"],
    "date": ["Ngày không được trong tương lai"]
  },
  "traceId": "00-abc123def456"
}
```

**HTTP Status Codes:**
| Code | Khi nào dùng |
|---|---|
| 200 | GET/PUT thành công |
| 201 | POST tạo mới thành công — kèm `Location` header |
| 204 | DELETE thành công, không có body |
| 400 | Validation error |
| 401 | Chưa xác thực |
| 403 | Không có quyền |
| 404 | Resource không tồn tại |
| 409 | Conflict (optimistic locking version mismatch) |
| 422 | Business rule violation (khác validation) |
| 500 | Server error — log đầy đủ, không expose detail |

---

### 5.4 State Management Patterns (NgRx)

**Cấu trúc state bắt buộc:**
```typescript
interface FeatureState {
  items: EntityState<Model>;   // dùng @ngrx/entity
  selectedId: string | null;
  loading: boolean;
  error: string | null;
}
```

**Action naming:**
```typescript
// Load lifecycle
'[TimeTracking] Load Entries'
'[TimeTracking] Load Entries Success'
'[TimeTracking] Load Entries Failure'

// CRUD
'[TimeTracking] Create Entry'
'[TimeTracking] Create Entry Success'
'[TimeTracking] Create Entry Failure'
```

**Quy tắc**:
- Component **không** gọi Service trực tiếp — dispatch Action
- Effect xử lý side effects (HTTP) — không có logic trong Reducer
- Selector **luôn** dùng `createSelector` — không truy cập state trực tiếp trong component

---

### 5.5 Error Handling Patterns

**Backend — Hierarchy xử lý:**
```
Domain Exception (business rule) → 422
Validation Exception (FluentValidation) → 400
NotFoundException → 404
ConflictException (version mismatch) → 409
Unhandled Exception → 500 (log full, return generic message)
```

**Frontend — Interceptor chain:**
```
401 → clear token → redirect /login
403 → MatSnackBar "Bạn không có quyền thực hiện thao tác này"
409 → trigger reconciliation dialog (inline, không toast)
422 → map errors → hiển thị inline form validation
500 → MatSnackBar "Lỗi hệ thống. Vui lòng thử lại sau."
```

---

### 5.6 Immutability Rules (Đặc Thù Dự Án)

**TimeEntry — TUYỆT ĐỐI không UPDATE:**
```csharp
// ❌ SAI
_dbContext.TimeEntries.Update(entry);

// ✅ ĐÚNG — tạo bản ghi mới với supersedes_id
var newEntry = entry.CreateRevision(newHours, updatedBy);
_dbContext.TimeEntries.Add(newEntry);
```

**Rate — chỉ thay đổi theo tháng:**
```csharp
// Khi tạo TimeEntry — PHẢI snapshot rate
var hourlyRate = await _rateService.GetHourlyRateAtDateAsync(resourceId, entryDate);
var timeEntry = new TimeEntry(hours, hourlyRate); // rate_at_time = hourlyRate
```

---

### 5.7 Logging Standards

**Serilog log levels:**
| Level | Khi nào |
|---|---|
| `Debug` | Diagnostic trong development |
| `Information` | Business events quan trọng (TimeEntry created, Import completed) |
| `Warning` | Degraded state nhưng vẫn hoạt động (cache miss, retry) |
| `Error` | Exception được handle nhưng cần attention |
| `Fatal` | Application không thể tiếp tục |

**Structured logging bắt buộc:**
```csharp
// ✅ ĐÚNG — structured properties
_logger.Information("TimeEntry created {@Entry} by {UserId}", entry, userId);

// ❌ SAI — string interpolation mất structured data
_logger.Information($"TimeEntry created {entry.Id} by {userId}");
```

---

## Phần 5 — Checklist Enforcement Cho AI Agents

Trước khi submit code, agent phải tự kiểm tra:
- [ ] Database columns dùng `snake_case`
- [ ] JSON response fields dùng `camelCase`
- [ ] API endpoints dùng `kebab-case` số nhiều, prefix `/api/v1/`
- [ ] HTTP 409 cho version conflict, không phải 400
- [ ] TimeEntry không có UPDATE — chỉ INSERT với `supersedes_id`
- [ ] `rate_at_time` không bao giờ null trong TimeEntry
- [ ] NgRx Action format: `[Feature] Event Name`
- [ ] Command/Query/Handler đúng naming suffix
- [ ] Serilog dùng structured logging, không string interpolation
- [ ] **[CC-01]** Không import `MatSnackBar` trong component mới — dùng `FeedbackDialogService` (xem AD-16)
- [ ] **[CC-01]** Error feedback phải truyền `HttpErrorResponse` gốc vào `feedbackDialog.error(msg, err)` để extract `traceId`
- [ ] **[CC-01]** Success feedback dùng `feedbackDialog.success(msg)` — không dùng dialog thủ công

---

## Phần 6: Quyết Định Kiến Trúc Cốt Lõi

### 5.1 Data Architecture

| # | Quyết định | Lựa chọn | Lý do |
|---|---|---|---|
| D-01 | EF Core Migration | **Mỗi module tự quản** trong `Infrastructure/Migrations/` | Module độc lập, microservice-ready. Host gọi `MigrateAsync()` từng DbContext khi startup |
| D-02 | Caching | **IMemoryCache** + `ICacheService` interface | Đủ cho 20 users. Interface cho phép swap sang Redis sau mà không refactor business logic |
| D-03 | Cross-module Query | **Cross-DbContext qua DI** + MediatR Notifications làm abstraction | Đơn giản cho Phase 1. Notification thay bằng message bus khi tách microservice |

**Cache targets**: Capacity forecast (4-week rolling), Holiday calendar, Resource monthly rate.

**MediatR Notification pattern cho cross-module**:
```csharp
// TimeTracking publish
await _mediator.Publish(new TimeEntryCreatedNotification(entry));

// Capacity subscribe
public class UpdateCapacityOnTimeEntry : INotificationHandler<TimeEntryCreatedNotification>
```

### 5.2 Authentication & Security

| # | Quyết định | Lựa chọn | Lý do |
|---|---|---|---|
| D-04 | Auth | **ASP.NET Core Identity + JWT Bearer** | Identity quản lý user/role/claim. JWT 8h session. `AddOpenIdConnect()` cấu hình sẵn nhưng chưa bật (SSO-ready) |
| D-05 | Authorization | **Role-based + Resource Ownership Policy** | `[Authorize(Roles = "PM")]` cho role. Custom `IAuthorizationHandler` cho ownership |

**Permission matrix**:
```
Admin      → toàn quyền hệ thống
PM         → CRUD project mình quản lý + xem resource allocation
VendorLead → submit/view TimeEntry của vendor mình
```

**SSO-ready**: Cấu hình OpenID Connect middleware từ Sprint 1 nhưng disabled. Khi cần SSO: enable + cung cấp IdP config (Azure AD, Okta).

### 5.3 API Design

| # | Quyết định | Lựa chọn | Lý do |
|---|---|---|---|
| D-06 | API style | **Controller-based** | Team intermediate, quen thuộc. Minimal API bổ sung sau nếu cần |
| D-07 | API versioning | **Không version Phase 1** — dùng prefix `/api/v1/` trong route | Internal tool, single consumer. Prefix sẵn để thêm v2 sau, không cần versioning library |

**Error response chuẩn — ProblemDetails (.NET 10 built-in)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Lỗi xác thực",
  "status": 400,
  "errors": { "field": ["message"] },
  "traceId": "00-abc123"
}
```

**Global exception handler**: Middleware bắt tất cả exception → map sang `ProblemDetails` → log Serilog → trả về response nhất quán.

### 5.4 Frontend Architecture

| # | Quyết định | Lựa chọn | Lý do |
|---|---|---|---|
| D-08 | Lazy loading | **Mỗi feature module lazy load** | `loadChildren` cho gantt, time-tracking, capacity, vendor-import — bundle tách nhỏ |
| D-09 | HTTP layer | **HttpClient + Interceptor chain** | Auth (gắn JWT) → Error (handle 401/409/500) → Retry (polling resilience) |
| D-10 | Error handling | **Global ErrorHandler + MatSnackBar** | `ErrorHandler` bắt unhandled exception. `MatSnackBar` cho user-facing notification |

### 5.5 Infrastructure & Deployment

| # | Quyết định | Lựa chọn | Lý do |
|---|---|---|---|
| D-11 | Deployment | **Docker + docker-compose** | Dev: `docker-compose up` (PostgreSQL + API). Prod: container image linh hoạt |
| D-12 | Logging | **Serilog + File sink (Phase 1)** | Structured JSON logging. Dev: Console sink. Prod: File sink rotating daily. Seq/App Insights Phase 2 |
| D-13 | Background jobs | **IHostedService + Channel\<T\>** | PDF export queue + CSV import queue — built-in .NET 10, không cần Hangfire cho Phase 1 |

**Background job pattern**:
```csharp
// Channel<T> làm in-memory queue
services.AddSingleton<Channel<PdfExportJob>>(
    Channel.CreateBounded<PdfExportJob>(100));
services.AddHostedService<PdfExportWorker>();
```

### 5.6 Kiến Trúc Tổng Thể

```
Angular 21 SPA (project-management-web/)
  ├── HttpClient + Interceptor chain (Auth/Error/Retry)
  ├── NgRx Store (cross-project state, polling cache)
  ├── Angular Material + SCSS
  └── Lazy-loaded feature modules
        ↓ REST/JSON HTTPS (/api/v1/*)
ASP.NET Core .NET 10 (ProjectManagement.Host)
  ├── JWT Bearer + OpenID Connect (SSO-ready, disabled)
  ├── Global Exception Middleware → ProblemDetails
  ├── Serilog (Console/File sink)
  ├── IHostedService workers (PDF queue, CSV queue)
  ├── IMemoryCache (Capacity forecast, Holiday, Rate)
  └── Module DI registration
        ↓ EF Core 10 (DbContext riêng mỗi module)
PostgreSQL
  ├── projects schema      (Projects, Tasks, Phases, TaskDependencies)
  ├── time_tracking schema (TimeEntries — immutable, MonthlyRates)
  ├── capacity schema      (Allocations, CapacityCache)
  └── vendor_import schema (ImportJobs, ImportRows, VendorMappings)
```

---

## Phần 7: Cấu Trúc Dự Án & Ranh Giới

### 7.1 Cấu Trúc Backend (.NET 10 Modular Monolith)

```
ProjectManagement.sln
├── src/
│   ├── Shared/
│   │   ├── ProjectManagement.Shared.Domain/
│   │   │   ├── Entities/
│   │   │   │   ├── BaseEntity.cs           ← Id (Guid), CreatedAt, UpdatedAt
│   │   │   │   └── AuditableEntity.cs      ← + CreatedBy, UpdatedBy, IsDeleted
│   │   │   ├── Results/
│   │   │   │   ├── Result.cs               ← Result.Success() / Result.Failure(error)
│   │   │   │   └── Result{T}.cs            ← Result<T>.Success(value) / Result<T>.Failure(error)
│   │   │   └── Exceptions/
│   │   │       ├── DomainException.cs      ← base, map → HTTP 422
│   │   │       ├── NotFoundException.cs    ← map → HTTP 404
│   │   │       └── ConflictException.cs    ← map → HTTP 409
│   │   └── ProjectManagement.Shared.Infrastructure/
│   │       ├── Services/
│   │       │   ├── ICacheService.cs        ← interface (Get/Set/Remove)
│   │       │   └── MemoryCacheService.cs   ← IMemoryCache impl
│   │       ├── Persistence/
│   │       │   └── IUnitOfWork.cs          ← cross-repo transaction interface
│   │       └── Middleware/
│   │           ├── GlobalExceptionMiddleware.cs   ← map exceptions → ProblemDetails
│   │           └── CorrelationIdMiddleware.cs     ← inject X-Correlation-Id vào logs
│   │
│   ├── Modules/
│   │   │
│   │   ├── Projects/                        ← FR-01, FR-02, FR-03, FR-04, FR-17
│   │   │   ├── ProjectManagement.Projects.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── Project.cs
│   │   │   │   │   ├── ProjectTask.cs
│   │   │   │   │   ├── Phase.cs
│   │   │   │   │   ├── Assignment.cs       ← vendor-resource assignment
│   │   │   │   │   └── TaskDependency.cs
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   └── ProjectStatus.cs    ← Planning/Active/OnHold/Completed
│   │   │   │   └── Events/
│   │   │   │       └── ProjectCreatedEvent.cs
│   │   │   ├── ProjectManagement.Projects.Application/
│   │   │   │   ├── Projects/
│   │   │   │   │   ├── Commands/
│   │   │   │   │   │   ├── CreateProject/CreateProjectCommand.cs + Handler
│   │   │   │   │   │   ├── UpdateProject/UpdateProjectCommand.cs + Handler
│   │   │   │   │   │   └── UpdateProjectStatus/UpdateProjectStatusCommand.cs + Handler
│   │   │   │   │   └── Queries/
│   │   │   │   │       ├── GetProjectById/GetProjectByIdQuery.cs + Handler + Dto
│   │   │   │   │       └── GetProjectList/GetProjectListQuery.cs + Handler + Dto
│   │   │   │   ├── Tasks/
│   │   │   │   │   ├── Commands/CreateTask/, UpdateTask/, ReorderTasks/
│   │   │   │   │   └── Queries/GetTasksByProject/
│   │   │   │   └── Assignments/
│   │   │   │       └── Commands/AssignVendor/, RemoveAssignment/
│   │   │   ├── ProjectManagement.Projects.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── ProjectsDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   │   ├── ProjectConfiguration.cs     ← EF fluent config, snake_case
│   │   │   │   │   │   ├── TaskConfiguration.cs
│   │   │   │   │   │   └── AssignmentConfiguration.cs
│   │   │   │   │   └── Migrations/                     ← EF migrations riêng module này
│   │   │   │   └── Repositories/
│   │   │   │       ├── IProjectRepository.cs
│   │   │   │       └── ProjectRepository.cs
│   │   │   └── ProjectManagement.Projects.Api/
│   │   │       └── Controllers/
│   │   │           ├── ProjectsController.cs           ← /api/v1/projects
│   │   │           ├── TasksController.cs              ← /api/v1/projects/{id}/tasks
│   │   │           └── AssignmentsController.cs        ← /api/v1/projects/{id}/assignments
│   │   │
│   │   ├── TimeTracking/                    ← FR-05, FR-06, FR-07, FR-08
│   │   │   ├── ProjectManagement.TimeTracking.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── TimeEntry.cs         ← IMMUTABLE: chỉ INSERT, không UPDATE
│   │   │   │   │   │                           supersedes_id, rate_at_time (không null)
│   │   │   │   │   └── MonthlyRate.cs
│   │   │   │   └── Events/
│   │   │   │       ├── TimeEntryCreatedNotification.cs  ← MediatR INotification
│   │   │   │       └── TimeEntryRevisedNotification.cs  ← MediatR INotification (supersede)
│   │   │   ├── ProjectManagement.TimeTracking.Application/
│   │   │   │   ├── TimeEntries/
│   │   │   │   │   ├── Commands/
│   │   │   │   │   │   ├── CreateTimeEntry/   ← publish TimeEntryCreatedNotification
│   │   │   │   │   │   └── ReviseTimeEntry/   ← INSERT record mới + publish TimeEntryRevisedNotification
│   │   │   │   │   └── Queries/
│   │   │   │   │       ├── GetAuditTrail/     ← chuỗi supersedes theo lineage
│   │   │   │   │       └── GetTimeEntriesByTask/
│   │   │   │   └── Rates/
│   │   │   │       └── Queries/GetCurrentRate/
│   │   │   ├── ProjectManagement.TimeTracking.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── TimeTrackingDbContext.cs
│   │   │   │   │   ├── Configurations/TimeEntryConfiguration.cs, RateConfiguration.cs
│   │   │   │   │   └── Migrations/
│   │   │   │   └── Services/
│   │   │   │       └── RateSnapshotService.cs  ← snapshot rate_at_time khi tạo entry
│   │   │   └── ProjectManagement.TimeTracking.Api/
│   │   │       └── Controllers/
│   │   │           ├── TimeEntriesController.cs   ← /api/v1/time-entries
│   │   │           └── RatesController.cs         ← /api/v1/rates
│   │   │
│   │   ├── Capacity/                        ← FR-09, FR-10, FR-11, FR-12, FR-15
│   │   │   ├── ProjectManagement.Capacity.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── ResourceCapacity.cs
│   │   │   │   │   └── CapacitySnapshot.cs
│   │   │   │   └── ValueObjects/
│   │   │   │       └── OverloadStatus.cs    ← Green/Yellow/Red
│   │   │   ├── ProjectManagement.Capacity.Application/
│   │   │   │   ├── Handlers/
│   │   │   │   │   ├── TimeEntryCreatedHandler.cs   ← INotificationHandler<TimeEntryCreatedNotification>
│   │   │   │   │   └── TimeEntryRevisedHandler.cs   ← INotificationHandler<TimeEntryRevisedNotification>
│   │   │   │   ├── Commands/RecalculateCapacity/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetResourceOverload/         ← trả OverloadStatus cho resource
│   │   │   │       └── GetCapacityForecast/         ← 4-week rolling forecast
│   │   │   ├── ProjectManagement.Capacity.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── CapacityDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   └── Migrations/
│   │   │   │   └── Services/
│   │   │   │       ├── OverloadDetectionService.cs  ← triggered bởi MediatR handler
│   │   │   │       └── CapacityCacheService.cs      ← wraps ICacheService, TTL 4h
│   │   │   └── ProjectManagement.Capacity.Api/
│   │   │       └── Controllers/
│   │   │           └── CapacityController.cs        ← /api/v1/capacity/resources/{id}/overload
│   │   │
│   │   ├── VendorImport/                    ← FR-13, FR-14
│   │   │   ├── ProjectManagement.VendorImport.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── ImportJob.cs         ← status: Pending/Processing/Completed/Failed
│   │   │   │   │   ├── ImportRow.cs
│   │   │   │   │   └── VendorMapping.cs     ← column mapping config per vendor
│   │   │   ├── ProjectManagement.VendorImport.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── StartImport/         ← validate CSV headers, queue job
│   │   │   │   │   └── SaveMapping/         ← lưu vendor column mapping
│   │   │   │   └── Queries/
│   │   │   │       └── GetImportStatus/     ← polling endpoint
│   │   │   ├── ProjectManagement.VendorImport.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── VendorImportDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   └── Migrations/
│   │   │   │   ├── Workers/
│   │   │   │   │   └── CsvProcessingWorker.cs   ← IHostedService + Channel<ImportJob>
│   │   │   │   │                                   includes retry + dead-letter to DB
│   │   │   │   └── Services/
│   │   │   │       ├── CsvValidationService.cs
│   │   │   │       └── MappingEngine.cs
│   │   │   └── ProjectManagement.VendorImport.Api/
│   │   │       └── Controllers/
│   │   │           └── VendorImportController.cs  ← /api/v1/vendor-import-jobs
│   │   │
│   │   └── Reporting/                       ← FR-16, FR-17
│   │       ├── ProjectManagement.Reporting.Domain/
│   │       │   ├── Entities/
│   │       │   │   ├── ReportDefinition.cs
│   │       │   │   └── ExportJob.cs         ← status: Queued/Processing/Ready/Failed
│   │       ├── ProjectManagement.Reporting.Application/
│   │       │   ├── Queries/
│   │       │   │   ├── GetCostSummary/      ← đọc từ ReadModel (xem 7.3)
│   │       │   │   └── GetExportJobStatus/
│   │       │   └── Commands/
│   │       │       └── TriggerPdfExport/    ← queue vào PdfExportWorker
│   │       ├── ProjectManagement.Reporting.Infrastructure/
│   │       │   ├── Persistence/
│   │       │   │   ├── ReportingDbContext.cs
│   │       │   │   ├── Configurations/
│   │       │   │   └── Migrations/
│   │       │   ├── ReadModels/              ← CRITICAL: cross-module data projection
│   │       │   │   ├── CostReportReadModel.cs   ← denormalized view từ nhiều module
│   │       │   │   └── CostReportProjector.cs   ← populate ReadModel từ events
│   │       │   ├── Workers/
│   │       │   │   └── PdfExportWorker.cs   ← IHostedService + Channel<ExportJob>
│   │       │   │                               includes retry + dead-letter to DB
│   │       │   └── Services/
│   │       │       ├── CostCalculationService.cs  ← tính từ ReadModel, không query module khác
│   │       │       └── PdfGenerationService.cs
│   │       └── ProjectManagement.Reporting.Api/
│   │           └── Controllers/
│   │               └── ReportingController.cs     ← /api/v1/reports
│   │
│   └── Host/
│       ├── ProjectManagement.Host.csproj    ← references tất cả *.Api projects
│       ├── Program.cs                       ← DI wiring, migration orchestration
│       ├── Middleware/
│       │   └── (GlobalExceptionMiddleware + CorrelationIdMiddleware → từ Shared)
│       └── Extensions/
│           └── ModuleExtensions.cs          ← AddProjectsModule(), AddTimeTrackingModule()...
│
└── tests/
    ├── UnitTests/
    │   ├── Projects.UnitTests/
    │   ├── TimeTracking.UnitTests/
    │   ├── Capacity.UnitTests/
    │   ├── VendorImport.UnitTests/
    │   └── Reporting.UnitTests/
    └── IntegrationTests/
        ├── Common/
        │   ├── CustomWebApplicationFactory.cs   ← PHẢI CÓ trước khi viết bất kỳ integration test
        │   └── TestContainersFixture.cs         ← PostgreSQL TestContainers fixture
        ├── Projects.IntegrationTests/
        ├── TimeTracking.IntegrationTests/
        ├── Capacity.IntegrationTests/
        └── VendorImport.IntegrationTests/
```

---

### 7.2 Cấu Trúc Frontend (Angular 21)

```
project-management-web/
├── package.json
├── angular.json
├── tsconfig.json
├── tsconfig.app.json
├── .env.example
├── .gitignore
├── Dockerfile
│
└── src/
    ├── index.html
    ├── main.ts                              ← bootstrapApplication()
    ├── styles.scss                          ← Angular Material theme + global SCSS
    │
    └── app/
        ├── app.config.ts                    ← provideRouter, provideStore, provideHttpClient
        ├── app.routes.ts                    ← top-level routes với loadChildren (lazy)
        │
        ├── core/                            ← singleton services, không lazy load
        │   ├── auth/
        │   │   ├── auth.service.ts          ← login/logout/refresh token
        │   │   ├── token.service.ts         ← lưu/đọc JWT từ localStorage
        │   │   └── auth.guard.ts            ← bảo vệ routes cần đăng nhập
        │   ├── interceptors/
        │   │   ├── auth.interceptor.ts      ← gắn Authorization header
        │   │   ├── error.interceptor.ts     ← handle 401/403/409/500
        │   │   └── retry.interceptor.ts     ← retry 3 lần với backoff
        │   ├── guards/
        │   │   └── unsaved-changes.guard.ts ← cảnh báo khi rời Gantt editor
        │   └── store/
        │       ├── app.state.ts             ← root AppState interface
        │       └── router/
        │           ├── router.actions.ts
        │           └── router.reducer.ts
        │
        ├── shared/                          ← components/models dùng chung nhiều features
        │   ├── components/
        │   │   ├── conflict-dialog/         ← hiện khi API trả 409, cho phép reconcile
        │   │   │   └── conflict-dialog.component.ts
        │   │   └── loading-spinner/
        │   │       └── loading-spinner.component.ts
        │   ├── models/
        │   │   ├── api-response.model.ts    ← map với Result<T> backend (success/error shape)
        │   │   ├── pagination.model.ts
        │   │   └── problem-details.model.ts ← ProblemDetails response shape từ .NET
        │   └── utils/
        │       └── date.utils.ts
        │
        └── features/                        ← lazy-loaded feature modules
            │
            ├── auth/                        ← Sprint 1
            │   ├── auth.routes.ts           ← /login, /logout
            │   ├── components/
            │   │   └── login/login.component.ts
            │   └── services/
            │       └── auth-api.service.ts
            │
            ├── projects/                    ← Sprint 1-2 (FR-01, FR-02, FR-04)
            │   ├── projects.routes.ts       ← /projects, /projects/:id
            │   ├── components/
            │   │   ├── project-list/
            │   │   │   └── project-list.component.ts
            │   │   ├── project-detail/
            │   │   │   └── project-detail.component.ts
            │   │   └── project-form/
            │   │       └── project-form.component.ts
            │   ├── store/
            │   │   ├── projects.actions.ts
            │   │   ├── projects.reducer.ts  ← EntityState<Project>
            │   │   ├── projects.effects.ts
            │   │   └── projects.selectors.ts
            │   └── services/
            │       └── projects-api.service.ts
            │
            ├── gantt/                       ← Sprint 3+ (FR-03 — HIGH RISK)
            │   ├── gantt.routes.ts          ← lazy loaded riêng, bundle lớn
            │   ├── gantt.config.ts          ← Bryntum license key + feature config
            │   ├── components/
            │   │   ├── gantt-chart/
            │   │   │   ├── gantt-chart.component.ts
            │   │   │   └── bryntum-adapter.ts   ← NgZone wrapper (runOutsideAngular)
            │   │   └── gantt-toolbar/
            │   │       └── gantt-toolbar.component.ts
            │   └── store/
            │       ├── gantt.actions.ts
            │       ├── gantt.reducer.ts
            │       ├── gantt.effects.ts
            │       └── gantt.selectors.ts
            │
            ├── time-tracking/               ← Sprint 3+ (FR-05, FR-06, FR-07, FR-08)
            │   ├── time-tracking.routes.ts
            │   ├── components/
            │   │   ├── time-entry-form/
            │   │   │   └── time-entry-form.component.ts
            │   │   ├── time-entry-status/
            │   │   │   └── time-entry-status.component.ts  ← state machine UI
            │   │   └── audit-trail-viewer/
            │   │       └── audit-trail-viewer.component.ts ← hiện chuỗi supersedes
            │   └── store/
            │       ├── time-tracking.actions.ts
            │       ├── time-tracking.reducer.ts  ← EntityState<TimeEntry>
            │       ├── time-tracking.effects.ts
            │       └── time-tracking.selectors.ts
            │
            ├── capacity/                    ← Sprint 4+ (FR-09, FR-10, FR-11, FR-12, FR-15)
            │   ├── capacity.routes.ts
            │   ├── components/
            │   │   ├── capacity-dashboard/
            │   │   │   └── capacity-dashboard.component.ts
            │   │   └── traffic-light-widget/
            │   │       └── traffic-light-widget.component.ts  ← Green/Yellow/Red overload
            │   └── store/
            │       ├── capacity.actions.ts
            │       ├── capacity.reducer.ts
            │       ├── capacity.effects.ts     ← polling interval via timer()
            │       └── capacity.selectors.ts
            │
            ├── vendor-import/               ← Sprint 4+ (FR-13, FR-14)
            │   ├── vendor-import.routes.ts
            │   ├── components/
            │   │   ├── csv-upload/
            │   │   │   └── csv-upload.component.ts
            │   │   ├── column-mapping-wizard/
            │   │   │   └── column-mapping-wizard.component.ts
            │   │   └── import-job-status/
            │   │       └── import-job-status.component.ts  ← polling GET /status
            │   └── services/
            │       └── vendor-import-api.service.ts        ← RxJS interval polling, không NgRx
            │
            └── reporting/                   ← Sprint 5+ (FR-16, FR-17)
                ├── reporting.routes.ts
                ├── components/
                │   ├── cost-dashboard/
                │   │   └── cost-dashboard.component.ts
                │   └── pdf-export-status/
                │       └── pdf-export-status.component.ts  ← polling async job
                └── store/
                    ├── reporting.actions.ts
                    ├── reporting.reducer.ts
                    ├── reporting.effects.ts
                    └── reporting.selectors.ts
```

---

### 7.3 Ranh Giới Kiến Trúc

#### API Boundaries

| Boundary | Quy tắc |
|---|---|
| External API | Tất cả đi qua `/api/v1/` prefix. Controller-based. ProblemDetails response format. |
| Authentication | JWT Bearer header. Middleware kiểm tra trước khi vào controller. |
| Authorization | `[Authorize(Roles)]` + `IAuthorizationHandler` resource ownership. |
| Module API exposure | Mỗi module expose controller riêng. Host project reference tất cả `*.Api` projects. |

**Exception → HTTP Status mapping (bắt buộc trong `GlobalExceptionMiddleware`):**

| Exception type | HTTP Status | Ghi chú |
|---|---|---|
| `NotFoundException` | 404 Not Found | Resource không tồn tại |
| `DomainException` | 422 Unprocessable Entity | Vi phạm business rule |
| `ConflictException` | 409 Conflict | Optimistic lock / version conflict |
| `ValidationException` | 400 Bad Request | FluentValidation failure |
| `UnauthorizedException` | 401 Unauthorized | Token invalid/expired |
| `ForbiddenException` | 403 Forbidden | Không đủ quyền |
| `Exception` (base) | 500 Internal Server Error | Log Fatal, trả generic message |

#### Data Boundaries (Cross-Module Access)

**Quy tắc nghiêm ngặt:**

```
✅ Module chỉ đọc DbContext của chính mình
✅ Cross-module WRITE → MediatR Notification (publish/subscribe in-process)
✅ Cross-module READ (Reporting) → ReadModel được denormalize riêng
❌ CẤM Reporting query trực tiếp TimeTrackingDbContext hoặc ProjectsDbContext
❌ CẤM module A inject IRepository của module B
```

**Reporting Read Model strategy:**

```csharp
// Reporting/Infrastructure/ReadModels/CostReportReadModel.cs
// Populated bởi CostReportProjector (subscribe MediatR events)
// Lưu trong reporting schema — denormalized, query-optimized
public class CostReportReadModel
{
    public Guid ProjectId { get; }
    public Guid ResourceId { get; }
    public decimal TotalHours { get; }
    public decimal TotalCost { get; }   // = TotalHours * rate_at_time (snapshotted)
    public DateOnly Month { get; }
    public DateTime LastUpdatedAt { get; }
}
```

#### Component Boundaries (Frontend)

```
core/          → không phụ thuộc vào features
shared/        → không phụ thuộc vào features
features/*     → CHỈ phụ thuộc vào core/ và shared/
features/A     → KHÔNG import trực tiếp từ features/B
```

Cross-feature communication: **NgRx Store** (dispatch action → effect → API → update state).

---

### 7.4 Mapping Yêu Cầu → Cấu Trúc

| FR | Mô tả | Backend Module | Frontend Feature | Sprint |
|---|---|---|---|---|
| FR-01 | Project CRUD | `Modules/Projects/` | `features/projects/` | 1-2 |
| FR-02 | Task management | `Modules/Projects/` | `features/projects/` | 2 |
| FR-03 | Gantt chart | `Modules/Projects/` | `features/gantt/` | 3 |
| FR-04 | Vendor assignment | `Modules/Projects/` | `features/projects/` | 2-3 |
| FR-05 | TimeEntry CRUD | `Modules/TimeTracking/` | `features/time-tracking/` | 3 |
| FR-06 | Multi-tier status | `Modules/TimeTracking/` | `features/time-tracking/` | 3 |
| FR-07 | Audit trail | `Modules/TimeTracking/` | `features/time-tracking/` | 4 |
| FR-08 | Rate snapshot | `Modules/TimeTracking/` | `features/time-tracking/` | 3 |
| FR-09 | Overload detection | `Modules/Capacity/` | `features/capacity/` | 4 |
| FR-10 | Resource planning | `Modules/Capacity/` | `features/capacity/` | 4 |
| FR-11 | Capacity forecast | `Modules/Capacity/` | `features/capacity/` | 5 |
| FR-12 | Holiday calendar | `Modules/Capacity/` | `features/capacity/` | 4 |
| FR-13 | CSV import | `Modules/VendorImport/` | `features/vendor-import/` | 4 |
| FR-14 | Column mapping | `Modules/VendorImport/` | `features/vendor-import/` | 4 |
| FR-15 | Smart assignment | `Modules/Capacity/` | `features/capacity/` | 5 |
| FR-16 | Cost reporting | `Modules/Reporting/` | `features/reporting/` | 5 |
| FR-17 | PDF/Excel export | `Modules/Reporting/` | `features/reporting/` | 5 |

**Cross-cutting concerns:**

| Concern | Backend | Frontend |
|---|---|---|
| Authentication | `Shared.Infrastructure/` + Identity | `core/auth/`, `core/interceptors/auth.interceptor.ts` |
| Error handling | `GlobalExceptionMiddleware` | `core/interceptors/error.interceptor.ts`, `shared/components/conflict-dialog/` |
| Logging | `Serilog` + `CorrelationIdMiddleware` | (Browser console, không cần infra) |
| Caching | `Shared.Infrastructure/ICacheService` | NgRx store (in-memory state) |
| Background jobs | `Host/` wires `IHostedService` workers | Polling via RxJS `timer()` / `interval()` |

---

### 7.5 Điểm Tích Hợp

#### Internal Communication (Backend)

```
TimeTracking → Capacity:
  CreateTimeEntry/ReviseTimeEntry → publish MediatR Notification
  → TimeEntryCreatedHandler / TimeEntryRevisedHandler
  → OverloadDetectionService.RecalculateAsync()
  → CapacityCacheService.InvalidateAsync()

TimeTracking → Reporting:
  CostReportProjector subscribes TimeEntryCreatedNotification
  → update CostReportReadModel (reporting schema)

Projects → (no direct cross-module)
VendorImport → TimeTracking: (Phase 2 — direct insert sau khi mapping)
```

**Transaction boundary (quan trọng):**

```csharp
// MediatR Notification publish xảy ra TRONG transaction của command
// Nếu handler throw → toàn bộ command rollback
// Dùng IDbContextTransaction trong command handler, publish TRƯỚC khi commit

await using var transaction = await _dbContext.Database.BeginTransactionAsync();
var entry = TimeEntry.Create(...);
_dbContext.TimeEntries.Add(entry);
await _dbContext.SaveChangesAsync();           // save trước
await _mediator.Publish(notification, ct);    // publish sau save, trong transaction
await transaction.CommitAsync();              // commit cuối
```

#### External Integrations

| Integration | Loại | Ghi chú |
|---|---|---|
| PostgreSQL | Database | EF Core 10, separate schema per module |
| Bryntum Gantt | Frontend lib (licensed) | NgZone.runOutsideAngular(), spike trước Sprint 3 |
| PDF generation | Server-side | Thư viện TBD (QuestPDF hoặc iTextSharp) |
| SSO IdP | OIDC (Phase 2) | Azure AD / Okta — middleware cấu hình sẵn, disabled |

#### Data Flow (luồng điển hình)

```
1. User submit TimeEntry:
   Angular form → POST /api/v1/time-entries
   → AuthInterceptor (gắn JWT)
   → CreateTimeEntryCommand
   → RateSnapshotService (snapshot rate_at_time)
   → TimeEntry INSERT
   → Publish TimeEntryCreatedNotification
   → Capacity handler recalculate
   → Reporting projector update ReadModel
   → Response 201 Created

2. Vendor import CSV:
   Angular csv-upload → POST /api/v1/vendor-import-jobs (multipart)
   → StartImportCommand → validate headers → INSERT ImportJob
   → Enqueue vào Channel<ImportJob>
   → Response 202 Accepted + jobId
   → Frontend polling GET /api/v1/vendor-import-jobs/{id}/status
   → CsvProcessingWorker dequeue → parse rows → INSERT TimeEntries
   → ImportJob status → Completed

3. Cost report:
   Angular cost-dashboard → GET /api/v1/reports/cost-summary?month=2026-04
   → GetCostSummaryQuery → query CostReportReadModel (reporting schema)
   → Response (không join sang module khác)
```

---

### 7.6 Lưu Ý Quan Trọng Cho AI Agents (Từ Kiến Trúc Review)

Những điểm này được xác định qua Party Mode review và **bắt buộc tuân thủ**:

**[CRITICAL] ReviseTimeEntry phải publish notification:**
```csharp
// ReviseTimeEntryHandler phải publish CẢ HAI notification
// để Capacity tính lại đúng sau khi supersede
await _mediator.Publish(new TimeEntryRevisedNotification(oldEntryId, newEntry), ct);
// TimeEntryRevisedHandler trong Capacity: vô hiệu hóa capacity của entry cũ, cộng entry mới
```

**[CRITICAL] Reporting KHÔNG được cross-query module khác:**
```csharp
// ❌ CẤM
public class GetCostSummaryHandler
{
    private readonly TimeTrackingDbContext _timeDb;  // ← VI PHẠM ranh giới module
}

// ✅ ĐÚNG
public class GetCostSummaryHandler
{
    private readonly ReportingDbContext _reportingDb;  // chỉ đọc ReadModel
}
```

**[CRITICAL] CsvProcessingWorker và PdfExportWorker phải lưu trạng thái job vào DB:**
```csharp
// Nếu app restart, Channel<T> mất. Job phải được persist vào ImportJob/ExportJob table
// Worker lúc startup: load các job có status = Pending từ DB vào Channel
```

**[IMPORTANT] Bryntum Gantt cần spike trước Sprint 3:**
- Tạo branch `spike/bryntum-integration`, estimate bundle size
- Xác nhận NgZone strategy (`runOutsideAngular` để không trigger CD)
- Quyết định `gantt.config.ts` shape trước khi viết component

**[IMPORTANT] `unsaved-changes.guard.ts` phải protect Gantt route:**
```typescript
// Khi user rời /projects/:id/gantt có unsaved changes
// Guard hỏi xác nhận trước khi navigate
```

**[IMPORTANT] `vendor-import` feature KHÔNG dùng NgRx:**
- Chỉ dùng `vendor-import-api.service.ts` với RxJS `interval()` + `takeUntil(destroy$)` để polling
- Không cần state management phức tạp cho flow đơn giản này

---

## Phần 8: Mở Rộng Dashboard & Reporting Module

_Bổ sung từ `prd-dashboard.md` — 2026-04-29. Mở rộng module `Reporting` hiện có thay vì tạo module mới._

### 8.1 Quyết Định Kiến Trúc Dashboard

| # | Quyết định | Lựa chọn | Lý do |
|---|---|---|---|
| DA-01 | Backend placement | Mở rộng module `Reporting` | Tái dụng ReadModel pattern, tránh boilerplate module mới |
| DA-02 | Frontend modules | 2 lazy-loaded modules riêng: `dashboard/` + `reports/` | Bundle isolation — dashboard nhẹ, reports load theo nhu cầu |
| DA-03 | NgRx store | Feature store `dashboard` độc lập | Không coupling với `capacity` store — concerns khác nhau |
| DA-04 | URL filter sync | `@ngrx/router-store` | URL là single source of truth → deep-link shareable tự nhiên, không cần manual sync code |

---

### 8.2 Backend — Mở Rộng Module Reporting

#### 8.2.1 Cấu Trúc Mới Trong Module Reporting

```
Modules/Reporting/
  ├── ProjectManagement.Reporting.Domain/
  │   └── Entities/
  │       ├── Alert.cs                         ← MỚI
  │       └── AlertPreference.cs               ← MỚI
  │
  ├── ProjectManagement.Reporting.Application/
  │   ├── Dashboard/
  │   │   └── Queries/
  │   │       ├── GetProjectsSummary/
  │   │       │   ├── GetProjectsSummaryQuery.cs
  │   │       │   ├── GetProjectsSummaryHandler.cs   ← đọc ProjectSummarySnapshot
  │   │       │   └── ProjectSummaryDto.cs
  │   │       ├── GetUpcomingDeadlines/
  │   │       │   ├── GetUpcomingDeadlinesQuery.cs
  │   │       │   ├── GetUpcomingDeadlinesHandler.cs
  │   │       │   └── DeadlineDto.cs
  │   │       ├── GetStatCards/
  │   │       │   ├── GetStatCardsQuery.cs
  │   │       │   ├── GetStatCardsHandler.cs
  │   │       │   └── StatCardsDto.cs
  │   │       └── GetMyTasksCrossProject/
  │   │           ├── GetMyTasksCrossProjectQuery.cs
  │   │           ├── GetMyTasksCrossProjectHandler.cs
  │   │           └── MyTaskDto.cs
  │   └── Alerts/
  │       ├── Commands/
  │       │   └── MarkAlertRead/
  │       │       ├── MarkAlertReadCommand.cs
  │       │       └── MarkAlertReadHandler.cs
  │       └── Queries/
  │           └── GetMyAlerts/
  │               ├── GetMyAlertsQuery.cs
  │               ├── GetMyAlertsHandler.cs
  │               └── AlertDto.cs
  │
  ├── ProjectManagement.Reporting.Infrastructure/
  │   ├── Persistence/
  │   │   └── ReportingDbContext.cs            ← THÊM DbSets: Alert, AlertPreference, ProjectSummarySnapshot
  │   ├── ReadModels/
  │   │   ├── CostReportReadModel.cs           ← đã có
  │   │   ├── ProjectSummarySnapshot.cs        ← MỚI — denormalized per project
  │   │   └── ProjectSummaryProjector.cs       ← MỚI — subscribe MediatR events
  │   └── Configurations/                      ← thêm EF config cho Alert, AlertPreference, ProjectSummarySnapshot
  │
  └── ProjectManagement.Reporting.Api/
      └── Controllers/
          ├── ReportingController.cs           ← đã có
          ├── DashboardController.cs           ← MỚI → /api/v1/dashboard/*
          └── AlertsController.cs              ← MỚI → /api/v1/alerts/*
```

#### 8.2.2 Data Model Mới

**ProjectSummarySnapshot** (denormalized, read-optimized, lưu trong `reporting` schema):
```csharp
public class ProjectSummarySnapshot
{
    public Guid ProjectId { get; }
    public string Name { get; }
    public string HealthStatus { get; }         // OnTrack | AtRisk | Delayed
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }
    public decimal PercentComplete { get; }     // % tasks done
    public decimal PercentTimeElapsed { get; }  // % thời gian đã trôi qua
    public int RemainingTaskCount { get; }
    public int OverdueTaskCount { get; }
    public int OverloadedResourceCount { get; }
    public DateTime LastUpdatedAt { get; }
}
```

**Alert + AlertPreference** (trong `reporting` schema):
```sql
CREATE TABLE alerts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id      UUID,
    user_id         UUID NOT NULL,
    type            VARCHAR(50) NOT NULL,    -- 'deadline' | 'overload' | 'budget'
    entity_type     VARCHAR(50),
    entity_id       UUID,
    title           VARCHAR(200) NOT NULL,
    description     TEXT,
    is_read         BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    read_at         TIMESTAMPTZ
);

CREATE TABLE alert_preferences (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NOT NULL,
    alert_type      VARCHAR(50) NOT NULL,
    enabled         BOOLEAN NOT NULL DEFAULT TRUE,
    threshold_days  INT,
    UNIQUE (user_id, alert_type)
);
```

**PostgreSQL Indexes** (bắt buộc — performance SLA):
```sql
-- reporting schema
CREATE INDEX ix_project_summary_snapshots_project_id
    ON project_summary_snapshots(project_id);
CREATE INDEX ix_alerts_user_read
    ON alerts(user_id, is_read, created_at DESC);

-- projects schema (cross-query được phép vì ProjectSummaryProjector chạy trong cùng process)
CREATE INDEX ix_tasks_project_status_due
    ON tasks(project_id, status, due_date);
CREATE INDEX ix_assignments_assignee_week_start
    ON assignments(assignee_id, week_start);
```

#### 8.2.3 ProjectSummaryProjector — Cập Nhật Khi Nào

```csharp
public class ProjectSummaryProjector :
    INotificationHandler<TaskCreatedNotification>,
    INotificationHandler<TaskStatusChangedNotification>,
    INotificationHandler<TaskDueDateChangedNotification>,
    INotificationHandler<TimeEntryCreatedNotification>
{
    // Mỗi handler: recompute snapshot cho project liên quan
    // Dùng PostgreSQL UPSERT (ON CONFLICT DO UPDATE) — atomic, không race condition
    // Snapshot được tính lại toàn bộ cho project đó, không incremental
}
```

**Quy tắc traffic-light ở project level:**
```
OnTrack:  PercentComplete >= PercentTimeElapsed - 10% AND OverdueTaskCount == 0
AtRisk:   OverdueTaskCount trong [1, 3] OR PercentComplete < PercentTimeElapsed - 10%
Delayed:  OverdueTaskCount > 3 OR PercentComplete < PercentTimeElapsed - 25%
```

#### 8.2.4 API Contracts Mới

```
GET  /api/v1/dashboard/summary
     Query: projectIds[] (optional, default = all PM's projects)
     Response: ProjectSummaryDto[]
     Cache: Cache-Control: max-age=60

GET  /api/v1/dashboard/deadlines
     Query: daysAhead=7, projectIds[]
     Response: DeadlineDto[]  (top 7, sorted due_date ASC)

GET  /api/v1/dashboard/stat-cards
     Response: { overdueTaskCount, atRiskProjectCount, overloadedResourceCount }
     Cache: Cache-Control: max-age=60

GET  /api/v1/dashboard/my-tasks
     Query: status[], projectIds[], page, pageSize
     Response: PagedResult<MyTaskDto>

GET  /api/v1/alerts
     Query: unreadOnly=true|false (default false)
     Response: AlertDto[]

PATCH /api/v1/alerts/{id}/read
     Response: 204 No Content

GET  /api/v1/reports/budget
     Query: month=2026-04, projectIds[]
     Response: BudgetReportDto
     Cache: Cache-Control: max-age=300
```

---

### 8.3 Frontend — Hai Module Lazy-Loaded Mới

#### 8.3.1 Route Architecture

**`app.routes.ts`** — thêm 2 lazy routes mới:
```typescript
{
  path: 'dashboard',
  loadChildren: () =>
    import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
},
{
  path: 'reports',
  loadChildren: () =>
    import('./features/reports/reports.routes').then(m => m.REPORTS_ROUTES)
}
```

**`dashboard.routes.ts`:**
```typescript
export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardShellComponent,   // layout với sidebar + navbar
    canActivate: [AuthGuard],
    providers: [provideState(dashboardFeature)],
    children: [
      { path: 'overview', component: DashboardOverviewComponent },
      { path: 'my-tasks', component: MyTasksComponent },
      { path: '', redirectTo: 'overview', pathMatch: 'full' }
    ]
  }
];
```

**`reports.routes.ts`:**
```typescript
export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    component: ReportShellComponent,      // clean layout: header + "← Back to Dashboard"
    canActivate: [AuthGuard],
    providers: [provideState(reportsFeature)],
    children: [
      { path: 'budget', component: BudgetReportComponent },
      { path: 'resources', component: ResourceReportComponent },   // Growth
      { path: 'milestones', component: MilestoneReportComponent }, // Growth
      { path: 'vendor', component: VendorReportComponent },        // Growth
      { path: '', redirectTo: 'budget', pathMatch: 'full' }
    ]
  }
];
```

#### 8.3.2 Cấu Trúc Thư Mục Frontend

```
src/app/features/
│
├── dashboard/
│   ├── dashboard.routes.ts
│   ├── shells/
│   │   └── dashboard-shell/
│   │       ├── dashboard-shell.ts          ← layout: sidebar + navbar + <router-outlet>
│   │       ├── dashboard-shell.html
│   │       └── dashboard-shell.scss
│   ├── components/
│   │   ├── overview/
│   │   │   ├── dashboard-overview.ts       ← container: compose widgets
│   │   │   ├── portfolio-health-card/
│   │   │   │   └── portfolio-health-card.ts    ← traffic light + % + pulse strip
│   │   │   ├── project-pulse-strip/
│   │   │   │   └── project-pulse-strip.ts      ← progress ring + mini timeline bar (dual-axis)
│   │   │   ├── stat-cards/
│   │   │   │   └── stat-cards.ts               ← 3 summary counters
│   │   │   └── upcoming-deadlines/
│   │   │       └── upcoming-deadlines.ts        ← top 7, click → drill-down Gantt/task
│   │   └── my-tasks/
│   │       └── my-tasks.ts                 ← cross-project task list + filter bar
│   ├── store/
│   │   ├── dashboard.actions.ts
│   │   ├── dashboard.reducer.ts
│   │   ├── dashboard.effects.ts            ← polling + router-store URL sync
│   │   ├── dashboard.selectors.ts
│   │   └── dashboard.facade.ts             ← DashboardFilterFacade (public API cho components)
│   ├── services/
│   │   └── dashboard-api.service.ts
│   └── models/
│       └── dashboard.model.ts
│
└── reports/
    ├── reports.routes.ts
    ├── shells/
    │   └── report-shell/
    │       ├── report-shell.ts             ← clean layout: project name + "← Back to Dashboard"
    │       ├── report-shell.html
    │       └── report-shell.scss           ← @media print: ẩn toolbar, page-break rules
    ├── components/
    │   └── budget/
    │       ├── budget-report.ts            ← container
    │       ├── budget-filter-bar.ts        ← month + project scope selector
    │       └── budget-table.ts             ← planned vs actual, vendor breakdown, anomaly highlight
    ├── store/
    │   ├── reports.actions.ts
    │   ├── reports.reducer.ts
    │   ├── reports.effects.ts
    │   └── reports.selectors.ts
    └── services/
        └── reports-api.service.ts
```

---

### 8.4 NgRx Store Design

#### DashboardState

```typescript
interface DashboardFilters {
  selectedProjectIds: string[];          // [] = tất cả projects của PM
  dateRange: { start: string; end: string } | null;
  quickChips: string[];
}

interface StatCards {
  overdueTaskCount: number;
  atRiskProjectCount: number;
  overloadedResourceCount: number;
}

interface DashboardState {
  filters: DashboardFilters;
  projects: EntityState<ProjectSummary>;  // @ngrx/entity adapter
  deadlines: Deadline[];
  statCards: StatCards | null;
  loading: boolean;
  error: string | null;
  lastUpdatedAt: number | null;           // Date.now() timestamp
}
```

**Actions:**
```typescript
'[Dashboard] Start Polling'
'[Dashboard] Stop Polling'
'[Dashboard] Load Portfolio'
'[Dashboard] Load Portfolio Success'
'[Dashboard] Load Portfolio Failure'
'[Dashboard] Set Filters'               ← dispatch khi router-store detect URL params thay đổi
'[Dashboard] Mark Alert Read'
'[Dashboard] Mark Alert Read Success'
```

#### ReportsState

```typescript
interface ReportsFilters {
  month: string;                        // 'YYYY-MM'
  projectIds: string[];                 // [] = tất cả
}

interface ReportsState {
  filters: ReportsFilters;
  budgetReport: BudgetReport | null;
  loading: boolean;
  error: string | null;
}
```

---

### 8.5 URL Sync với @ngrx/router-store

**Nguyên tắc:** URL là **nguồn sự thật duy nhất** — component không tự cập nhật store filter trực tiếp, chỉ navigate URL, router-store tự sync.

```typescript
// dashboard.effects.ts

// 1. Router → Store: khi URL thay đổi, parse params → dispatch SetFilters
syncFiltersFromUrl$ = createEffect(() =>
  this.actions$.pipe(
    ofType(routerNavigatedAction),
    filter(action => action.payload.routerState.url.startsWith('/dashboard')),
    map(action => {
      const params = action.payload.routerState.queryParams;
      return DashboardActions.setFilters({
        filters: {
          selectedProjectIds: params['projects'] ? params['projects'].split(',') : [],
          dateRange: params['from'] && params['to']
            ? { start: params['from'], end: params['to'] }
            : null,
          quickChips: params['chips'] ? params['chips'].split(',') : []
        }
      });
    })
  )
);

// 2. Store → URL: khi filter UI thay đổi, navigate để cập nhật URL
updateUrl$ = createEffect(() =>
  this.store.select(selectDashboardFilters).pipe(
    distinctUntilChanged(isEqual),
    skip(1),  // bỏ qua emit đầu tiên (init từ URL)
    tap(filters => this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        projects: filters.selectedProjectIds.length ? filters.selectedProjectIds.join(',') : null,
        from: filters.dateRange?.start ?? null,
        to: filters.dateRange?.end ?? null,
        chips: filters.quickChips.length ? filters.quickChips.join(',') : null
      },
      queryParamsHandling: 'merge'
    }))
  ),
  { dispatch: false }
);
```

**URL param convention:**
| Filter | URL Param | Ví dụ |
|---|---|---|
| Project scope | `projects` | `?projects=id1,id2` |
| Date range start | `from` | `?from=2026-04-01` |
| Date range end | `to` | `?to=2026-04-30` |
| Quick chips | `chips` | `?chips=overdue,atRisk` |

---

### 8.6 Polling Strategy

```typescript
// dashboard.effects.ts
pollDashboard$ = createEffect(() =>
  this.actions$.pipe(
    ofType(DashboardActions.startPolling),
    switchMap(() =>
      timer(0, 30_000).pipe(                        // load ngay + mỗi 30s
        takeUntil(this.actions$.pipe(ofType(DashboardActions.stopPolling))),
        map(() => DashboardActions.loadPortfolio())
      )
    )
  )
);

// Phân tách 3 API calls riêng biệt — failure độc lập, không block nhau
loadPortfolio$ = createEffect(() =>
  this.actions$.pipe(
    ofType(DashboardActions.loadPortfolio),
    switchMap(() => merge(
      this.dashboardApi.getSummary().pipe(
        map(data => DashboardActions.loadSummarySuccess({ data })),
        catchError(err => of(DashboardActions.loadSummaryFailure({ error: err.message })))
      ),
      this.dashboardApi.getDeadlines().pipe(
        map(data => DashboardActions.loadDeadlinesSuccess({ data })),
        catchError(err => of(DashboardActions.loadDeadlinesFailure({ error: err.message })))
      ),
      this.dashboardApi.getStatCards().pipe(
        map(data => DashboardActions.loadStatCardsSuccess({ data })),
        catchError(err => of(DashboardActions.loadStatCardsFailure({ error: err.message })))
      )
    ))
  )
);
```

**Lifecycle trong shell:**
```typescript
// dashboard-shell.ts
ngOnInit() { this.store.dispatch(DashboardActions.startPolling()); }
ngOnDestroy() { this.store.dispatch(DashboardActions.stopPolling()); }
// ReportShellComponent: KHÔNG dispatch startPolling — load once theo filter
```

---

### 8.7 Widget Error Isolation

Mỗi widget là standalone component nhận data qua `@Input()`:

```typescript
// portfolio-health-card.ts — ĐÚNG pattern
@Input() loading = false;
@Input() error: string | null = null;
@Input() projects: ProjectSummary[] = [];

// DashboardOverviewComponent (container) inject Store, map selectors → @Input bindings
// Khi 1 API fail → widget đó hiện error-state, 2 widget kia vẫn hiển thị bình thường
```

---

### 8.8 Mapping FRs Dashboard → Implementation

| FR | Mô tả | Backend | Frontend | Phase |
|---|---|---|---|---|
| FR1–FR5 | Portfolio health cards, pulse strip | `GetProjectsSummaryQuery` | `portfolio-health-card`, `project-pulse-strip` | MVP W1-2 |
| FR6 | Stakeholder read-only view | (auth + existing) | `DashboardShellComponent` (no edit actions) | MVP W1-2 |
| FR7–FR9 | Overload detection stat cards | `GetStatCardsQuery` | `stat-cards` | MVP W1-2 |
| FR12–FR14 | Upcoming deadlines + drill-down | `GetUpcomingDeadlinesQuery` | `upcoming-deadlines` | MVP W1-2 |
| FR15–FR16 | My tasks cross-project | `GetMyTasksCrossProjectQuery` | `my-tasks` | MVP W3-4 |
| FR17–FR22 | Budget report | `GetCostSummaryQuery` (mở rộng) | `budget-report`, `budget-filter-bar` | MVP W3-4 |
| FR25–FR30 | Filter + URL sync | N/A | NgRx + router-store effects | MVP W3-4 |
| FR31–FR32 | Export PDF/Excel | `TriggerPdfExport` (mở rộng Reporting) | Export buttons trong `report-shell` | Growth |
| FR37–FR39 | Alert Center schema | `Alert`, `AlertPreference` entities | UI ở Growth | Schema MVP W3-4 |
| FR40–FR44 | Data freshness, widget isolation | `lastUpdatedAt` field trên mọi response | `lastUpdatedAt` hiển thị trên widget, error state | MVP W1-2 |

---

### 8.9 Checklist Bổ Sung Cho AI Agents (Dashboard Extension)

- [ ] `DashboardController` route prefix: `/api/v1/dashboard/` (không phải `/api/v1/dashboard-*`)
- [ ] `ProjectSummarySnapshot` dùng UPSERT (`ON CONFLICT DO UPDATE`) — không INSERT mới mỗi lần
- [ ] Polling Effect dùng `switchMap` + `takeUntil(stopPolling)` — **không** dùng `mergeMap`
- [ ] `ReportShellComponent` template **KHÔNG** chứa sidebar/navbar — chỉ `<router-outlet>` và header tối giản
- [ ] URL filter params: `projects`, `from`, `to`, `chips` (viết tắt để URL ngắn)
- [ ] Widget components nhận data qua `@Input()` — **không** inject Store trực tiếp trong widget
- [ ] `alerts` và `alert_preferences` tables trong `reporting` schema — **không** tạo schema mới
- [ ] `Cache-Control: max-age=60` cho `/api/v1/dashboard/*`; `max-age=300` cho `/api/v1/reports/*`
- [ ] `ProjectSummaryProjector` phải subscribe cả `TaskStatusChangedNotification` lẫn `TimeEntryCreatedNotification`
- [ ] `ReportsModule` lazy-loaded — **không** import vào `AppModule` hay `DashboardModule`
- [ ] `@ngrx/router-store` phải được provide ở root (`app.config.ts`) không phải feature level


---

## Phần 9: Phase 2 — Architecture Decisions (Jira Feature Parity)

_Các quyết định kiến trúc cho Epics 8–15 được bổ sung 2026-04-29._

---

### AD-09: Issue Model Migration Strategy (Epic 8)

**Quyết định**: Dùng **expand-contract pattern** với Table-Per-Hierarchy (TPH) và `discriminator` column.

**Migration scripts (4 phases):**

```
V008_001__expand_add_issue_columns.sql       -- Phase 1: additive columns (non-breaking)
V008_002__backfill_discriminator_issue_type.sql -- Phase 2: data backfill
V008_003__rename_table_create_compat_view.sql  -- Phase 3: rename + backward-compat view
V008_004__drop_compat_view_add_constraints.sql -- Phase 4: contract (cleanup)
```

**Phase 1 — Expand (non-breaking):**
```sql
ALTER TABLE project_tasks
  ADD COLUMN IF NOT EXISTS discriminator     VARCHAR(50)  NULL,
  ADD COLUMN IF NOT EXISTS issue_type_id     UUID         NULL REFERENCES issue_type_definitions(id),
  ADD COLUMN IF NOT EXISTS issue_key         VARCHAR(20)  NULL,
  ADD COLUMN IF NOT EXISTS parent_issue_id   UUID         NULL REFERENCES project_tasks(id),
  ADD COLUMN IF NOT EXISTS custom_fields     JSONB        NULL,
  ADD COLUMN IF NOT EXISTS workflow_state_id UUID         NULL;
```

**Phase 3 — Rename + view:**
```sql
ALTER TABLE project_tasks RENAME TO issues;
CREATE VIEW project_tasks AS SELECT * FROM issues WHERE discriminator = 'Task';
```

**EF Core:** Thêm `HasDiscriminator<string>("discriminator")` trên root `IssueConfiguration`. Derived types: `EpicIssue`, `StoryIssue`, `BugIssue`, `SubTaskIssue` với `.HasValue("Epic")` etc. Giai đoạn Phase 1–3: `DbSet<ProjectTask>` map sang view; Phase 4: switch sang `issues` trực tiếp.

**Rollback strategy:**
- Phase 1–3: fully reversible (drop columns / rename back)
- Phase 4: cần blue/green deployment + feature flag trước khi apply

---

### AD-10: Issue Type Definitions Schema (Epic 8)

```sql
CREATE TABLE issue_type_definitions (
  id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  name         VARCHAR(50) NOT NULL,
  icon_key     VARCHAR(50),
  color        VARCHAR(7),
  is_built_in  BOOLEAN     NOT NULL DEFAULT false,
  is_deletable BOOLEAN     NOT NULL DEFAULT true,
  project_id   UUID        NULL REFERENCES projects(id) ON DELETE CASCADE,
  sort_order   INT         NOT NULL DEFAULT 0,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_issue_type_name_per_project UNIQUE (project_id, name)
);
CREATE INDEX idx_issue_type_defs_project ON issue_type_definitions(project_id);
```

Built-in types (seeded, immutable): `Epic`, `Story`, `Task`, `Bug`, `Sub-task` với `project_id = NULL` (system-wide).

**API endpoints:**
| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/issue-types` | List system-wide built-in types |
| `GET` | `/api/v1/projects/{id}/issue-types` | List all types (built-in + custom) |
| `POST` | `/api/v1/projects/{id}/issue-types` | Create custom type |
| `PUT` | `/api/v1/projects/{id}/issue-types/{typeId}` | Update custom type |
| `DELETE` | `/api/v1/projects/{id}/issue-types/{typeId}` | Delete custom type (if unused) |

---

### AD-11: Resource → User Identity Bridge (Epic 10 — Collaboration prerequisite)

**Problem**: `Resource` entity (workforce) ≠ `User` entity (auth). @mentions cần biết "Resource X" = "User account Y".

**Solution**: Optional FK `user_id` trên `resources` table:

```sql
-- V010_001__add_user_id_to_resources.sql
ALTER TABLE resources
  ADD COLUMN user_id UUID NULL REFERENCES users(id) ON DELETE SET NULL;
CREATE UNIQUE INDEX uq_resources_user_id ON resources(user_id) WHERE user_id IS NOT NULL;
```

**Rules:**
- Quan hệ: `Resource` 0..1 ↔ 1 `User` (một user chỉ link với 1 resource max)
- @mentions chỉ hoạt động cho resources có `user_id IS NOT NULL`
- UI hiển thị badge "No account linked" khi `user_id IS NULL`
- Admin link/unlink qua: `PATCH /api/v1/resources/{id}/link-user` và `/unlink-user`
- Audit log mọi link/unlink action

---

### AD-12: Workflow Engine — FSM Config in PostgreSQL (Epic 11)

**Quyết định**: Stateless FSM configuration lưu dạng JSON trong PostgreSQL `workflow_definitions` table. Implement FSM thuần C# với dictionary-based transition table. Không dùng workflow thư viện phức tạp.

**Schema JSON:**
```json
{
  "states": ["Open", "InProgress", "Review", "Done"],
  "transitions": [
    {"from": "Open", "to": "InProgress", "permission": "assignee"},
    {"from": "InProgress", "to": "Review", "permission": "assignee"},
    {"from": "Review", "to": "Done", "permission": "reviewer"}
  ]
}
```

**MediatR pipeline behavior** validate transition trước khi apply — clean và testable.

---

### AD-13: Custom Fields — JSONB Column (Epic 14)

**Quyết định**: JSONB column `custom_fields` trên `issues` table. Không dùng EAV.

**Lý do**: EAV với 20 users là over-engineering; query complexity cao; JSONB với GIN index đủ performant.

```sql
-- Đã thêm trong Phase 1 migration (V008_001)
-- custom_fields JSONB NULL
-- Phase 4 thêm GIN index:
CREATE INDEX idx_issues_custom_fields_gin ON issues USING gin(custom_fields);
```

Schema validation ở application layer qua `field_definitions` table (không enforce tại DB level).

---

### AD-14: Search — PostgreSQL Full-Text Search First (Epic 12)

**Quyết định**: PostgreSQL `tsvector` + GIN index. Elasticsearch sau nếu cần.

**Lý do**: ~20 users, dự kiến <10k issues — PostgreSQL FTS đủ performant (p95 <1s). Migrate sang Elasticsearch chỉ khi latency >500ms dưới load thực tế.

```sql
-- Phase 4 migration thêm:
ALTER TABLE issues ADD COLUMN search_vector tsvector
  GENERATED ALWAYS AS (
    to_tsvector('simple', coalesce(name, '') || ' ' || coalesce(description, ''))
  ) STORED;
CREATE INDEX idx_issues_search_gin ON issues USING gin(search_vector);
```

Dùng **parameterized query builder** — map UI filter components thành `WHERE` clauses. Không implement JQL parser đầy đủ ở Phase 2 (tiết kiệm 2–3 tuần dev).

---

### AD-15: File Attachments — S3-Compatible Storage (Epic 10)

**Quyết định**: S3-compatible storage từ đầu (MinIO cho self-host on-premise).

**Lý do**: Local disk storage là technical debt — scale, backup, CDN đều khó sau này. Cost với 20 users: gần như zero.

**Flow**: Frontend upload trực tiếp qua presigned URL → S3; metadata (filename, size, content_type, storage_key) lưu PostgreSQL `issue_attachments` table.

---

### AD-16: UI Feedback Pattern — FeedbackDialogService (Bắt buộc từ CC-01)

**Quyết định**: Toàn bộ user feedback (success/error) phải dùng `FeedbackDialogService` — không dùng `MatSnackBar` trực tiếp trong bất kỳ component nào.

**Lý do**:
- Error dialog phải hiển thị `traceId` từ `ProblemDetails` để support production debugging
- Error feedback phải có nút "Xác nhận" bắt buộc — user không được bỏ lỡ lỗi quan trọng
- Snackbar không log structured → khó correlate với backend Serilog logs
- Consistency: một pattern duy nhất cho toàn bộ codebase, không phụ thuộc vị trí hiển thị

**Pattern bắt buộc**:
```typescript
// ✅ ĐÚNG — inject FeedbackDialogService
private readonly feedbackDialog = inject(FeedbackDialogService);
// Thành công:
this.feedbackDialog.success('Lưu thành công');
// Lỗi (truyền HttpErrorResponse gốc để extract traceId):
this.feedbackDialog.error('Không thể tạo task', err);

// ❌ SAI — không được dùng trực tiếp
this.snackBar.open('...', 'Đóng', { duration: 3000 });
```

**Behavior**:
| Mode | Auto-close | User action | TraceId | Log |
|---|---|---|---|---|
| `success` | Sau 3s | Click ngoài để đóng sớm | Không hiển thị | Không |
| `error` | Không bao giờ | Nút "Xác nhận" bắt buộc | Hiển thị `Mã lỗi: xxx` | `console.error` structured |

**Location**: `shared/services/feedback-dialog.service.ts` + `shared/components/feedback-dialog/`

**Áp dụng**: Mọi story từ CC-01 trở đi. Mọi story mới có UI phải:
1. Inject `FeedbackDialogService` cho tất cả success/error feedback
2. KHÔNG import `MatSnackBar` hay `MatSnackBarModule`
3. Truyền `HttpErrorResponse` gốc (không chỉ message string) vào `.error()` để extract traceId tự động

---

### Checklist Epics 8–15 — AI Implementation Notes

- [ ] Issue migration **PHẢI** có integration test coverage trước khi Phase 3 (rename)
- [ ] Gantt module (Bryntum adapter) cần verify vẫn nhận đúng data sau rename — test với `project_tasks` view
- [ ] `TaskType` enum migration: tạo `IssueType` entity reference **trong story 8.1 riêng**, không đồng thời với Story 8.0
- [ ] Board polling interval: **10s** (nhanh hơn dashboard 30-60s) — configure riêng per route
- [ ] Automation rules: **async always** — không block API response; dùng background job/queue
- [ ] Webhook HMAC: `X-Hub-Signature-256: sha256=...` header bắt buộc; retry exponential backoff 1s/5s/25s
- [ ] Permission check: deny-by-default; cache 60s TTL; audit log mọi failed permission check
- [ ] `fr55-fr160-jira-requirements.md` là source of truth cho requirements Phase 2+

