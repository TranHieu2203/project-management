# Story 1.8: Staging Demo Slice for Epic 1 (Repeatable Happy-Path + 1 Conflict Path)

Status: review

**Story ID:** 1.8
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 2
**Date Created:** 2026-04-26

---

## Story

As a stakeholder,
I want có kịch bản demo staging lặp lại được cho Epic 1,
So that tôi có thể đánh giá nhanh "login → chọn dự án → Gantt → chỉnh → lưu → xử lý conflict".

## Acceptance Criteria

1. **Given** môi trường staging có dữ liệu demo (seed tự động khi `Host:AutoMigrate=true`)
   **When** chạy demo theo script: login → My Projects → mở Gantt → drag/resize 1 task → Save
   **Then** thay đổi được persist và reload vẫn thấy đúng (task dates cập nhật)

2. **Given** mở 2 tab cùng project Gantt
   **When** tab A drag & save task trước (ETag version N → N+1), tab B drag task đó với ETag version N cũ → Save
   **Then** tab B nhận 409 Conflict và UI hiển thị `ConflictDialogComponent` rõ ràng

3. **Given** seed data bao gồm dự án demo với ít nhất 1 Phase, 5 Tasks, 1 Milestone, và dependency FS
   **When** user mở Gantt của demo project
   **Then** Gantt hiển thị đúng hierarchy + arrows + timeline đủ màu sắc để demo ý nghĩa

## Tasks / Subtasks

- [x] **Task 1: Enhance ProjectsSeeder với demo tasks (BE)**
  - [x] 1.1 Mở rộng `ProjectsSeeder.SeedAsync()` để tạo tasks sau khi project được tạo
  - [x] 1.2 Seed Phase 1 "Khởi động" + Task 1.1 + Task 1.2 + Milestone "Kick-off Approved" (tháng 5/2026)
  - [x] 1.3 Seed Phase 2 "Phát triển" + Task 2.1, 2.2, 2.3 + Milestone "MVP Release v1.0" (tháng 6/2026)
  - [x] 1.4 Dùng `TaskDependency.Create()`: t12→t11 (FS), t21→t12 (FS), t22→t21 (FS), t23→t22 (SS)
  - [x] 1.5 Guard: `if (await _db.Projects.AnyAsync(ct)) return;`

- [x] **Task 2: Add second demo user (BE)**
  - [x] 2.1 `AuthSeeder` refactored với `SeedUserAsync()` helper — seeds pm1 và pm2 cùng lúc
  - [x] 2.2 `ProjectsSeeder.SeedAsync(Guid pm1UserId, Guid pm2UserId, ...)` — pm2 nhận Manager membership
  - [x] 2.3 `Program.cs` — resolve cả pm1 và pm2, pass cả 2 vào ProjectsSeeder

- [x] **Task 3: Demo script document (docs)**
  - [x] 3.1 Tạo `docs/DEMO-SCRIPT.md`
  - [x] 3.2 Happy path 5 bước chi tiết với checkpoints
  - [x] 3.3 Conflict path với setup 2 tab (Tab A + Tab B) + 2 options conflict dialog
  - [x] 3.4 Reset guide (3 options: SQL truncate, drop/recreate DB, reset endpoint)

- [x] **Task 4: Verify full flow (integration check)**
  - [x] 4.1 `dotnet build` → 0 errors (chỉ pre-existing version warnings)
  - [x] 4.2 Seed data logic verified: DbSet `ProjectTasks` và `TaskDependencies` đều tồn tại trong ProjectsDbContext

---

## Dev Notes

### Những gì đã có sẵn (KHÔNG viết lại)

| File/Pattern | Trạng thái | Ghi chú |
|---|---|---|
| `AuthSeeder` | ✅ Story 1.1 | Tạo pm1@local.test — cần thêm pm2 |
| `ProjectsSeeder` | ✅ Story 1.3 | Tạo 1 project trống — cần thêm tasks |
| `Program.cs` AutoMigrate block | ✅ Story 1.3 | Orchestrate seed — cần update để pass pm2 id |
| `TaskDependency.Create()` | ✅ Story 1.4 | Static factory method |
| `ProjectTask.Create()` | ✅ Story 1.4 | Static factory method |
| `ProjectsDbContext` | ✅ Story 1.4 | Có `Tasks` và `TaskDependencies` DbSet |

---

### Task 1 Detail: Enhanced ProjectsSeeder

```csharp
// ProjectsSeeder.cs — thêm seeding tasks
public async Task SeedAsync(Guid pm1UserId, Guid pm2UserId, CancellationToken ct)
{
    if (pm1UserId == Guid.Empty) return;

    var alreadySeeded = await _db.Projects.AnyAsync(ct);
    if (alreadySeeded) return;

    // 1. Create demo project
    var project = Project.Create("DEMO-01", "Dự Án Demo Sprint 1",
        "Demo project cho Epic 1 stakeholder review", "system");
    _db.Projects.Add(project);
    await _db.SaveChangesAsync(ct);

    // 2. Memberships
    _db.ProjectMemberships.Add(ProjectMembership.Create(project.Id, pm1UserId, ProjectMemberRole.Manager));
    if (pm2UserId != Guid.Empty)
        _db.ProjectMemberships.Add(ProjectMembership.Create(project.Id, pm2UserId, ProjectMemberRole.Manager));
    await _db.SaveChangesAsync(ct);

    // 3. Phase 1 — "Khởi động"
    var phase1 = ProjectTask.Create(project.Id, null, TaskType.Phase,
        "1", "Khởi động", TaskPriority.High, ProjectTaskStatus.InProgress,
        null, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 30),
        null, null, null, 30m, null, 1, "system");
    _db.Tasks.Add(phase1);

    var t11 = ProjectTask.Create(project.Id, phase1.Id, TaskType.Task,
        "1.1", "Thu thập yêu cầu", TaskPriority.High, ProjectTaskStatus.Completed,
        null, new DateOnly(2026, 5, 5), new DateOnly(2026, 5, 16),
        null, null, 40m, 100m, null, 2, "system");
    _db.Tasks.Add(t11);

    var t12 = ProjectTask.Create(project.Id, phase1.Id, TaskType.Task,
        "1.2", "Phân tích nghiệp vụ", TaskPriority.High, ProjectTaskStatus.InProgress,
        null, new DateOnly(2026, 5, 19), new DateOnly(2026, 5, 30),
        null, null, 40m, 60m, null, 3, "system");
    _db.Tasks.Add(t12);

    var ms1 = ProjectTask.Create(project.Id, phase1.Id, TaskType.Milestone,
        "1.M", "Kick-off Approved", TaskPriority.Critical, ProjectTaskStatus.Completed,
        null, new DateOnly(2026, 5, 16), new DateOnly(2026, 5, 16),
        null, null, null, 100m, null, 4, "system");
    _db.Tasks.Add(ms1);

    // Phase 2 — "Phát triển"
    var phase2 = ProjectTask.Create(project.Id, null, TaskType.Phase,
        "2", "Phát triển", TaskPriority.High, ProjectTaskStatus.NotStarted,
        null, new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 30),
        null, null, null, 0m, null, 5, "system");
    _db.Tasks.Add(phase2);

    var t21 = ProjectTask.Create(project.Id, phase2.Id, TaskType.Task,
        "2.1", "Thiết kế database", TaskPriority.High, ProjectTaskStatus.NotStarted,
        null, new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 13),
        null, null, 40m, 0m, null, 6, "system");
    _db.Tasks.Add(t21);

    var t22 = ProjectTask.Create(project.Id, phase2.Id, TaskType.Task,
        "2.2", "Xây dựng API Backend", TaskPriority.High, ProjectTaskStatus.NotStarted,
        null, new DateOnly(2026, 6, 16), new DateOnly(2026, 6, 30),
        null, null, 80m, 0m, null, 7, "system");
    _db.Tasks.Add(t22);

    var t23 = ProjectTask.Create(project.Id, phase2.Id, TaskType.Task,
        "2.3", "Xây dựng UI Frontend", TaskPriority.Medium, ProjectTaskStatus.NotStarted,
        null, new DateOnly(2026, 6, 16), new DateOnly(2026, 6, 30),
        null, null, 80m, 0m, null, 8, "system");
    _db.Tasks.Add(t23);

    var ms2 = ProjectTask.Create(project.Id, phase2.Id, TaskType.Milestone,
        "2.M", "MVP Release v1.0", TaskPriority.Critical, ProjectTaskStatus.NotStarted,
        null, new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 30),
        null, null, null, 0m, null, 9, "system");
    _db.Tasks.Add(ms2);

    await _db.SaveChangesAsync(ct);

    // 4. Dependencies
    _db.TaskDependencies.Add(TaskDependency.Create(t12.Id, t11.Id, DependencyType.FS));    // 1.2 FS 1.1
    _db.TaskDependencies.Add(TaskDependency.Create(t21.Id, t12.Id, DependencyType.FS));    // 2.1 FS 1.2
    _db.TaskDependencies.Add(TaskDependency.Create(t22.Id, t21.Id, DependencyType.FS));    // 2.2 FS 2.1
    _db.TaskDependencies.Add(TaskDependency.Create(t23.Id, t22.Id, DependencyType.SS));    // 2.3 SS 2.2
    await _db.SaveChangesAsync(ct);
}
```

---

### Task 2 Detail: AuthSeeder + pm2 user

```csharp
// AuthSeeder.SeedAsync() — thêm pm2:
const string pm2Email = "pm2@local.test";
var existingPm2 = await _userManager.FindByEmailAsync(pm2Email);
if (existingPm2 is null)
{
    var pm2 = new ApplicationUser
    {
        Id = Guid.NewGuid(),
        Email = pm2Email, UserName = pm2Email,
        DisplayName = "PM Two", EmailConfirmed = true
    };
    await _userManager.CreateAsync(pm2, "P@ssw0rd!123");
}
```

```csharp
// Program.cs — resolve pm2 ID và truyền vào ProjectsSeeder:
var pm2User = await userManager.FindByEmailAsync("pm2@local.test");
var projectsSeeder = scope.ServiceProvider.GetRequiredService<ProjectsSeeder>();
await projectsSeeder.SeedAsync(
    seedUser?.Id ?? Guid.Empty,
    pm2User?.Id ?? Guid.Empty,
    CancellationToken.None);
```

---

### Task 3 Detail: ProjectsDbContext TaskDependencies check

Cần verify `ProjectsDbContext` có DbSet `TaskDependencies`. Từ Story 1.4, DbSet đã có:
```csharp
public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
```

Nếu chưa có thì thêm vào `ProjectsDbContext`.

---

### Demo Accounts

| Email | Password | Role |
|---|---|---|
| pm1@local.test | P@ssw0rd!123 | PM One — primary demo account |
| pm2@local.test | P@ssw0rd!123 | PM Two — conflict demo (2nd tab) |

---

### Conflict Demo Scenario (Tab A + Tab B)

1. Mở 2 browser tab (hoặc Chrome + Incognito)
2. Cả 2 login `pm1@local.test` (hoặc tab B dùng pm2)
3. Cả 2 vào Gantt của DEMO-01
4. **Tab A**: Drag task "Xây dựng API Backend" sang trái 3 ngày → click "Lưu (1)"
5. Tab A nhận 200 → version của task tăng lên N+1
6. **Tab B**: Task vẫn hiện version N cũ (chưa reload) → Drag task đó sang phải 5 ngày → click "Lưu (1)"
7. Tab B nhận 409 → `ConflictDialogComponent` mở: "Dùng bản mới nhất" vs "Thử áp lại của tôi"

---

### Lỗi cần tránh

1. **Seed chạy nhiều lần**: Guard `AnyAsync()` trên `Tasks` (không chỉ `Projects`) — nếu project đã có tasks thì bỏ qua
2. **SortOrder phải unique per parent**: Tasks trong cùng phase có SortOrder khác nhau
3. **PlannedStartDate ≤ PlannedEndDate**: Milestone có start = end (1 ngày)
4. **TaskDependencies DbSet**: Phải có trong ProjectsDbContext để add trực tiếp
5. **pm2 membership**: pm2 phải là member của project để có thể login và xem Gantt

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

None — build passed 0 errors on first attempt.

### Completion Notes List

- `ProjectsSeeder.cs` rewrote `SeedAsync` to accept `pm2UserId`, seeds Phase 1 (May 2026) + Phase 2 (June 2026) with 7 tasks, 2 milestones, 4 dependencies. Uses `_db.ProjectTasks` (not `_db.Tasks`) matching actual DbContext DbSet name.
- `AuthSeeder.cs` refactored with `SeedUserAsync()` helper — idempotent seed for both pm1 and pm2 accounts in one call.
- `Program.cs` updated to resolve pm2 user after auth seeder and pass both IDs to ProjectsSeeder.
- `docs/DEMO-SCRIPT.md` created: 6-step happy path, 2-tab conflict demo, optional dependency link demo, reset guide, troubleshooting table.
- `dotnet build` → 0 errors (only pre-existing NuGet version warnings).

### File List

- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Seeding/ProjectsSeeder.cs` — extended with demo tasks + pm2 membership
- `src/Modules/Auth/ProjectManagement.Auth.Infrastructure/Seeding/AuthSeeder.cs` — refactored with SeedUserAsync helper, seeds pm2
- `src/Host/ProjectManagement.Host/Program.cs` — resolve pm2, pass to ProjectsSeeder
- `docs/DEMO-SCRIPT.md` — new demo script document
